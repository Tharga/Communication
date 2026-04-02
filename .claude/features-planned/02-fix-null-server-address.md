# Fix: NullReferenceException in SignalRHostedService when ServerAddress is null

## Request
From Tharga.MongoDB — see central Requests.md

## Problem
`SignalRHostedService` constructor (line 30) does:
```csharp
_serverAddress = $"{_options.ServerAddress.TrimEnd('/')}/{_options.Pattern.TrimStart('/')}";
```
This throws `NullReferenceException` when `ServerAddress` is null. This happens when `AddThargaCommunicationClient()` is called as a workaround (to register handler dependencies) without configuring a server address.

## Fix approach
Handle null `ServerAddress` gracefully:
1. In the constructor: skip building `_serverAddress` if `ServerAddress` is null
2. In `ExecuteAsync`: skip the connection loop entirely if no server address is configured — log a debug message and return
3. `IsConnected` returns `false`, `SendAsync` no-ops or throws a clear exception

This makes it safe to register the client without a server address — the hosted service simply stays idle.

## Steps
- [ ] Guard `_serverAddress` construction against null `ServerAddress`
- [ ] In `ExecuteAsync`, return early with a log message if no server address configured
- [ ] Ensure `SendAsync` handles the no-connection case (either no-op or clear exception)
- [ ] Write test: null ServerAddress does not throw in constructor
- [ ] Write test: ExecuteAsync exits cleanly with null ServerAddress

## Acceptance criteria
- [ ] `AddThargaCommunicationClient()` without `ServerAddress` does not crash
- [ ] `SignalRHostedService` stays idle when no address configured
- [ ] Existing behavior unchanged when `ServerAddress` is provided
- [ ] Tests pass
