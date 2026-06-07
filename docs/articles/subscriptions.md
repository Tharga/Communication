# Subscriptions

Subscriptions let the server signal clients whether anyone is consuming a particular message type. Clients can skip work — serializing payloads, walking caches, generating reports — when no dashboard or other consumer is active.

## Server side (consumer/dashboard)

```csharp
// Type-based: subscribe to all messages of a type
await using var sub = await serverCommunication.SubscribeAsync<CollectionDto>();

// Data-based: subscribe to a specific entity
await using var sub = await serverCommunication.SubscribeAsync<FarmDetailsDto>(farmId.ToString());

// Monitor active subscriptions (diagnostics / admin UI)
IReadOnlyDictionary<string, int> active = serverCommunication.GetSubscriptions();
```

In Blazor, tie the subscription to the page lifecycle:

```csharp
@implements IAsyncDisposable
@inject IServerCommunication ServerCommunication

@code {
    private IAsyncDisposable? _subscription;

    protected override async Task OnInitializedAsync()
    {
        _subscription = await ServerCommunication.SubscribeAsync<FarmDetailsDto>(FarmId.ToString());
    }

    public async ValueTask DisposeAsync()
    {
        if (_subscription != null) await _subscription.DisposeAsync();
    }
}
```

## Client side (agent/producer)

```csharp
// Check before sending
if (clientCommunication.HasSubscribers<FarmDetailsDto>(farmId.ToString()))
    await clientCommunication.PostAsync(farmDetails);

// Or use the convenience method (no-ops when no subscribers)
await clientCommunication.PostIfSubscribedAsync(farmDetails, farmId.ToString());

// React to subscription changes
clientCommunication.SubscriptionChanged += (sender, e) =>
{
    Console.WriteLine($"{e.Topic}:{e.Key} → {(e.HasSubscribers ? "active" : "inactive")}");
};
```

## Matching rules

- **Type-based** (`SubscribeAsync<T>()` without key) is a wildcard — `HasSubscribers<T>("anyKey")` returns `true`.
- **Data-based** (`SubscribeAsync<T>("1")` with key) is specific — only `HasSubscribers<T>("1")` returns `true`.

Multiple subscribers to the same topic/key are reference-counted; the server only signals clients on the 0↔1 transition.

## When to use which

Use type-based subscriptions when a dashboard shows aggregated data across all entities of a type (e.g. "all collections"). Use data-based subscriptions when a dashboard shows one entity (e.g. "this farm"). Clients can use both flavors of `HasSubscribers` to decide what to send.
