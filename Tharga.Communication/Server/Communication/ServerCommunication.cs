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
    // Held across the announcement too, so clients receive announcements in the order of the changes.
    private readonly SemaphoreSlim _subscriptionLock = new(1, 1);

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
        var pending = (tcs, cd);
        if (!_pendingRequests.TryAdd(connectionId, pending)) throw new InvalidOperationException($"A request is already pending for connection '{connectionId}'.");

        try
        {
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
                _logger.LogWarning("Timeout waiting for response from agent '{connectionId}' after {timeout}.", connectionId, effectiveTimeout);

                return Response<TResponse>.Fail("TIMEOUT", $"No response received within {effectiveTimeout.TotalSeconds} seconds.");
            }

            if (tcs.Task.IsCanceled)
            {
                return Response<TResponse>.Fail("DISCONNECTED", $"Client '{connectionId}' disconnected before it responded.");
            }

            var responseWrapper = await tcs.Task;
            var value = JsonSerializer.Deserialize<TResponse>(responseWrapper.Payload);

            return new Response<TResponse>(value);
        }
        finally
        {
            if (_pendingRequests.TryRemove(new KeyValuePair<string, (TaskCompletionSource<IMessageWrapper>, DateTime)>(connectionId, pending)))
            {
                PendingRequestEvent?.Invoke(this, new PendingRequestEventArgs(connectionId, false, cd));
            }
        }
    }

    internal void OnDisconnected(string connectionId)
    {
        if (_pendingRequests.TryGetValue(connectionId, out var pending))
        {
            pending.Request.TrySetCanceled();
        }
    }

    public Dictionary<string, DateTime> GetPendingAsync()
    {
        return _pendingRequests.ToDictionary(x => x.Key, x => x.Value.CreateDate);
    }

    public async Task<IAsyncDisposable> SubscribeAsync<T>(string key = null)
    {
        var topic = typeof(T).FullName!;
        await ChangeSubscriptionAsync(() => _subscriptionManager.Subscribe(topic, key), topic, key, true);
        return new SubscriptionHandle(this, topic, key);
    }

    public IReadOnlyDictionary<string, int> GetSubscriptions()
    {
        return _subscriptionManager.GetSubscriptions();
    }

    /// <summary>
    /// Tells one client about every subscription that is currently active.
    /// Uses the same <see cref="SubscriptionStateChanged"/> message as the 0↔1 notifications,
    /// so clients of any version apply it.
    /// </summary>
    internal async Task SendActiveSubscriptionsAsync(string connectionId)
    {
        foreach (var (topic, key) in _subscriptionManager.GetActiveTopics())
        {
            await PostAsync(connectionId, new SubscriptionStateChanged { Topic = topic, Key = key, HasSubscribers = true });
        }
    }

    internal Task UnsubscribeAsync(string topic, string key)
    {
        return ChangeSubscriptionAsync(() => _subscriptionManager.Unsubscribe(topic, key), topic, key, false);
    }

    private async Task ChangeSubscriptionAsync(Func<bool> crossesZero, string topic, string key, bool hasSubscribers)
    {
        await _subscriptionLock.WaitAsync();
        try
        {
            if (crossesZero())
            {
                await PostToAllAsync(new SubscriptionStateChanged { Topic = topic, Key = key, HasSubscribers = hasSubscribers });
            }
        }
        finally
        {
            _subscriptionLock.Release();
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
