using FluentAssertions;
using Tharga.Communication.Client;
using Tharga.Communication.Contract;
using Xunit;

namespace Tharga.Communication.Tests;

public class SubscriptionStateChangedHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesTracker()
    {
        var tracker = new SubscriptionStateTracker();
        var handler = new SubscriptionStateChangedHandler(tracker);

        await handler.Handle(new SubscriptionStateChanged
        {
            Topic = "FarmDetailsDto",
            Key = "1",
            HasSubscribers = true
        });

        tracker.HasSubscribers("FarmDetailsDto", "1").Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Unsubscribe_UpdatesTracker()
    {
        var tracker = new SubscriptionStateTracker();
        var handler = new SubscriptionStateChangedHandler(tracker);

        await handler.Handle(new SubscriptionStateChanged
        {
            Topic = "FarmDetailsDto",
            Key = "1",
            HasSubscribers = true
        });

        await handler.Handle(new SubscriptionStateChanged
        {
            Topic = "FarmDetailsDto",
            Key = "1",
            HasSubscribers = false
        });

        tracker.HasSubscribers("FarmDetailsDto", "1").Should().BeFalse();
    }
}
