# Tharga Communication

[![GitHub repo Issues](https://img.shields.io/github/issues/Tharga/Communication?style=flat&logo=github&logoColor=red&label=Issues)](https://github.com/Tharga/Communication/issues?q=is%3Aopen)
[![NuGet](https://img.shields.io/nuget/v/Tharga.Communication)](https://www.nuget.org/packages/Tharga.Communication)
![Nuget](https://img.shields.io/nuget/dt/Tharga.Communication)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A SignalR-based communication framework for .NET with built-in message handler patterns for request-response and fire-and-forget messaging between clients and servers.

**Docs:** [communication.tharga.net](https://communication.tharga.net) — guides, API reference, and walkthroughs for messaging, subscriptions, and authentication.

## Features

- **Fire-and-forget messaging** - Send one-way messages from client to server or server to client(s)
- **Request-response messaging** - Send a request and await a typed response with configurable timeout
- **Automatic handler discovery** - Message handlers are discovered and registered via dependency injection
- **Client connection tracking** - Track connected clients with metadata (machine name, app type, version)
- **Automatic reconnection** - Configurable reconnect delays for client connections
- **Extensible storage** - Abstract repository pattern for client state with an in-memory default

## Installation

```
dotnet add package Tharga.Communication
```

## Quick start

### Server setup

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddThargaCommunicationServer(options =>
{
    options.RegisterClientStateService<MyClientStateService>();
    options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
});

var app = builder.Build();
app.UseThargaCommunicationServer();
app.Run();
```

### Client setup

Add the configuration section to `appsettings.json`:

```json
{
  "Tharga": {
    "Communication": {
      "ServerAddress": "https://localhost:5001"
    }
  }
}
```

Register the client services:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.AddThargaCommunicationClient();
```

### Creating a message handler (fire-and-forget)

```csharp
public record MyNotification(string Text);

public class MyNotificationHandler : PostMessageHandlerBase<MyNotification>
{
    public override Task Handle(MyNotification message)
    {
        Console.WriteLine(message.Text);
        return Task.CompletedTask;
    }
}
```

Register the handler in DI:

```csharp
builder.Services.AddTransient<PostMessageHandlerBase<MyNotification>, MyNotificationHandler>();
```

### Creating a message handler (request-response)

```csharp
public record PingRequest(string Message);
public record PingResponse(string Reply);

public class PingHandler : SendMessageHandlerBase<PingRequest, PingResponse>
{
    public override Task<PingResponse> Handle(PingRequest message)
    {
        return Task.FromResult(new PingResponse($"Pong: {message.Message}"));
    }
}
```

### Sending messages

From the client:

```csharp
public class MyService(IClientCommunication client)
{
    public async Task NotifyServer()
    {
        await client.PostAsync(new MyNotification("Hello from client"));
    }

    public async Task<PingResponse> PingServer()
    {
        return await client.SendMessage<PingRequest, PingResponse>(new PingRequest("Ping"));
    }
}
```

From the server:

```csharp
public class MyServerService(IServerCommunication server)
{
    public async Task NotifyClient(string connectionId)
    {
        await server.PostAsync(connectionId, new MyNotification("Hello from server"));
    }

    public async Task NotifyAll()
    {
        await server.PostToAllAsync(new MyNotification("Broadcast message"));
    }

    public async Task<PingResponse> PingClient(string connectionId)
    {
        var response = await server.SendMessageAsync<PingRequest, PingResponse>(
            connectionId, new PingRequest("Ping"));
        return response.Value;
    }
}
```

### Implementing a client state service

```csharp
public class MyClientStateService : ClientStateServiceBase<ClientConnectionInfo>
{
    public MyClientStateService(IServiceProvider sp, IOptions<CommunicationOptions> options)
        : base(sp, options) { }

    protected override ClientConnectionInfo Build(IClientConnectionInfo info) =>
        new()
        {
            Instance = info.Instance,
            ConnectionId = info.ConnectionId,
            Machine = info.Machine,
            Type = info.Type,
            Version = info.Version,
            IsConnected = info.IsConnected,
            ConnectTime = info.ConnectTime
        };

    protected override ClientConnectionInfo BuildDisconnect(ClientConnectionInfo info, DateTime disconnectTime) =>
        info with { IsConnected = false, DisconnectTime = disconnectTime };
}
```

## Configuration

### Client options

| Property | Description | Default |
|---|---|---|
| `ServerAddress` | The server URL to connect to | *(required)* |
| `Pattern` | The hub endpoint pattern | `"hub"` |
| `ReconnectDelays` | Delays between reconnection attempts | `[0s, 2s, 10s, 30s]` |
| `ApiKey` | API key sent to the server for authentication | *(none)* |
| `AdditionalAssemblies` | Extra assemblies to scan for message handlers | *(none)* |
| `SendMessageTimeout` | Default timeout for request-response messages | `60s` |

### Server options

The server requires registering a `ClientStateServiceBase` implementation and a `ClientRepositoryBase` implementation via the options callback. Use `MemoryClientRepository<T>` for an in-memory default.

| Property | Description | Default |
|---|---|---|
| `ApiKeys` | Keys accepted by the default `IApiKeyValidator` | *(none — all connections accepted)* |
| `AdditionalAssemblies` | Extra assemblies to scan for message handlers | *(none)* |

## Authentication

The server uses a pluggable `IApiKeyValidator`. The default implementation checks the connecting key against `CommunicationOptions.ApiKeys`. Custom validators (e.g. delegating to a Platform-backed API key store) can be registered via `options.RegisterApiKeyValidator<T>()`.

See [Authentication](https://communication.tharga.net/articles/authentication.html) on the docs site for the full walkthrough, including a Platform-backed validator example and the `IHttpContextAccessor` injection hint.

## Handler discovery

By default, message handlers are discovered by scanning assemblies that match the entry assembly name prefix. If your handlers are in an external package (e.g. a separate NuGet), they won't be found automatically. Use `AdditionalAssemblies` to include them:

```csharp
builder.AddThargaCommunicationClient(o =>
{
    o.ServerAddress = "https://localhost:5001";
    o.AdditionalAssemblies = [typeof(MyExternalHandler).Assembly];
});
```

The same option is available on the server side:

```csharp
builder.AddThargaCommunicationServer(options =>
{
    options.AdditionalAssemblies = [typeof(MyExternalHandler).Assembly];
    options.RegisterClientStateService<MyClientStateService>();
    options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
});
```

If a client receives a `SendMessage` for a type with no registered handler, it immediately returns an error response to the server instead of silently timing out.

## License

[MIT](LICENSE)
