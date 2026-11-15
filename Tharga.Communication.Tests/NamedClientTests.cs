using System.Collections.Concurrent;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tharga.Communication.Client;
using Tharga.Communication.Client.Communication;
using Tharga.Communication.MessageHandler;
using Tharga.Communication.Server;
using Tharga.Communication.Server.Communication;
using Xunit;
using ClientOptions = Tharga.Communication.Client.CommunicationOptions;

namespace Tharga.Communication.Tests;

public class NamedClientTests : IAsyncLifetime
{
    private const string DefaultMachine = "default-client";
    private const string NamedMachine = "named-client";
    private const string ClientName = "other";
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(500);

    private WebApplication _server;
    private IHost _client;

    public record NamedNotice(string Text);
    public record NamedTopic;

    public class NamedInbox
    {
        public ConcurrentQueue<string> Received { get; } = new();
    }

    public class NamedNoticeHandler : PostMessageHandlerBase<NamedNotice>
    {
        private readonly NamedInbox _inbox;

        public NamedNoticeHandler(NamedInbox inbox)
        {
            _inbox = inbox;
        }

        public override Task Handle(NamedNotice message)
        {
            _inbox.Received.Enqueue(message.Text);
            return Task.CompletedTask;
        }
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

        await _server.StopAsync();
        await _server.DisposeAsync();
    }

