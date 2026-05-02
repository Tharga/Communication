using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;
using Tharga.Communication.Server;
using Tharga.Communication.Server.Communication;
using Xunit;

namespace Tharga.Communication.Tests;

/// <summary>
/// Integration tests using a real ASP.NET Core TestServer with SignalR hub.
/// </summary>
public class IntegrationTests : IAsyncLifetime
{
    private WebApplication _app;
    private HubConnection _hubConnection;
    private string _serverUrl;

    // Test types
    public record EchoRequest(string Message);
    public record EchoResponse(string Reply);
    public record FireAndForgetMessage(string Text);

    // Test handler for server-side request-response
    public class EchoHandler : SendMessageHandlerBase<EchoRequest, EchoResponse>
    {
        public override Task<EchoResponse> Handle(EchoRequest message) =>
            Task.FromResult(new EchoResponse($"Echo: {message.Message}"));
    }

    // Test handler for server-side fire-and-forget
    public static readonly List<string> ReceivedMessages = new();

    public class FireAndForgetHandler : PostMessageHandlerBase<FireAndForgetMessage>
    {
        public override Task Handle(FireAndForgetMessage message)
        {
            ReceivedMessages.Add(message.Text);
            return Task.CompletedTask;
        }
    }

    // Minimal client state service for testing
    private class TestClientStateService : ClientStateServiceBase<ClientConnectionInfo>
    {
        public TestClientStateService(IServiceProvider sp, IOptions<CommunicationOptions> options)
            : base(sp, options) { }

        protected override ClientConnectionInfo Build(IClientConnectionInfo info) => new()
        {
            Instance = info.Instance,
            ConnectionId = info.ConnectionId,
            Machine = info.Machine,
            Type = info.Type,
            Version = info.Version,
            IsConnected = info.IsConnected,
            ConnectTime = info.ConnectTime
        };

        protected override ClientConnectionInfo BuildDisconnect(ClientConnectionInfo info, DateTime disconnectTime) =>
            info with { IsConnected = false, DisconnectTime = disconnectTime };
    }

    public async ValueTask InitializeAsync()
    {
        ReceivedMessages.Clear();

        var builder = WebApplication.CreateBuilder();
        // random port set after build

        builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterClientStateService<TestClientStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
            options.AdditionalAssemblies = [GetType().Assembly];
        });

        _app = builder.Build();
        _app.Urls.Add("http://127.0.0.1:0");
        _app.UseThargaCommunicationServer();

        await _app.StartAsync();

        _serverUrl = _app.Urls.First();

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_serverUrl}/hub", options =>
            {
                options.Headers.Add(Constants.Header.Instance, Guid.NewGuid().ToString());
                options.Headers.Add(Constants.Header.Machine, "test-machine");
                options.Headers.Add(Constants.Header.Type, "test-app");
                options.Headers.Add(Constants.Header.Version, "1.0.0");
            })
            .Build();

        await _hubConnection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
        }

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public void Client_ConnectsSuccessfully()
    {
        _hubConnection.State.Should().Be(HubConnectionState.Connected);
    }

    [Fact]
    public async Task Client_PostAsync_ServerHandlerReceivesMessage()
    {
        var ct = TestContext.Current.CancellationToken;
        var wrapper = new RequestWrapper
        {
            Type = typeof(FireAndForgetMessage).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new FireAndForgetMessage("integration-test"))
        };

        await _hubConnection.SendAsync(Constants.PostMessage, wrapper, ct);

        // Give handler time to execute
        await Task.Delay(200, ct);

        ReceivedMessages.Should().Contain("integration-test");
    }

    [Fact]
    public async Task Server_SendMessageAsync_ClientHandlerResponds()
    {
        // Register a client-side handler that echoes back
        _hubConnection.On<RequestWrapper>(Constants.SendMessage, async payload =>
        {
            var request = JsonSerializer.Deserialize<EchoRequest>(payload.Payload);
            var response = new RequestWrapper
            {
                Type = typeof(EchoResponse).AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(new EchoResponse($"ClientEcho: {request.Message}"))
            };
            await _hubConnection.SendAsync(Constants.ResponseMessage, response);
        });

        var serverComm = _app.Services.GetRequiredService<IServerCommunication>();
        var result = await serverComm.SendMessageAsync<EchoRequest, EchoResponse>(
            _hubConnection.ConnectionId!, new EchoRequest("hello"), TimeSpan.FromSeconds(5));

        result.IsSuccess.Should().BeTrue();
        result.Value.Reply.Should().Be("ClientEcho: hello");
    }

    [Fact]
    public async Task ApiKey_Rejection_ClientCannotConnect()
    {
        // Start a second server with API key required
        var builder2 = WebApplication.CreateBuilder();

        builder2.AddThargaCommunicationServer(options =>
        {
            options.PrimaryApiKey = "secret-key";
            options.RegisterClientStateService<TestClientStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });

        var app2 = builder2.Build();
        app2.Urls.Add("http://127.0.0.1:0");
        app2.UseThargaCommunicationServer();
        var ct = TestContext.Current.CancellationToken;
        await app2.StartAsync(ct);

        try
        {
            var url = app2.Urls.First();
            var badConnection = new HubConnectionBuilder()
                .WithUrl($"{url}/hub", options =>
                {
                    options.Headers.Add(Constants.Header.Instance, Guid.NewGuid().ToString());
                    options.Headers.Add(Constants.Header.Machine, "test");
                    options.Headers.Add(Constants.Header.Type, "test");
                    options.Headers.Add(Constants.Header.Version, "1.0");
                    // No API key — should be rejected
                })
                .Build();

            await badConnection.StartAsync(ct);

            // Connection is established but server aborts it — wait for disconnect
            await Task.Delay(500, ct);

            badConnection.State.Should().Be(HubConnectionState.Disconnected);

            await badConnection.DisposeAsync();
        }
        finally
        {
            await app2.StopAsync(ct);
            await app2.DisposeAsync();
        }
    }

    [Fact]
    public async Task ClientState_TrackedOnConnectAndDisconnect()
    {
        var ct = TestContext.Current.CancellationToken;
        var stateService = _app.Services.GetRequiredService<TestClientStateService>();

        // Give time for connect event
        await Task.Delay(100, ct);

        var clients = new List<ClientConnectionInfo>();
        await foreach (var client in stateService.GetAsync())
            clients.Add(client);

        clients.Should().ContainSingle(c => c.IsConnected && c.Machine == "test-machine");

        // Disconnect
        await _hubConnection.StopAsync(ct);
        await Task.Delay(200, ct);

        clients.Clear();
        await foreach (var client in stateService.GetAsync())
            clients.Add(client);

        clients.Should().BeEmpty();
    }
}
