using Microsoft.AspNetCore.SignalR.Client;

namespace Tharga.Communication.Client;

/// <summary>
/// Manages the SignalR hub connection as a hosted background service.
/// </summary>
public interface ISignalRHostedService
{
    /// <summary>
    /// Raised when the hub connection state changes (e.g. Disconnected to Connected).
    /// </summary>
    event EventHandler<HubConnectionStateChangedEventArgs> HubConnectionStateChangedEvent;

    /// <summary>Gets the current state of the hub connection.</summary>
    HubConnectionState State { get; }

    /// <summary>
    /// Sends a message to the server hub.
    /// </summary>
    /// <param name="methodName">The hub method name to invoke.</param>
    /// <param name="payload">The serialized payload to send.</param>
    Task SendAsync(string methodName, object payload);
}