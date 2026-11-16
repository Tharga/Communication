using AwesomeAssertions;
using Tharga.Communication.Client;
using Tharga.Communication.Contract;
using Xunit;

namespace Tharga.Communication.Tests;

public class SubscriptionStateTrackerTests
{
    private readonly SubscriptionStateTracker _sut = new();

    private static SubscriptionSnapshot Snapshot(params (string Topic, string Key)[] active) => new()
    {
        Subscriptions = active.Select(x => new SubscriptionStateChanged { Topic = x.Topic, Key = x.Key, HasSubscribers = true }).ToArray()
    };

    [Fact]
    public void Replace_DropsSubscriptionsMissingFromTheSnapshot()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "X", HasSubscribers = true });
        _sut.Update(new SubscriptionStateChanged { Topic = "Y", Key = "1", HasSubscribers = true });

        _sut.Replace(Snapshot(("Y", "1")));

        _sut.HasSubscribers("X").Should().BeFalse();
        _sut.HasSubscribers("Y", "1").Should().BeTrue();
    }

    [Fact]
    public void Replace_AddsSubscriptionsFromTheSnapshot()
    {
        _sut.Replace(Snapshot(("X", null)));

        _sut.HasSubscribers("X").Should().BeTrue();
    }

    [Fact]
    public void Replace_EmptySnapshot_ClearsEverything()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "X", HasSubscribers = true });

        _sut.Replace(Snapshot());

        _sut.HasSubscribers("X").Should().BeFalse();
    }

    [Fact]
    public void Replace_RaisesSubscriptionChangedOnlyForWhatDiffers()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "Kept", HasSubscribers = true });
        _sut.Update(new SubscriptionStateChanged { Topic = "Gone", HasSubscribers = true });
        var raised = new List<(string Topic, bool HasSubscribers)>();
        _sut.SubscriptionChanged += (_, e) => raised.Add((e.Topic, e.HasSubscribers));

        _sut.Replace(Snapshot(("Kept", null), ("New", null)));

        raised.Should().BeEquivalentTo(new[] { ("Gone", false), ("New", true) });
    }

    [Fact]
    public void Update_WithoutASnapshot_KeepsEarlierSubscriptions()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "X", HasSubscribers = true });
        _sut.Update(new SubscriptionStateChanged { Topic = "Y", HasSubscribers = true });

        _sut.HasSubscribers("X").Should().BeTrue();
        _sut.HasSubscribers("Y").Should().BeTrue();
    }

    [Fact]
    public async Task SnapshotHandler_ReplacesTheTrackerState()
    {
        _sut.Update(new SubscriptionStateChanged { Topic = "X", HasSubscribers = true });

        await new SubscriptionSnapshotHandler(_sut).Handle(Snapshot());

        _sut.HasSubscribers("X").Should().BeFalse();
    }

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
