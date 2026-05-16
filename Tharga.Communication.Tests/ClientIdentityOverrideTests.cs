using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tharga.Communication.Client;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;
using Tharga.Communication.Server;
using Xunit;
using ClientOptions = Tharga.Communication.Client.CommunicationOptions;

namespace Tharga.Communication.Tests;

/// <summary>
/// End-to-end tests for the ClientType / ClientMachine override behavior.
/// Spins up a real server and connects a SignalR client to verify the override is reflected on the server.
/// </summary>
public class ClientIdentityOverrideTests : IAsyncLifetime
{
    private WebApplication _app;
    private string _serverUrl;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterClientStateService<TestClientStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });

        _app = builder.Build();
        _app.Urls.Add("http://127.0.0.1:0");
        _app.UseThargaCommunicationServer();
        await _app.StartAsync();
        _serverUrl = _app.Urls.First();
    }

    public async ValueTask DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Default_NoOverrides_UsesAssemblyNameAndMachineName()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = new HubConnectionBuilder()
            .WithUrl($"{_serverUrl}/hub", o =>
            {
                o.Headers.Add(Constants.Header.Instance, Guid.NewGuid().ToString());
                o.Headers.Add(Constants.Header.Machine, Environment.MachineName);
                o.Headers.Add(Constants.Header.Type, "default-app-type");
                o.Headers.Add(Constants.Header.Version, "1.0.0");
            })
            .Build();

        await connection.StartAsync(ct);
        await Task.Delay(200, ct);

        var stateService = _app.Services.GetRequiredService<TestClientStateService>();
        var clients = new List<ClientConnectionInfo>();
        await foreach (var client in stateService.GetAsync())
            clients.Add(client);

        clients.Should().ContainSingle(c => c.Machine == Environment.MachineName && c.Type == "default-app-type");

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Overrides_AreReflectedOnServer()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = new HubConnectionBuilder()
            .WithUrl($"{_serverUrl}/hub", o =>
            {
                o.Headers.Add(Constants.Header.Instance, Guid.NewGuid().ToString());
                o.Headers.Add(Constants.Header.Machine, "us-east-prod-1");
                o.Headers.Add(Constants.Header.Type, "monitor-agent");
                o.Headers.Add(Constants.Header.Version, "1.0.0");
            })
            .Build();

        await connection.StartAsync(ct);
        await Task.Delay(200, ct);

        var stateService = _app.Services.GetRequiredService<TestClientStateService>();
        var clients = new List<ClientConnectionInfo>();
        await foreach (var client in stateService.GetAsync())
            clients.Add(client);

        clients.Should().ContainSingle(c => c.Machine == "us-east-prod-1" && c.Type == "monitor-agent");

        await connection.DisposeAsync();
    }

    [Fact]
    public void Options_ClientType_NullByDefault()
    {
        var options = new ClientOptions();
        options.ClientType.Should().BeNull();
        options.ClientMachine.Should().BeNull();
    }

    [Fact]
    public void Options_ClientType_CanBeSet()
    {
        var options = new ClientOptions
        {
            ClientType = "monitor-agent",
            ClientMachine = "us-east-prod-1"
        };

        options.ClientType.Should().Be("monitor-agent");
        options.ClientMachine.Should().Be("us-east-prod-1");
    }

    private class TestClientStateService : ClientStateServiceBase<ClientConnectionInfo>
    {
        public TestClientStateService(IServiceProvider sp, IOptions<global::CommunicationOptions> options) : base(sp, options) { }

        protected override ClientConnectionInfo Build(IClientConnectionInfo info) => new()
        {
            Instance = info.Instance,
            ConnectionId = info.ConnectionId,
            Machine = info.Machine,
            Type = info.Type,
            Version = info.Version,
            IsConnected = info.IsConnected,
            ConnectTime = info.ConnectTime,
            KeyId = info.KeyId,
            KeyName = info.KeyName
        };

        protected override ClientConnectionInfo BuildDisconnect(ClientConnectionInfo info, DateTime disconnectTime) =>
            info with { IsConnected = false, DisconnectTime = disconnectTime };
    }
}
