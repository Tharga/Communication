using Microsoft.AspNetCore.SignalR.Client;

namespace Tharga.Communication.Client;

public class HubConnectionStateChangedEventArgs : EventArgs
{
    public HubConnectionStateChangedEventArgs(HubConnectionState before, HubConnectionState after)
    {
        Before = before;
        After = after;
    }

    public HubConnectionState Before { get; }
    public HubConnectionState After { get; }
}