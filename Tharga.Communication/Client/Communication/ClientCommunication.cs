using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tharga.Communication.Client;
using Tharga.Communication.Contract;

namespace Tharga.Communication.Client.Communication;

internal class ClientCommunication : IClientCommunication
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    private readonly ISignalRHostedService _signalRConnectionState;
    private readonly SubscriptionStateTracker _subscriptionStateTracker;
    private readonly CommunicationOptions _options;
    private readonly ILogger<ClientCommunication> _logger;

    private volatile TaskCompletionSource<IMessageWrapper> _pendingRequest;
    private readonly object _sendLock = new();

    public ClientCommunication(ISignalRHostedService signalRConnectionState, ClientResponseMediator responseMediator, SubscriptionStateTracker subscriptionStateTracker, IOptions<CommunicationOptions> options, ILogger<ClientCommunication> logger)
    {
        _signalRConnectionState = signalRConnectionState;
        _subscriptionStateTracker = subscriptionStateTracker;
        _options = options.Value;
        _logger = logger;
        _subscriptionStateTracker.SubscriptionChanged += (_, e) => SubscriptionChanged?.Invoke(this, e);
        responseMediator.ResponseReceived += (_, wrapper) => OnResponse(wrapper);
    }

    public event EventHandler<SubscriptionStateChanged> SubscriptionChanged;

    public Task PostAsync<T>(T message)
    {
        var wrapper = new RequestWrapper
        {
            Type = typeof(T).AssemblyQualifiedName,
            Payload = JsonSerializer.Serialize(message)
        };
        return _signalRConnectionState.SendAsync(Constants.PostMessage, wrapper);
    }

    public async Task<TResponse> SendMessage<TRequest, TResponse>(TRequest message, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _options.SendMessageTimeout;
        if (effectiveTimeout == default) effectiveTimeout = DefaultTimeout;

        var tcs = new TaskCompletionSource<IMessageWrapper>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_sendLock)
        {
            if (_pendingRequest != null)
                throw new InvalidOperationException("A request is already pending. Only one SendMessage can be in-flight at a time.");
            _pendingRequest = tcs;
        }

        try
        {
            var requestWrapper = new RequestWrapper
            {
                Type = typeof(TRequest).AssemblyQualifiedName,
                Payload = JsonSerializer.Serialize(message)
            };

            await _signalRConnectionState.SendAsync(Constants.SendMessage, requestWrapper);

            using var cts = new CancellationTokenSource(effectiveTimeout);
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));

            if (completedTask != tcs.Task)
            {
                _logger.LogWarning("Timeout waiting for response from server after {Timeout}.", effectiveTimeout);
                throw new TimeoutException($"No response received from server within {effectiveTimeout.TotalSeconds} seconds.");
            }

            var responseWrapper = await tcs.Task;
            return JsonSerializer.Deserialize<TResponse>(responseWrapper.Payload);
        }
        finally
        {
            lock (_sendLock)
            {
                _pendingRequest = null;
            }
        }
    }

    internal void OnResponse(IMessageWrapper response)
    {
        var tcs = _pendingRequest;
        if (tcs != null)
        {
            tcs.TrySetResult(response);
        }
        else
        {
            _logger.LogWarning("Received response but no request is pending.");
        }
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
