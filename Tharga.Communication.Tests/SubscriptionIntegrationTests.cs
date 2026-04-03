using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;
using Tharga.Communication.Client;
using Tharga.Communication.Client.Communication;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;
using Tharga.Communication.Server;
using Tharga.Communication.Server.Communication;
using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Tharga.Communication.Tests;

/// <summary>
/// Tests the full subscription flow: server subscribes → notification captured → fed to client tracker → client checks.
/// </summary>
public class SubscriptionIntegrationTests
{
    private readonly ServerCommunication _server;
    private readonly ClientCommunication _client;
    private readonly SubscriptionStateTracker _tracker;
    private readonly SubscriptionStateChangedHandler _handler;
    private readonly List<SubscriptionStateChanged> _notifications = new();

    public SubscriptionIntegrationTests()
    {
        // Server side
        var serverMocker = new AutoMocker();
        serverMocker.Use(new SubscriptionManager());

        var allClients = new Mock<IClientProxy>();
        allClients
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((method, args, _) =>
            {
                if (method == Constants.PostMessage && args.Length == 1 && args[0] is RequestWrapper wrapper)
                {
                    var message = JsonSerializer.Deserialize<SubscriptionStateChanged>(wrapper.Payload);
                    if (message != null)
                    {
                        _notifications.Add(message);
                        // Simulate client receiving the message
                        _handler.Handle(message);
                    }
                }
            })
            .Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(x => x.All).Returns(allClients.Object);

        var hubContext = new Mock<IHubContext<SignalRHub>>();
        hubContext.Setup(x => x.Clients).Returns(hubClients.Object);
        serverMocker.Use(hubContext.Object);

        _server = serverMocker.CreateInstance<ServerCommunication>();

        // Client side
        _tracker = new SubscriptionStateTracker();
        _handler = new SubscriptionStateChangedHandler(_tracker);

        var signalR = new Mock<ISignalRHostedService>();
        signalR.Setup(x => x.State).Returns(HubConnectionState.Connected);
        _client = new ClientCommunication(signalR.Object, new ClientResponseMediator(), _tracker, Options.Create(new Tharga.Communication.Client.CommunicationOptions()), Microsoft.Extensions.Logging.Abstractions.NullLogger<ClientCommunication>.Instance);
    }

    [Fact]
    public async Task FullLifecycle_SubscribeAndUnsubscribe()
    {
        // Initially no subscribers
        _client.HasSubscribers<FarmDetails>("1").Should().BeFalse();

        // Server subscribes
        var handle = await _server.SubscribeAsync<FarmDetails>("1");

        // Client should now see subscribers
        _client.HasSubscribers<FarmDetails>("1").Should().BeTrue();
        _notifications.Should().HaveCount(1);
        _notifications[0].HasSubscribers.Should().BeTrue();

        // Server unsubscribes
        await handle.DisposeAsync();

        // Client should see no subscribers
        _client.HasSubscribers<FarmDetails>("1").Should().BeFalse();
        _notifications.Should().HaveCount(2);
        _notifications[1].HasSubscribers.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleSubscribers_ClientSeesSubscribersUntilAllGone()
    {
        var handle1 = await _server.SubscribeAsync<FarmDetails>("1");
        var handle2 = await _server.SubscribeAsync<FarmDetails>("1");

        // Only one notification (first subscriber)
        _notifications.Should().HaveCount(1);
        _client.HasSubscribers<FarmDetails>("1").Should().BeTrue();

        // Remove one — still has subscribers, no notification
        await handle1.DisposeAsync();
        _notifications.Should().HaveCount(1);
        _client.HasSubscribers<FarmDetails>("1").Should().BeTrue();

        // Remove last — notification sent
        await handle2.DisposeAsync();
        _notifications.Should().HaveCount(2);
        _client.HasSubscribers<FarmDetails>("1").Should().BeFalse();
    }

    [Fact]
    public async Task WildcardSubscription_ClientMatchesAnyKey()
    {
        await _server.SubscribeAsync<FarmDetails>();

        _client.HasSubscribers<FarmDetails>("1").Should().BeTrue();
        _client.HasSubscribers<FarmDetails>("99").Should().BeTrue();
        _client.HasSubscribers<FarmDetails>().Should().BeTrue();
    }

    [Fact]
    public async Task KeyedSubscription_ClientDoesNotMatchOtherKeys()
    {
        await _server.SubscribeAsync<FarmDetails>("1");

        _client.HasSubscribers<FarmDetails>("1").Should().BeTrue();
        _client.HasSubscribers<FarmDetails>("2").Should().BeFalse();
        _client.HasSubscribers<FarmDetails>().Should().BeFalse();
    }

    [Fact]
    public async Task GetSubscriptions_ShowsActiveMonitoringData()
    {
        await _server.SubscribeAsync<FarmDetails>("1");
        await _server.SubscribeAsync<FarmDetails>("1");
        await _server.SubscribeAsync<CollectionData>();

        var subs = _server.GetSubscriptions();

        subs.Should().HaveCount(2);
        subs[$"{typeof(FarmDetails).FullName}:1"].Should().Be(2);
        subs[typeof(CollectionData).FullName!].Should().Be(1);
    }

    [Fact]
    public async Task NavigationScenario_SwitchFromFarm1ToFarm2()
    {
        // Open Farm 1 page
        var farm1Sub = await _server.SubscribeAsync<FarmDetails>("1");
        _client.HasSubscribers<FarmDetails>("1").Should().BeTrue();
        _client.HasSubscribers<FarmDetails>("2").Should().BeFalse();

        // Navigate to Farm 2 — dispose old, create new
        await farm1Sub.DisposeAsync();
        var farm2Sub = await _server.SubscribeAsync<FarmDetails>("2");

        _client.HasSubscribers<FarmDetails>("1").Should().BeFalse();
        _client.HasSubscribers<FarmDetails>("2").Should().BeTrue();

        await farm2Sub.DisposeAsync();
    }

    [Fact]
    public async Task SubscriptionChanged_EventFiredOnClient()
    {
        var received = new List<SubscriptionStateChanged>();
        _client.SubscriptionChanged += (_, e) => received.Add(e);

        var handle = await _server.SubscribeAsync<FarmDetails>("1");
        await handle.DisposeAsync();

        received.Should().HaveCount(2);
        received[0].HasSubscribers.Should().BeTrue();
        received[1].HasSubscribers.Should().BeFalse();
    }

    public record FarmDetails;
    public record CollectionData;
}
