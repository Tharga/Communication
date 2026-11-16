using System.Collections.Concurrent;
using Tharga.Communication.Contract;

namespace Tharga.Communication.Client;

/// <summary>
/// Client-side mirror of active server subscriptions.
/// Updated when <see cref="SubscriptionStateChanged"/> messages arrive from the server.
/// </summary>
internal class SubscriptionStateTracker
{
    private readonly ConcurrentDictionary<string, SubscriptionStateChanged> _activeTopics = new();
    private readonly object _writeLock = new();

    /// <summary>
    /// Raised when any subscription state changes.
    /// </summary>
    public event EventHandler<SubscriptionStateChanged> SubscriptionChanged;

    /// <summary>
    /// Updates the local state from a server notification.
    /// </summary>
    public void Update(SubscriptionStateChanged message)
    {
        var key = BuildKey(message.Topic, message.Key);

        lock (_writeLock)
        {
            if (message.HasSubscribers)
            {
                _activeTopics[key] = message;
            }
            else
            {
                _activeTopics.TryRemove(key, out _);
            }
        }

        SubscriptionChanged?.Invoke(this, message);
    }

    /// <summary>
    /// Replaces the local state with the subscriptions the server reports as active.
    /// Raises <see cref="SubscriptionChanged"/> for every topic whose state differs afterwards.
    /// </summary>
    public void Replace(SubscriptionSnapshot snapshot)
    {
        var active = new Dictionary<string, SubscriptionStateChanged>();
        foreach (var subscription in snapshot.Subscriptions ?? [])
        {
            active[BuildKey(subscription.Topic, subscription.Key)] = subscription with { HasSubscribers = true };
        }

        var changes = new List<SubscriptionStateChanged>();
        lock (_writeLock)
        {
            foreach (var key in _activeTopics.Keys.Where(x => !active.ContainsKey(x)).ToArray())
            {
                if (_activeTopics.TryRemove(key, out var removed)) changes.Add(removed with { HasSubscribers = false });
            }

            foreach (var (key, subscription) in active)
            {
                if (_activeTopics.TryAdd(key, subscription)) changes.Add(subscription);
            }
        }

        foreach (var change in changes)
        {
            SubscriptionChanged?.Invoke(this, change);
        }
    }

    /// <summary>
    /// Checks whether there are active subscribers for the given type/key.
    /// A wildcard (keyless) subscription for the topic matches any key.
    /// </summary>
    public bool HasSubscribers(string topic, string key = null)
    {
        // Check for an exact match (topic:key or just topic)
        var exactKey = BuildKey(topic, key);
        if (_activeTopics.ContainsKey(exactKey))
            return true;

        // If checking with a specific key, also check for a wildcard subscription (topic without key)
        if (key is not null && _activeTopics.ContainsKey(topic))
            return true;

        return false;
    }

    internal static string BuildKey(string topic, string key)
    {
        return key is null ? topic : $"{topic}:{key}";
    }
}
