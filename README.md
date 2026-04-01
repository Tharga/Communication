# Tharga Communication

[![GitHub repo Issues](https://img.shields.io/github/issues/Tharga/Communication?style=flat&logo=github&logoColor=red&label=Issues)](https://github.com/Tharga/Communication/issues?q=is%3Aopen)
[![NuGet](https://img.shields.io/nuget/v/Tharga.Communication)](https://www.nuget.org/packages/Tharga.Communication)
![Nuget](https://img.shields.io/nuget/dt/Tharga.Communication)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A SignalR-based communication framework for .NET with built-in message handler patterns for request-response and fire-and-forget messaging between clients and servers.

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

### Server options

The server requires registering a `ClientStateServiceBase` implementation and a `ClientRepositoryBase` implementation via the options callback. Use `MemoryClientRepository<T>` for an in-memory default.

| Property | Description | Default |
|---|---|---|
| `PrimaryApiKey` | Primary API key for client authentication | *(none)* |
| `SecondaryApiKey` | Secondary API key for zero-downtime key rotation | *(none)* |

When no API keys are configured on the server, all connections are accepted (backwards compatible). When one or both keys are set, clients must provide a matching key via the `X-Api-Key` header.

## Authentication

To secure the SignalR connection with API key authentication:

**Server** — configure one or both keys:

```csharp
builder.AddThargaCommunicationServer(options =>
{
    options.PrimaryApiKey = builder.Configuration["Communication:PrimaryApiKey"];
    options.SecondaryApiKey = builder.Configuration["Communication:SecondaryApiKey"];
    options.RegisterClientStateService<MyClientStateService>();
    options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
});
```

**Client** — provide the matching key:

```json
{
  "Tharga": {
    "Communication": {
      "ServerAddress": "https://localhost:5001",
      "ApiKey": "your-secret-key"
    }
  }
}
```

Or via the options callback:

```csharp
builder.AddThargaCommunicationClient(o =>
{
    o.ApiKey = builder.Configuration["Communication:ApiKey"];
});
```

API keys can also be configured via User Secrets or environment variables. To rotate keys without downtime, set both `PrimaryApiKey` and `SecondaryApiKey` on the server — either key is accepted.

## License

[MIT](LICENSE)
