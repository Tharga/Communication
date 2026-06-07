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
