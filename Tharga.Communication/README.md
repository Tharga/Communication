# Tharga Communication

[![GitHub repo](https://img.shields.io/github/repo-size/Tharga/Communication?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Tharga/Communication)

A SignalR-based communication framework for .NET with built-in message handler patterns for request-response and fire-and-forget messaging.

## Features

- Fire-and-forget and request-response messaging patterns
- Automatic message handler discovery via dependency injection
- Client connection tracking with metadata
- Automatic reconnection with configurable delays
- Extensible client state storage

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

For full documentation and examples, see the [GitHub repository](https://github.com/Tharga/Communication).
