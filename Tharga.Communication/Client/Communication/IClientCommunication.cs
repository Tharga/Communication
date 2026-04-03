using Tharga.Communication.Contract;

namespace Tharga.Communication.Client.Communication;

/// <summary>
/// Provides client-side communication with a SignalR server.
/// </summary>
public interface IClientCommunication
{
    /// <summary>
    /// Sends a fire-and-forget message to the server.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message payload to send.</param>
    Task PostAsync<T>(T message);

    /// <summary>
    /// Sends a request to the server and waits for a typed response.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="message">The request payload.</param>
    /// <param name="timeout">Optional timeout. Defaults to <see cref="CommunicationOptions.SendMessageTimeout"/> or 60 seconds.</param>
    /// <returns>The response from the server.</returns>
    Task<TResponse> SendMessage<TRequest, TResponse>(TRequest message, TimeSpan? timeout = null);

    /// <summary>
    /// Gets whether the client is currently connected to the server.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Checks whether there are active subscribers on the server for the given message type,
    /// optionally scoped to a specific key. A wildcard (keyless) server subscription matches any key.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="key">Optional data key. When <c>null</c>, checks for a type-level subscription.</param>
    bool HasSubscribers<T>(string key = null);

    /// <summary>
    /// Sends a fire-and-forget message to the server only if there are active subscribers.
    /// No-ops when no subscribers are present.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message payload to send.</param>
    /// <param name="key">Optional data key to check against subscriptions.</param>
    /// <returns><c>true</c> if the message was sent; <c>false</c> if skipped.</returns>
    Task<bool> PostIfSubscribedAsync<T>(T message, string key = null);

    /// <summary>
    /// Raised when the server notifies this client of a subscription state change.
    /// </summary>
    event EventHandler<SubscriptionStateChanged> SubscriptionChanged;
}