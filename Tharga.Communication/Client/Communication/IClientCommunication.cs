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
    /// <returns>The response from the server.</returns>
    Task<TResponse> SendMessage<TRequest, TResponse>(TRequest message);

    /// <summary>
    /// Gets whether the client is currently connected to the server.
    /// </summary>
    bool IsConnected { get; }
}