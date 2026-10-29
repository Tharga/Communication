# Messaging

Tharga.Communication supports two patterns: **fire-and-forget** (`PostAsync`) and **request-response** (`SendMessage`).

## Fire-and-forget

The sender doesn't wait for a result.

```csharp
// Client → Server
await clientCommunication.PostAsync(new TemperatureReading(22.4));

// Server → specific client
await serverCommunication.PostAsync(connectionId, new ConfigChanged());

// Server → all connected clients
await serverCommunication.PostToAllAsync(new MaintenanceWindow(start, end));
```

## Request-response

The sender waits for a typed response with a configurable timeout (default 60s).

```csharp
// Server → client, expecting a response
var response = await serverCommunication.SendMessageAsync<PingRequest, PingResponse>(
    connectionId,
    new PingRequest("hello"),
    TimeSpan.FromSeconds(5));

if (response.IsSuccess)
    Console.WriteLine(response.Value.Reply);
else
    Console.WriteLine($"Failed: {response.Code} — {response.Message}");
```

A failed response carries one of these codes:

| `Code` | Meaning |
|---|---|
| `TIMEOUT` | No response arrived within the timeout. |
| `DISCONNECTED` | The client disconnected before it responded. Before 1.2 this waited for the full timeout and reported `TIMEOUT`. |

```csharp
// Client → server, expecting a response
var response = await clientCommunication.SendMessage<EchoRequest, EchoResponse>(
    new EchoRequest("hi"));
```

The default client-side timeout is configurable via `CommunicationOptions.SendMessageTimeout`.

## Handlers

Handlers are discovered automatically — define a class that inherits from `PostMessageHandlerBase<T>` or `SendMessageHandlerBase<TRequest, TResponse>` and it gets registered in DI.

```csharp
// Fire-and-forget
public class TemperatureHandler : PostMessageHandlerBase<TemperatureReading>
{
    public override Task Handle(TemperatureReading message)
    {
        Console.WriteLine($"Got {message.Value}");
        return Task.CompletedTask;
    }
}

// Request-response
public class PingHandler : SendMessageHandlerBase<PingRequest, PingResponse>
{
    public override Task<PingResponse> Handle(PingRequest message) =>
        Task.FromResult(new PingResponse($"Pong: {message.Message}"));
}
```

Handlers can sit in either the server or client project, depending on which side initiates the conversation.

Each incoming message is handled in its own dependency injection scope, on the client as well as on the server. A handler can depend on scoped services such as an EF Core `DbContext`, and the scope, with any disposable services in it, is disposed when the handler completes.

## Loading handlers from additional assemblies

By default the assembly scan runs against the loaded assembly set. If your handlers live in a sibling package that wouldn't otherwise be scanned, opt them in:

```csharp
builder.AddThargaCommunicationServer(options =>
{
    options.AdditionalAssemblies = [typeof(MyHandlerFromAnotherPackage).Assembly];
    // …
});
```

The same option is available on the client.
