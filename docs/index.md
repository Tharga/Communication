---
_layout: landing
---

# Tharga.Communication

SignalR-based client/server communication framework for .NET with built-in message handler patterns for request-response and fire-and-forget messaging, plus a pluggable API key validator and subscription-based messaging. Built for **.NET 8 / 9**.

## Packages

| Package | What it does |
|---|---|
| [Tharga.Communication](https://www.nuget.org/packages/Tharga.Communication) | SignalR transport, message handlers, subscriptions, and pluggable API key validation. |
| [Tharga.Communication.Mcp](https://www.nuget.org/packages/Tharga.Communication.Mcp) | Exposes Communication runtime data (connected clients, active subscriptions, registered handlers) as MCP resources. Plugs into [Tharga.Mcp](https://mcp.tharga.net). |

## Quick start

```
dotnet add package Tharga.Communication
```

```csharp
// Server
builder.AddThargaCommunicationServer(options =>
{
    options.RegisterClientStateService<MyClientStateService>();
    options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
});

app.UseThargaCommunicationServer();
```

```csharp
// Client
builder.AddThargaCommunicationClient();
```

See [Getting started](articles/getting-started.md) for the full walkthrough.

## What's in the box

- **Messaging** — `PostAsync` (fire-and-forget) and `SendMessage` (request-response), routed to handlers via DI. See [Messaging](articles/messaging.md).
- **Subscriptions** — server signals clients when consumers are active, with type-based and data-based granularity. Clients can skip work when nobody is watching. See [Subscriptions](articles/subscriptions.md).
- **Authentication** — pluggable `IApiKeyValidator` with a default keys-array implementation; consumers can swap in Platform-backed lookups. Client identity (`ClientType`, `ClientMachine`) can be overridden. See [Authentication](articles/authentication.md).

## Repo

[github.com/Tharga/Communication](https://github.com/Tharga/Communication) — source, issues, releases.
