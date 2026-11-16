namespace Tharga.Communication.Contract;

/// <summary>
/// Message pushed from server to a client when it connects, listing every subscription that is active at that moment.
/// A client that understands it replaces what it knows with this list, so subscriptions that ended while the client was away are dropped.
/// </summary>
public record SubscriptionSnapshot
{
    /// <summary>The active subscriptions. Every entry has <see cref="SubscriptionStateChanged.HasSubscribers"/> set to <c>true</c>.</summary>
    public required IReadOnlyList<SubscriptionStateChanged> Subscriptions { get; init; }
}
