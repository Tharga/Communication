using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;

namespace Tharga.Communication.Server.Communication;

internal class ServerCommunication : IServerCommunication
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(60);

    private readonly IMessageExecutor _messageExecutor;
    private readonly IHubContext<SignalRHub> _signalRHub;
    private readonly ILogger<ServerCommunication> _logger;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly ConcurrentDictionary<string, (TaskCompletionSource<IMessageWrapper> Request, DateTime CreateDate)> _pendingRequests = new();

    public ServerCommunication(IMessageExecutor messageExecutor, IHubContext<SignalRHub> signalRHub, SubscriptionManager subscriptionManager, ILogger<ServerCommunication> logger)
    {
        _messageExecutor = messageExecutor;
        _signalRHub = signalRHub;
        _subscriptionManager = subscriptionManager;
        _logger = logger;
    }

    public event EventHandler<PendingRequestEventArgs> PendingRequestEvent;

    internal void OnResponseEvent(object sender, ResponseEventArgs e)
    {
        if (_pendingRequests.TryRemove(e.ConnectionId, out var tcs))
        {
            tcs.Request.TrySetResult(e.Response);
            PendingRequestEvent?.Invoke(this, new PendingRequestEventArgs(e.ConnectionId, false, tcs.CreateDate));
        }
        else
        {
            _messageExecutor.ExecuteAsync(e.ConnectionId, e.Response);
        }
    }

    public Task PostAsync<T>(string connectionId, T message)
    {
        var requestWrapper = new RequestWrapper
        {
            Type = typeof(T).AssemblyQualifiedName,
            Payload = JsonSerializer.Serialize(message)
        };

        return _signalRHub.Clients.Client(connectionId)
            .SendAsync(Constants.PostMessage, requestWrapper);
    }

    public Task PostToAllAsync<T>(T message)
    {
        var requestWrapper = new RequestWrapper
        {
            Type = typeof(T).AssemblyQualifiedName,
            Payload = JsonSerializer.Serialize(message)
        };

        return _signalRHub.Clients.All
            .SendAsync(Constants.PostMessage, requestWrapper);
    }

    public async Task<Response<TResponse>> SendMessageAsync<TRequest, TResponse>(string connectionId, TRequest message, TimeSpan? timeout)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var tcs = new TaskCompletionSource<IMessageWrapper>(TaskCreationOptions.RunContinuationsAsynchronously);

        var cd = DateTime.UtcNow;
        if (!_pendingRequests.TryAdd(connectionId, (tcs, cd))) throw new InvalidOperationException($"A request is already pending for connection '{connectionId}'.");

        PendingRequestEvent?.Invoke(this, new PendingRequestEventArgs(connectionId, true, cd));

        var requestWrapper = new RequestWrapper
        {
            Type = typeof(TRequest).AssemblyQualifiedName,
            Payload = JsonSerializer.Serialize(message)
        };

        await _signalRHub.Clients.Client(connectionId).SendAsync(Constants.SendMessage, requestWrapper);

        using var cts = new CancellationTokenSource(effectiveTimeout);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));

        if (completedTask != tcs.Task)
        {
            if (_pendingRequests.TryRemove(connectionId, out var tcsx))
            {
                PendingRequestEvent?.Invoke(this, new PendingRequestEventArgs(connectionId, false, tcsx.CreateDate));
            }

            _logger.LogWarning("Timeout waiting for response from agent '{connectionId}' after {timeout}.", connectionId, effectiveTimeout);

            return Response<TResponse>.Fail("TIMEOUT", $"No response received within {effectiveTimeout.TotalSeconds} seconds.");
        }

        var responseWrapper = await tcs.Task;
        var value = JsonSerializer.Deserialize<TResponse>(responseWrapper.Payload);

        return new Response<TResponse>(value);
    }

    public Dictionary<string, DateTime> GetPendingAsync()
    {
        return _pendingRequests.ToDictionary(x => x.Key, x => x.Value.CreateDate);
    }

    public async Task<IAsyncDisposable> SubscribeAsync<T>(string key = null)
    {
        var topic = typeof(T).FullName!;
        var isFirst = _subscriptionManager.Subscribe(topic, key);

        if (isFirst)
        {
            await PostToAllAsync(new SubscriptionStateChanged { Topic = topic, Key = key, HasSubscribers = true });
        }

        return new SubscriptionHandle(this, topic, key);
    }

    public IReadOnlyDictionary<string, int> GetSubscriptions()
    {
        return _subscriptionManager.GetSubscriptions();
    }

    internal async Task UnsubscribeAsync(string topic, string key)
    {
        var isLast = _subscriptionManager.Unsubscribe(topic, key);

        if (isLast)
        {
            await PostToAllAsync(new SubscriptionStateChanged { Topic = topic, Key = key, HasSubscribers = false });
        }
    }

    private sealed class SubscriptionHandle : IAsyncDisposable
    {
        private readonly ServerCommunication _owner;
        private readonly string _topic;
        private readonly string _key;
        private int _disposed;

        public SubscriptionHandle(ServerCommunication owner, string topic, string key)
        {
            _owner = owner;
            _topic = topic;
            _key = key;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                await _owner.UnsubscribeAsync(_topic, _key);
            }
        }
    }
}
