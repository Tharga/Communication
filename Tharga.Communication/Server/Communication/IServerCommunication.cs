namespace Tharga.Communication.Server.Communication;

/// <summary>
/// Provides server-side communication with connected SignalR clients.
/// </summary>
public interface IServerCommunication
{
    /// <summary>
    /// Raised when a pending request is added or removed.
    /// </summary>
    event EventHandler<PendingRequestEventArgs> PendingRequestEvent;

    /// <summary>
    /// Sends a fire-and-forget message to a specific client.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="connectionId">The SignalR connection ID of the target client.</param>
    /// <param name="message">The message payload to send.</param>
    Task PostAsync<T>(string connectionId, T message);

    /// <summary>
    /// Broadcasts a fire-and-forget message to all connected clients.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message payload to broadcast.</param>
    Task PostToAllAsync<T>(T message);

    /// <summary>
    /// Sends a request to a specific client and waits for a typed response.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="connectionId">The SignalR connection ID of the target client.</param>
    /// <param name="message">The request payload.</param>
    /// <param name="timeout">Optional timeout. Defaults to 60 seconds.</param>
    /// <returns>A <see cref="Response{T}"/> containing the result or failure information.</returns>
    Task<Response<TResponse>> SendMessageAsync<TRequest, TResponse>(string connectionId, TRequest message, TimeSpan? timeout = null);

    /// <summary>
    /// Gets a dictionary of pending requests, keyed by connection ID with the request creation time.
    /// </summary>
    Dictionary<string, DateTime> GetPendingAsync();
}