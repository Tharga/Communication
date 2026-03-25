using Microsoft.AspNetCore.SignalR.Client;

namespace Tharga.Communication.Client;

/// <summary>
/// Event arguments for hub connection state transitions.
/// </summary>
public class HubConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance with the previous and new connection states.
    /// </summary>
    /// <param name="before">The connection state before the change.</param>
    /// <param name="after">The connection state after the change.</param>
    public HubConnectionStateChangedEventArgs(HubConnectionState before, HubConnectionState after)
    {
        Before = before;
        After = after;
    }

    /// <summary>Gets the connection state before the change.</summary>
    public HubConnectionState Before { get; }

    /// <summary>Gets the connection state after the change.</summary>
    public HubConnectionState After { get; }
}