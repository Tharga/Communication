using System.Collections.Concurrent;
using Tharga.Communication.Contract;

namespace Tharga.Communication.Client;

/// <summary>
/// Client-side mirror of active server subscriptions.
/// Updated when <see cref="SubscriptionStateChanged"/> messages arrive from the server.
/// </summary>
internal class SubscriptionStateTracker
{
    private readonly ConcurrentDictionary<string, bool> _activeTopics = new();

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

        if (message.HasSubscribers)
        {
            _activeTopics[key] = true;
        }
        else
        {
            _activeTopics.TryRemove(key, out _);
        }

        SubscriptionChanged?.Invoke(this, message);
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
