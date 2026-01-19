using Microsoft.AspNetCore.SignalR.Client;

namespace Tharga.Communication.Client;

public interface ISignalRHostedService
{
    event EventHandler<HubConnectionStateChangedEventArgs> HubConnectionStateChangedEvent;
    HubConnectionState State { get; }
    Task SendAsync(string methodName, object payload);
}