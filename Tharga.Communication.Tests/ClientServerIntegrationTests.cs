using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tharga.Communication.Client;
using Tharga.Communication.Client.Communication;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;
using Tharga.Communication.Server;
using Tharga.Communication.Server.Communication;
using Xunit;

namespace Tharga.Communication.Tests;

/// <summary>
/// Runs the client services, as registered by AddThargaCommunicationClient, against a real server.
/// </summary>
public class ClientServerIntegrationTests : IAsyncLifetime
{
    private const string ClientMachine = "client-server-test";
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(5);

    private WebApplication _server;
    private IHost _client;

    public record OrderedTopic;
    public record SlowEcho(string Text, int DelayMilliseconds);
    public record SlowEchoReply(string Text);

    public class SlowEchoHandler : SendMessageHandlerBase<SlowEcho, SlowEchoReply>
    {
        public override async Task<SlowEchoReply> Handle(SlowEcho message)
        {
            await Task.Delay(message.DelayMilliseconds);
            return new SlowEchoReply(message.Text);
        }
    }

    public record FailingRequest;
    public record FailingReply(string Text);
    public record UnhandledRequest;

    public class FailingHandler : SendMessageHandlerBase<FailingRequest, FailingReply>
    {
        public override Task<FailingReply> Handle(FailingRequest message) => throw new InvalidOperationException("Handler failure.");
    }

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterClientStateService<IntegrationTests.TestClientStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
            options.AdditionalAssemblies = [GetType().Assembly];
        });

        _server = builder.Build();
        _server.Urls.Add("http://127.0.0.1:0");
        _server.UseThargaCommunicationServer();
        await _server.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.StopAsync();
            _client.Dispose();
        }

        if (_server != null)
        {
            await _server.StopAsync();
            await _server.DisposeAsync();
        }
    }

    [Fact]
    public async Task SubscriptionChangesArrivingTogether_AreAppliedInTheOrderTheyWereSent()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await StartClientAsync(services => services.AddTransient<IMessageExecutor>(sp =>
            new SlowToApplyUnsubscribeExecutor(ActivatorUtilities.CreateInstance<MessageExecutor>(sp))));
        var clientCommunication = client.Services.GetRequiredService<IClientCommunication>();
        var applied = new List<bool>();
        var allApplied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        clientCommunication.SubscriptionChanged += (_, e) =>
        {
            lock (applied)
            {
                applied.Add(e.HasSubscribers);
                if (applied.Count == 3) allApplied.TrySetResult();
            }
        };

        var serverCommunication = _server.Services.GetRequiredService<IServerCommunication>();
        var first = await serverCommunication.SubscribeAsync<OrderedTopic>();
        await first.DisposeAsync();
        await using var second = await serverCommunication.SubscribeAsync<OrderedTopic>();
        await allApplied.Task.WaitAsync(WaitLimit, ct);

        applied.Should().Equal(true, false, true);
        clientCommunication.HasSubscribers<OrderedTopic>().Should().BeTrue();
    }

    [Fact]
    public async Task Client_SendMessage_ResponseArrivingAfterItsRequestTimedOut_IsNotReturnedForTheNextRequest()
    {
        var client = await StartClientAsync();
        var clientCommunication = client.Services.GetRequiredService<IClientCommunication>();

        var timedOut = () => clientCommunication.SendMessage<SlowEcho, SlowEchoReply>(new SlowEcho("first", 500), TimeSpan.FromMilliseconds(100));
        await timedOut.Should().ThrowAsync<TimeoutException>();
        var reply = await clientCommunication.SendMessage<SlowEcho, SlowEchoReply>(new SlowEcho("second", 800), WaitLimit);

        reply.Text.Should().Be("second");
    }

    [Fact]
    public async Task Server_SendMessageAsync_ResponseArrivingAfterItsRequestTimedOut_IsNotReturnedForTheNextRequest()
    {
        await StartClientAsync();
        var serverCommunication = _server.Services.GetRequiredService<IServerCommunication>();
        var connectionId = await GetClientConnectionIdAsync();

        var timedOut = await serverCommunication.SendMessageAsync<SlowEcho, SlowEchoReply>(connectionId, new SlowEcho("first", 500), TimeSpan.FromMilliseconds(100));
        var reply = await serverCommunication.SendMessageAsync<SlowEcho, SlowEchoReply>(connectionId, new SlowEcho("second", 800), WaitLimit);

        timedOut.Code.Should().Be("TIMEOUT");
        reply.IsSuccess.Should().BeTrue();
        reply.Value.Text.Should().Be("second");
    }

    [Fact]
    public async Task Server_SendMessageAsync_ClientHandlerThrows_ReturnsAFailedResponse()
    {
        await StartClientAsync();
        var serverCommunication = _server.Services.GetRequiredService<IServerCommunication>();

        var response = await serverCommunication.SendMessageAsync<FailingRequest, FailingReply>(await GetClientConnectionIdAsync(), new FailingRequest(), WaitLimit);

        response.IsSuccess.Should().BeFalse();
        response.Code.Should().Be("REMOTE_ERROR");
        response.Message.Should().Be("Handler failure.");
    }

    [Fact]
    public async Task Server_SendMessageAsync_ClientHasNoHandler_ReturnsAFailedResponse()
    {
        await StartClientAsync();
        var serverCommunication = _server.Services.GetRequiredService<IServerCommunication>();

        var response = await serverCommunication.SendMessageAsync<UnhandledRequest, FailingReply>(await GetClientConnectionIdAsync(), new UnhandledRequest(), WaitLimit);

        response.IsSuccess.Should().BeFalse();
        response.Code.Should().Be("REMOTE_ERROR");
        response.Message.Should().Contain("Cannot find handler");
    }

    [Fact]
    public async Task Client_SendMessage_ServerHandlerThrows_Throws()
    {
        var client = await StartClientAsync();
        var clientCommunication = client.Services.GetRequiredService<IClientCommunication>();

        var act = () => clientCommunication.SendMessage<FailingRequest, FailingReply>(new FailingRequest(), WaitLimit);

        await act.Should().ThrowAsync<RemoteRequestException>().WithMessage("*Handler failure.*");
    }

    private async Task<string> GetClientConnectionIdAsync()
    {
        var stateService = _server.Services.GetRequiredService<IntegrationTests.TestClientStateService>();
        await foreach (var connection in stateService.GetAsync())
        {
            if (connection.Machine == ClientMachine) return connection.ConnectionId;
        }

        throw new InvalidOperationException("The client is not connected.");
    }

    private async Task<IHost> StartClientAsync(Action<IServiceCollection> configureServices = null)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.AddThargaCommunicationClient(o =>
        {
            o.ServerAddress = _server.Urls.First();
            o.ClientMachine = ClientMachine;
            o.AdditionalAssemblies = [typeof(IClientCommunication).Assembly, GetType().Assembly];
        });
        configureServices?.Invoke(builder.Services);

        _client = builder.Build();
        await _client.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilTheServerHasTheClientAsync();
        return _client;
    }

    private async Task WaitUntilTheServerHasTheClientAsync()
    {
        var stateService = _server.Services.GetRequiredService<IntegrationTests.TestClientStateService>();
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        limit.CancelAfter(WaitLimit);
        while (true)
        {
            await foreach (var connection in stateService.GetAsync())
            {
                if (connection.Machine == ClientMachine) return;
            }

            await Task.Delay(20, limit.Token);
        }
    }

    private sealed class SlowToApplyUnsubscribeExecutor : IMessageExecutor
    {
        private readonly IMessageExecutor _inner;

        public SlowToApplyUnsubscribeExecutor(IMessageExecutor inner)
        {
            _inner = inner;
        }

        public async Task<IMessageWrapper> ExecuteAsync(string connectionId, IMessageWrapper wrapper)
        {
            if (wrapper.Type.StartsWith(typeof(SubscriptionStateChanged).FullName!)
                && !JsonSerializer.Deserialize<SubscriptionStateChanged>(wrapper.Payload)!.HasSubscribers)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(300));
            }

            return await _inner.ExecuteAsync(connectionId, wrapper);
        }
    }
}
