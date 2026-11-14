# Tharga Communication

[![GitHub repo](https://img.shields.io/github/repo-size/Tharga/Communication?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Tharga/Communication)

A SignalR-based communication framework for .NET with built-in message handler patterns for request-response and fire-and-forget messaging. Supports .NET 10 and .NET 11.

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

Every client option except `AdditionalAssemblies` can be set in this section, `ApiKey` included. An options callback passed to `AddThargaCommunicationClient` runs after the section is read, so a value set in code wins.

A host that has no `IHostApplicationBuilder`, and a library that registers its own services, can register on the `IServiceCollection` instead. No service provider is built during registration:

```csharp
services.AddThargaCommunicationClient(configuration, o => o.ApiKey = "my-secret-key");
```

### Named clients

An application can hold more than one connection, for example when a library needs its own server next to the application's. A named client has its own options, connection, reconnect state, subscription state and handlers. The unnamed registration stays the default client and is not affected.

```csharp
services.AddThargaCommunicationClient("quilt4net", configuration, o =>
{
    o.ServerAddress = "https://quilt4net.example";
    o.ApiKey = "...";
    o.AdditionalAssemblies = [typeof(SomeLibraryHandler).Assembly];
});

public MyService([FromKeyedServices("quilt4net")] IClientCommunication client) { }
```

Each name binds its own section, `Tharga:Communication:<name>`. `ISignalRHostedService` (connection state events) and `IHandlerTypeService` are also keyed by name. A named client looks for handlers only in the Tharga.Communication assembly and its `AdditionalAssemblies`, not in the application's own assemblies, so its handlers do not run for the default client's messages. The default client's scan covers the application's assemblies, so avoid giving a library handler the same message type as one the application scans.

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

The validator decides everything — including whether to accept empty keys (return `IsValid = true, KeyId = null` to allow anonymous), or how to identify the matched key. `KeyId` and `KeyName` flow through to `IClientConnectionInfo`, where consumers can use them for admin UIs and audit logs. A validator can also return `Metadata` (a string dictionary, for example the tenant that owns the key). It reaches `ClientConnection.Metadata`, `IClientConnectionInfo.Metadata` and the `Build` method of `ClientStateServiceBase<T>`, and is `null` when the validator supplies none.

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

Each message is handled in its own dependency injection scope, so handlers can depend on scoped services.

When a request-response message fails, the server's `SendMessageAsync` returns a failed `Response` with `Code` `TIMEOUT`, `DISCONNECTED` or `REMOTE_ERROR`, and the client's `SendMessage` throws `TimeoutException` or `RemoteRequestException`. Changed in 1.2: a failed handler used to come back as a successful response with default values.

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

A client that connects or reconnects is sent the subscriptions that are already active, so it sees subscriptions made before it connected.

For full documentation and examples, see the [GitHub repository](https://github.com/Tharga/Communication).
