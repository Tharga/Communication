using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;
using Tharga.Communication.Contract;

namespace Tharga.Communication.Client.Communication;

internal class ClientCommunication : IClientCommunication
{
    private readonly ISignalRHostedService _signalRConnectionState;
    private readonly SubscriptionStateTracker _subscriptionStateTracker;

    public ClientCommunication(ISignalRHostedService signalRConnectionState, SubscriptionStateTracker subscriptionStateTracker)
    {
        _signalRConnectionState = signalRConnectionState;
        _subscriptionStateTracker = subscriptionStateTracker;
        _subscriptionStateTracker.SubscriptionChanged += (_, e) => SubscriptionChanged?.Invoke(this, e);
    }

    public event EventHandler<SubscriptionStateChanged> SubscriptionChanged;

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

    public bool HasSubscribers<T>(string key = null)
    {
        return _subscriptionStateTracker.HasSubscribers(typeof(T).FullName!, key);
    }

    public async Task<bool> PostIfSubscribedAsync<T>(T message, string key = null)
    {
        if (!HasSubscribers<T>(key))
            return false;

        await PostAsync(message);
        return true;
    }
}