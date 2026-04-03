using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Tharga.Communication.Client;
using Tharga.Communication.Client.Communication;
using Tharga.Communication.Contract;
using Xunit;
using ClientOptions = Tharga.Communication.Client.CommunicationOptions;

namespace Tharga.Communication.Tests;

public class ClientCommunicationSubscriptionTests
{
    private readonly SubscriptionStateTracker _tracker = new();
    private readonly Mock<ISignalRHostedService> _signalR = new();
    private readonly ClientCommunication _sut;

    public ClientCommunicationSubscriptionTests()
    {
        _signalR.Setup(x => x.State).Returns(HubConnectionState.Connected);
        _sut = new ClientCommunication(_signalR.Object, new ClientResponseMediator(), _tracker, Options.Create(new ClientOptions()), NullLogger<ClientCommunication>.Instance);
    }

    [Fact]
    public void HasSubscribers_NoSubscriptions_ReturnsFalse()
    {
        _sut.HasSubscribers<FakeDto>("1").Should().BeFalse();
    }

    [Fact]
    public void HasSubscribers_AfterServerNotification_ReturnsTrue()
    {
        _tracker.Update(new SubscriptionStateChanged
        {
            Topic = typeof(FakeDto).FullName!,
            Key = "1",
            HasSubscribers = true
        });

        _sut.HasSubscribers<FakeDto>("1").Should().BeTrue();
    }

    [Fact]
    public void HasSubscribers_WildcardSubscription_MatchesSpecificKey()
    {
        _tracker.Update(new SubscriptionStateChanged
        {
            Topic = typeof(FakeDto).FullName!,
            HasSubscribers = true
        });

        _sut.HasSubscribers<FakeDto>("42").Should().BeTrue();
    }

    [Fact]
    public async Task PostIfSubscribedAsync_NoSubscribers_DoesNotSend()
    {
        var sent = await _sut.PostIfSubscribedAsync(new FakeDto(), "1");

        sent.Should().BeFalse();
        _signalR.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task PostIfSubscribedAsync_HasSubscribers_Sends()
    {
        _tracker.Update(new SubscriptionStateChanged
        {
            Topic = typeof(FakeDto).FullName!,
            Key = "1",
            HasSubscribers = true
        });

        var sent = await _sut.PostIfSubscribedAsync(new FakeDto(), "1");

        sent.Should().BeTrue();
        _signalR.Verify(x => x.SendAsync(Constants.PostMessage, It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public void SubscriptionChanged_RaisedWhenTrackerUpdates()
    {
        SubscriptionStateChanged received = null;
        _sut.SubscriptionChanged += (_, e) => received = e;

        _tracker.Update(new SubscriptionStateChanged
        {
            Topic = typeof(FakeDto).FullName!,
            Key = "1",
            HasSubscribers = true
        });

        received.Should().NotBeNull();
        received.HasSubscribers.Should().BeTrue();
    }

    public record FakeDto;
}
