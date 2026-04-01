using System.Collections.Concurrent;

namespace Tharga.Communication.Server;

/// <summary>
/// Tracks subscription reference counts per topic/key combination.
/// Thread-safe for concurrent subscribe/unsubscribe calls.
/// </summary>
internal class SubscriptionManager
{
    private readonly ConcurrentDictionary<string, int> _subscriptions = new();

    /// <summary>
    /// Increments the subscriber count for the given topic/key.
    /// </summary>
    /// <returns><c>true</c> if the count crossed from 0 to 1 (first subscriber).</returns>
    public bool Subscribe(string topic, string key = null)
    {
        var subscriptionKey = BuildKey(topic, key);
        var newCount = _subscriptions.AddOrUpdate(subscriptionKey, 1, (_, count) => count + 1);
        return newCount == 1;
    }

    /// <summary>
    /// Decrements the subscriber count for the given topic/key.
    /// </summary>
    /// <returns><c>true</c> if the count crossed from 1 to 0 (last subscriber left).</returns>
    public bool Unsubscribe(string topic, string key = null)
    {
        var subscriptionKey = BuildKey(topic, key);
        var newCount = _subscriptions.AddOrUpdate(subscriptionKey, 0, (_, count) => Math.Max(0, count - 1));
        if (newCount == 0)
        {
            _subscriptions.TryRemove(subscriptionKey, out _);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns a snapshot of all active subscriptions and their counts.
    /// </summary>
    public IReadOnlyDictionary<string, int> GetSubscriptions()
    {
        return _subscriptions.ToDictionary(x => x.Key, x => x.Value);
    }

    internal static string BuildKey(string topic, string key)
    {
        return key is null ? topic : $"{topic}:{key}";
    }
}
