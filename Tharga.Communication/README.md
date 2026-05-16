# Tharga Communication

[![GitHub repo](https://img.shields.io/github/repo-size/Tharga/Communication?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Tharga/Communication)

A SignalR-based communication framework for .NET with built-in message handler patterns for request-response and fire-and-forget messaging.

## Features

- Fire-and-forget and request-response messaging patterns
- Automatic message handler discovery via dependency injection
- Client connection tracking with metadata
- Automatic reconnection with configurable delays
- Extensible client state storage
- Pluggable API key validation with custom validators
- Subscription-based messaging with type and data-level granularity

## Getting started

### Server

```csharp
builder.AddThargaCommunicationServer(options =>
{
    options.RegisterClientStateService<MyClientStateService>();
    options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
});

app.UseThargaCommunicationServer();
```

### Client

Add to `appsettings.json`:

```json
{
  "Tharga": {
    "Communication": {
      "ServerAddress": "https://localhost:5001"
    }
  }
}
```

```csharp
builder.AddThargaCommunicationClient();
```

### API key validation

By default, the server accepts all connections. To require API keys, set `ApiKeys` on the server options:

```csharp
builder.AddThargaCommunicationServer(options =>
{
    options.ApiKeys = ["my-secret-key", "rotation-key"];
    options.RegisterClientStateService<MyClientStateService>();
    options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
});
```

Clients send the key via `CommunicationOptions.ApiKey` (or `appsettings.json`):

```csharp
builder.AddThargaCommunicationClient(o => o.ApiKey = "my-secret-key");
```

#### Custom validators

For more advanced scenarios (per-key lookup in a database, per-IP allowlists, integration with Tharga.Platform's API key management, etc.), register an `IApiKeyValidator`:

```csharp
public class MyApiKeyValidator : IApiKeyValidator
{
    private readonly IApiKeyAdministrationService _keys;

    public MyApiKeyValidator(IApiKeyAdministrationService keys) => _keys = keys;

    public async Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken ct = default)
    {
        var key = await _keys.GetByValueAsync(apiKey, ct);
        return key is null
            ? new() { IsValid = false }
            : new() { IsValid = true, KeyId = key.Id.ToString(), KeyName = key.Name };
    }
}

builder.AddThargaCommunicationServer(options =>
{
    options.RegisterApiKeyValidator<MyApiKeyValidator>();
    // …
});
```

The validator decides everything — including whether to accept empty keys (return `IsValid = true, KeyId = null` to allow anonymous), or how to identify the matched key. `KeyId` and `KeyName` flow through to `IClientConnectionInfo`, where consumers can use them for admin UIs and audit logs.

> **Need HTTP context?** Inject `IHttpContextAccessor` into your validator if you need access to the request (IP allowlists, custom headers, etc.). The framework intentionally does not pass `HttpContext` to keep `IApiKeyValidator` narrow and testable.

### Client identity

The client sends four self-reported identity headers during connection: `Instance` (Guid generated per process run), `Machine`, `Type` (assembly name), and `Version`. Two of those — `Machine` and `Type` — can be overridden via options:

```csharp
builder.AddThargaCommunicationClient(o =>
{
    o.ServerAddress = "https://localhost:5001";
    o.ClientType = "monitor-agent";        // override; defaults to entry assembly name
    o.ClientMachine = "us-east-prod-1";    // override; defaults to Environment.MachineName
});
```

Useful when one assembly hosts multiple roles, or when containerized hosts have meaningless hash-based hostnames.

### Message handlers

```csharp
// Fire-and-forget
public class MyHandler : PostMessageHandlerBase<MyMessage>
{
    public override Task Handle(MyMessage message) => Task.CompletedTask;
}

// Request-response
public class PingHandler : SendMessageHandlerBase<PingRequest, PingResponse>
{
    public override Task<PingResponse> Handle(PingRequest message) =>
        Task.FromResult(new PingResponse("Pong"));
}
```

### Subscription messaging

Subscriptions allow the server to signal clients whether anyone is consuming a particular message type, so clients can skip sending data when no dashboard or consumer is active.

#### Server side (consumer/dashboard)

```csharp
// Type-based: subscribe to all messages of a type
await using var sub = await serverCommunication.SubscribeAsync<CollectionDto>();

// Data-based: subscribe to a specific entity
await using var sub = await serverCommunication.SubscribeAsync<FarmDetailsDto>(farmId.ToString());

// Monitor active subscriptions
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

#### Client side (agent/producer)

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

#### Matching rules

- **Type-based** (`SubscribeAsync<T>()` without key): wildcard — `HasSubscribers<T>("anyKey")` returns `true`.
- **Data-based** (`SubscribeAsync<T>("1")` with key): specific — only `HasSubscribers<T>("1")` returns `true`.

For full documentation and examples, see the [GitHub repository](https://github.com/Tharga/Communication).
