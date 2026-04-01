using FluentAssertions;
using Tharga.Communication.Server;
using Xunit;

namespace Tharga.Communication.Tests;

public class SubscriptionManagerTests
{
    private readonly SubscriptionManager _sut = new();

    [Fact]
    public void Subscribe_FirstSubscriber_ReturnsTrue()
    {
        var crossed = _sut.Subscribe("FarmDetailsDto", "1");

        crossed.Should().BeTrue();
    }

    [Fact]
    public void Subscribe_SecondSubscriber_ReturnsFalse()
    {
        _sut.Subscribe("FarmDetailsDto", "1");

        var crossed = _sut.Subscribe("FarmDetailsDto", "1");

        crossed.Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_LastSubscriber_ReturnsTrue()
    {
        _sut.Subscribe("FarmDetailsDto", "1");

        var crossed = _sut.Unsubscribe("FarmDetailsDto", "1");

        crossed.Should().BeTrue();
    }

    [Fact]
    public void Unsubscribe_StillHasSubscribers_ReturnsFalse()
    {
        _sut.Subscribe("FarmDetailsDto", "1");
        _sut.Subscribe("FarmDetailsDto", "1");

        var crossed = _sut.Unsubscribe("FarmDetailsDto", "1");

        crossed.Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_NeverSubscribed_ReturnsTrue()
    {
        var crossed = _sut.Unsubscribe("FarmDetailsDto", "1");

        crossed.Should().BeTrue();
    }

    [Fact]
    public void Unsubscribe_DoesNotGoNegative()
    {
        _sut.Unsubscribe("FarmDetailsDto", "1");
        _sut.Subscribe("FarmDetailsDto", "1");

        var crossed = _sut.Subscribe("FarmDetailsDto", "1");

        crossed.Should().BeFalse("count should be 2 after subscribe, not crossing 0→1 again");
    }

    [Fact]
    public void Subscribe_WithoutKey_TracksAsSeparateTopic()
    {
        var wildcardCrossed = _sut.Subscribe("FarmDetailsDto");
        var keyedCrossed = _sut.Subscribe("FarmDetailsDto", "1");

        wildcardCrossed.Should().BeTrue();
        keyedCrossed.Should().BeTrue("keyed and wildcard are separate entries");
    }

    [Fact]
    public void GetSubscriptions_ReturnsActiveTopicsAndCounts()
    {
        _sut.Subscribe("FarmDetailsDto", "1");
        _sut.Subscribe("FarmDetailsDto", "1");
        _sut.Subscribe("CollectionDto");

        var subs = _sut.GetSubscriptions();

        subs.Should().HaveCount(2);
        subs["FarmDetailsDto:1"].Should().Be(2);
        subs["CollectionDto"].Should().Be(1);
    }

    [Fact]
    public void GetSubscriptions_ExcludesRemovedTopics()
    {
        _sut.Subscribe("FarmDetailsDto", "1");
        _sut.Unsubscribe("FarmDetailsDto", "1");

        var subs = _sut.GetSubscriptions();

        subs.Should().BeEmpty();
    }

    [Fact]
    public void BuildKey_WithoutKey_ReturnsTopicOnly()
    {
        var key = SubscriptionManager.BuildKey("FarmDetailsDto", null);

        key.Should().Be("FarmDetailsDto");
    }

    [Fact]
    public void BuildKey_WithKey_ReturnsCombined()
    {
        var key = SubscriptionManager.BuildKey("FarmDetailsDto", "42");

        key.Should().Be("FarmDetailsDto:42");
    }
}
