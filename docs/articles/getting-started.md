# Getting started

Tharga.Communication ships a server and a client that talk over a SignalR hub. The server is a regular ASP.NET Core host; the client is any .NET host (console, worker, Blazor, etc.).

## Install

```
dotnet add package Tharga.Communication
```

For MCP introspection (connected clients, subscriptions, handlers) also add:

```
dotnet add package Tharga.Communication.Mcp
```

## Server

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

`AddThargaCommunicationServer` wires the SignalR hub, message handler dispatch, and the default API key validator. `UseThargaCommunicationServer` maps the hub endpoint (default `/hub`).

You must provide:
- A client state service (subclass `ClientStateServiceBase<T>`) — tracks connected clients
- A client repository (use `MemoryClientRepository<T>` or your own backing store)

## Client

In `appsettings.json`:

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
var builder = Host.CreateApplicationBuilder(args);
builder.AddThargaCommunicationClient();
builder.Build().Run();
```

Configuration values can also be supplied via the options callback:

```csharp
builder.AddThargaCommunicationClient(o =>
{
    o.ServerAddress = "https://localhost:5001";
    o.ApiKey = "my-secret-key";
});
```

The client maintains a persistent SignalR connection with automatic reconnection.

## Next steps

- [Messaging](messaging.md) — send and handle messages
- [Subscriptions](subscriptions.md) — skip work when nobody is consuming
- [Authentication](authentication.md) — API keys and custom validators
