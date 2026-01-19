using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Features.Measure;
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
    private readonly ConcurrentDictionary<string, (TaskCompletionSource<IMessageWrapper> Request, DateTime CreateDate)> _pendingRequests = new();

    public ServerCommunication(IMessageExecutor messageExecutor, IHubContext<SignalRHub> signalRHub, ILogger<ServerCommunication> logger)
    {
        _messageExecutor = messageExecutor;
        _signalRHub = signalRHub;
        _logger = logger;

        //_signalRHub.ResponseEvent += OnResponseEvent;
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
        return await _logger.MeasureAsync($"{nameof(ServerCommunication)}.{nameof(SendMessageAsync)}", async s =>
        {
            var effectiveTimeout = timeout ?? _defaultTimeout;
            var tcs = new TaskCompletionSource<IMessageWrapper>(TaskCreationOptions.RunContinuationsAsynchronously);

            var cd = DateTime.UtcNow;
            if (!_pendingRequests.TryAdd(connectionId, (tcs, cd)))
            {
                //NOTE: Event thgough it look slike the agent is idle, there might be a pending request. Here is why...
                //When a request is timed out, or cancelled. The agent gets the information to abort the request and sends back a response to the web-server.
                //My quess is that the request is still pending and waiting for a response. (If there is handling for this, there might be a slight gap between the TIMEOUT and the cancellation request to the agent and the response back from the agent.)
                throw new InvalidOperationException($"A request is already pending for connection '{connectionId}'.");
            }
            else
            {
                PendingRequestEvent?.Invoke(this, new PendingRequestEventArgs(connectionId, true, cd));
            }

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
        });
    }

    public Dictionary<string, DateTime> GetPendingAsync()
    {
        return _pendingRequests.ToDictionary(x => x.Key, x => x.Value.CreateDate);
    }
}
