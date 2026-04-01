namespace Tharga.Communication.Contract;

/// <summary>
/// Message pushed from server to clients when subscription state crosses the 0↔1 boundary.
/// </summary>
public record SubscriptionStateChanged
{
    /// <summary>The full type name of the subscribed message type.</summary>
    public required string Topic { get; init; }

    /// <summary>The optional data key within the topic, or <c>null</c> for a type-level (wildcard) subscription.</summary>
    public string Key { get; init; }

    /// <summary>Whether at least one subscriber is now active for this topic/key combination.</summary>
    public required bool HasSubscribers { get; init; }
}
