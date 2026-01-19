using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;
using Tharga.Communication.Contract;

namespace Tharga.Communication.Client.Communication;

internal class ClientCommunication : IClientCommunication
{
    private readonly ISignalRHostedService _signalRConnectionState;

    public ClientCommunication(ISignalRHostedService signalRConnectionState)
    {
        _signalRConnectionState = signalRConnectionState;
    }

    public Task PostAsync<T>(T message)
    {
        var wrapper = new RequestWrapper
        {
            Type = typeof(T).AssemblyQualifiedName,
            Payload = System.Text.Json.JsonSerializer.Serialize(message)
        };
        return _signalRConnectionState.SendAsync(Constants.PostMessage, wrapper);
    }

    public Task<TResponse> SendMessage<TRequest, TResponse>(TRequest message)
    {
        Debugger.Break();
        throw new NotImplementedException();
    }

    public bool IsConnected => _signalRConnectionState.State == HubConnectionState.Connected;
}