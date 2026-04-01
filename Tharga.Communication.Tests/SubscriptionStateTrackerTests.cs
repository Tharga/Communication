using FluentAssertions;
using Tharga.Communication.Client;
using Tharga.Communication.Contract;
using Xunit;

namespace Tharga.Communication.Tests;

public class SubscriptionStateTrackerTests
{
    private readonly SubscriptionStateTracker _sut = new();

    [Fact]
    public void HasSubscribers_NoSubscriptions_ReturnsFalse()
    {
        _sut.HasSubscribers("FarmDetailsDto", "1").Should().BeFalse();
    }

    [Fact]
    public void HasSubscribers_ExactKeyMatch_ReturnsTrue()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "FarmDetailsDto", Key = "1", HasSubscribers = true });

        _sut.HasSubscribers("FarmDetailsDto", "1").Should().BeTrue();
    }

    [Fact]
    public void HasSubscribers_DifferentKey_ReturnsFalse()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "FarmDetailsDto", Key = "1", HasSubscribers = true });

        _sut.HasSubscribers("FarmDetailsDto", "2").Should().BeFalse();
    }

    [Fact]
    public void HasSubscribers_WildcardSubscription_MatchesAnyKey()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "FarmDetailsDto", HasSubscribers = true });

        _sut.HasSubscribers("FarmDetailsDto", "1").Should().BeTrue();
        _sut.HasSubscribers("FarmDetailsDto", "99").Should().BeTrue();
    }

    [Fact]
    public void HasSubscribers_WildcardSubscription_MatchesNoKey()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "FarmDetailsDto", HasSubscribers = true });

        _sut.HasSubscribers("FarmDetailsDto").Should().BeTrue();
    }

    [Fact]
    public void HasSubscribers_KeyedSubscription_DoesNotMatchWildcardQuery()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "FarmDetailsDto", Key = "1", HasSubscribers = true });

        _sut.HasSubscribers("FarmDetailsDto").Should().BeFalse();
    }

    [Fact]
    public void HasSubscribers_AfterUnsubscribe_ReturnsFalse()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "FarmDetailsDto", Key = "1", HasSubscribers = true });
        _sut.Update(new SubscriptionStateChanged { Topic = "FarmDetailsDto", Key = "1", HasSubscribers = false });

        _sut.HasSubscribers("FarmDetailsDto", "1").Should().BeFalse();
    }

    [Fact]
    public void HasSubscribers_DifferentTopics_AreIndependent()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "FarmDetailsDto", Key = "1", HasSubscribers = true });

        _sut.HasSubscribers("CollectionDto", "1").Should().BeFalse();
    }

    [Fact]
    public void SubscriptionChanged_RaisedOnUpdate()
    {
        SubscriptionStateChanged received = null;
        _sut.SubscriptionChanged += (_, e) => received = e;

        var message = new SubscriptionStateChanged { Topic = "FarmDetailsDto", Key = "1", HasSubscribers = true };
        _sut.Update(message);

        received.Should().BeEquivalentTo(message);
    }

    [Fact]
    public void SubscriptionChanged_RaisedOnUnsubscribe()
    {
        SubscriptionStateChanged received = null;
        _sut.SubscriptionChanged += (_, e) => received = e;

        var message = new SubscriptionStateChanged { Topic = "FarmDetailsDto", Key = "1", HasSubscribers = false };
        _sut.Update(message);

        received.Should().NotBeNull();
        received.HasSubscribers.Should().BeFalse();
    }
}