    [Fact]
    public void NamedClient_DoesNotReplaceTheDefaultClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddThargaCommunicationClient(options: o => o.ServerAddress = "https://default:5001");
        services.AddThargaCommunicationClient(ClientName, o => o.ServerAddress = "https://named:5001");

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<ClientOptions>>().Value.ServerAddress.Should().Be("https://default:5001");
        provider.GetRequiredKeyedService<IOptions<ClientOptions>>(ClientName).Value.ServerAddress.Should().Be("https://named:5001");
        provider.GetRequiredKeyedService<IClientCommunication>(ClientName).Should().NotBeSameAs(provider.GetRequiredService<IClientCommunication>());
        provider.GetRequiredKeyedService<ISignalRHostedService>(ClientName).Should().NotBeSameAs(provider.GetRequiredService<ISignalRHostedService>());
    }

    [Fact]
    public void NamedClient_BindsItsOwnConfigurationSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Tharga:Communication:ServerAddress"] = "https://default:5001",
                ["Tharga:Communication:other:ServerAddress"] = "https://named:5001",
                ["Tharga:Communication:other:ApiKey"] = "named-key"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddThargaCommunicationClient(configuration);
        services.AddThargaCommunicationClient(ClientName, configuration);

        using var provider = services.BuildServiceProvider();

        var named = provider.GetRequiredKeyedService<IOptions<ClientOptions>>(ClientName).Value;
        named.ServerAddress.Should().Be("https://named:5001");
        named.ApiKey.Should().Be("named-key");
        provider.GetRequiredService<IOptions<ClientOptions>>().Value.ApiKey.Should().BeNull();
    }

    [Fact]
    public void NamedClient_RegisteredTwiceWithTheSameName_Throws()
    {
        var services = new ServiceCollection();
        services.AddThargaCommunicationClient(ClientName, o => o.ServerAddress = "https://named:5001");

        var act = () => services.AddThargaCommunicationClient(ClientName, o => o.ServerAddress = "https://again:5001");

        act.Should().Throw<InvalidOperationException>().WithMessage("*'other'*");
    }

    [Fact]
    public void NamedClient_WithoutName_Throws()
    {
        var services = new ServiceCollection();

        var act = () => services.AddThargaCommunicationClient(" ", o => o.ServerAddress = "https://named:5001");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NamedClient_ScansOnlyTheLibraryAndItsAdditionalAssemblies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddThargaCommunicationClient(options: o => o.ServerAddress = "https://default:5001");
        services.AddThargaCommunicationClient(ClientName, o => o.ServerAddress = "https://named:5001");
        services.AddThargaCommunicationClient("with-handlers", o =>
        {
            o.ServerAddress = "https://named:5001";
            o.AdditionalAssemblies = [GetType().Assembly];
        });

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IHandlerTypeService>().TryGetHandler(typeof(NamedNotice), out _).Should().BeTrue();
        provider.GetRequiredKeyedService<IHandlerTypeService>(ClientName).TryGetHandler(typeof(NamedNotice), out _).Should().BeFalse();
        provider.GetRequiredKeyedService<IHandlerTypeService>("with-handlers").TryGetHandler(typeof(NamedNotice), out _).Should().BeTrue();
    }

    [Fact]
    public async Task NamedClient_ReceivesMessagesOnItsOwnConnection()
    {
        await StartClientsAsync(namedHandlers: true);
        var inbox = _client.Services.GetRequiredService<NamedInbox>();
        var server = _server.Services.GetRequiredService<IServerCommunication>();

        await server.PostAsync(await GetConnectionIdAsync(NamedMachine), new NamedNotice("for the named client"));

        await WaitForAsync(inbox, 1);
        inbox.Received.Should().Equal("for the named client");
    }

    [Fact]
    public async Task NamedClient_WithoutHandlers_DoesNotRunTheApplicationsHandlers()
    {
        await StartClientsAsync(namedHandlers: false);
        var inbox = _client.Services.GetRequiredService<NamedInbox>();
        var server = _server.Services.GetRequiredService<IServerCommunication>();

        await server.PostAsync(await GetConnectionIdAsync(NamedMachine), new NamedNotice("for the named client"));

        await Task.Delay(QuietPeriod, TestContext.Current.CancellationToken);
        inbox.Received.Should().BeEmpty();
    }

    [Fact]
    public async Task EachClient_TracksItsOwnConnectionAndSubscriptionState()
    {
        await StartClientsAsync(namedHandlers: false);
        var defaultClient = _client.Services.GetRequiredService<IClientCommunication>();
        var namedClient = _client.Services.GetRequiredKeyedService<IClientCommunication>(ClientName);
        var namedChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        namedClient.SubscriptionChanged += (_, _) => namedChanged.TrySetResult();
        var server = _server.Services.GetRequiredService<IServerCommunication>();

        await using var subscription = await server.SubscribeAsync<NamedTopic>();

        await namedChanged.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        namedClient.IsConnected.Should().BeTrue();
        defaultClient.IsConnected.Should().BeTrue();
        namedClient.HasSubscribers<NamedTopic>().Should().BeTrue();
    }

    [Fact]
    public async Task EachClient_RaisesItsOwnConnectionStateEvents()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.AddThargaCommunicationClient(o =>
        {
            o.ServerAddress = _server.Urls.First();
            o.ClientMachine = DefaultMachine;
        });
        builder.AddThargaCommunicationClient(ClientName, o =>
        {
            o.ServerAddress = _server.Urls.First();
            o.ClientMachine = NamedMachine;
        });
        _client = builder.Build();
        var defaultEvents = new ConcurrentQueue<HubConnectionState>();
        var namedEvents = new ConcurrentQueue<HubConnectionState>();
        _client.Services.GetRequiredService<ISignalRHostedService>().HubConnectionStateChangedEvent += (_, e) => defaultEvents.Enqueue(e.After);
        _client.Services.GetRequiredKeyedService<ISignalRHostedService>(ClientName).HubConnectionStateChangedEvent += (_, e) => namedEvents.Enqueue(e.After);

        await _client.StartAsync(TestContext.Current.CancellationToken);
        await GetConnectionIdAsync(DefaultMachine);
        await GetConnectionIdAsync(NamedMachine);

        defaultEvents.Should().Contain(HubConnectionState.Connected);
        namedEvents.Should().Contain(HubConnectionState.Connected);
    }

    private static async Task WaitForAsync(NamedInbox inbox, int count)
    {
        using var limit = new CancellationTokenSource(WaitLimit);
        while (inbox.Received.Count < count)
        {
            await Task.Delay(20, limit.Token);
        }
    }

    private async Task StartClientsAsync(bool namedHandlers)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.AddThargaCommunicationClient(o =>
        {
            o.ServerAddress = _server.Urls.First();
            o.ClientMachine = DefaultMachine;
        });
        builder.AddThargaCommunicationClient(ClientName, o =>
        {
            o.ServerAddress = _server.Urls.First();
            o.ClientMachine = NamedMachine;
            if (namedHandlers) o.AdditionalAssemblies = [GetType().Assembly];
        });
        builder.Services.AddSingleton<NamedInbox>();

        _client = builder.Build();
        await _client.StartAsync(TestContext.Current.CancellationToken);
        await GetConnectionIdAsync(DefaultMachine);
        await GetConnectionIdAsync(NamedMachine);
    }

    private async Task<string> GetConnectionIdAsync(string machine)
    {
        var stateService = _server.Services.GetRequiredService<IntegrationTests.TestClientStateService>();
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        limit.CancelAfter(WaitLimit);
        while (true)
        {
            await foreach (var connection in stateService.GetAsync())
            {
                if (connection.Machine == machine) return connection.ConnectionId;
            }

            await Task.Delay(20, limit.Token);
        }
    }
}
