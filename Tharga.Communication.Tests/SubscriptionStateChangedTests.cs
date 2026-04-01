using System.Text.Json;
using FluentAssertions;
using Tharga.Communication.Contract;
using Xunit;

namespace Tharga.Communication.Tests;

public class SubscriptionStateChangedTests
{
    [Fact]
    public void RoundTrip_WithKey_PreservesAllProperties()
    {
        var original = new SubscriptionStateChanged
        {
            Topic = "FarmDetailsDto",
            Key = "1",
            HasSubscribers = true
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<SubscriptionStateChanged>(json);

        deserialized.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void RoundTrip_WithoutKey_PreservesNullKey()
    {
        var original = new SubscriptionStateChanged
        {
            Topic = "CollectionDto",
            HasSubscribers = false
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<SubscriptionStateChanged>(json);

        deserialized.Should().BeEquivalentTo(original);
        deserialized.Key.Should().BeNull();
    }

    [Fact]
    public void CanWrap_InRequestWrapper()
    {
        var message = new SubscriptionStateChanged
        {
            Topic = "FarmDetailsDto",
            Key = "42",
            HasSubscribers = true
        };

        var wrapper = new RequestWrapper
        {
            Type = typeof(SubscriptionStateChanged).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(message)
        };

        var payload = JsonSerializer.Deserialize<SubscriptionStateChanged>(wrapper.Payload);

        payload.Should().BeEquivalentTo(message);
    }
}
