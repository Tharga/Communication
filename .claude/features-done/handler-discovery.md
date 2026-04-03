# Feature: handler-discovery

## Originating branch
`develop`

## Requests
From Tharga.MongoDB — see `Requests.md`:
1. SendMessageAsync should fail fast when no handler is registered
2. Assembly scan should include explicitly registered handler assemblies

## Goal
Improve message handler discovery and error reporting so that (1) missing handlers produce immediate errors instead of 60s timeouts, and (2) consumers can register additional assemblies for handler scanning.

## Scope

### Fail fast on missing handler
- In `SignalRHostedService.BuildConnection` SendMessage handler: catch exceptions from `MessageExecutor.ExecuteAsync` and send back an error response instead of swallowing the exception
- The server already gets a `Response<T>.Fail` on timeout — now it should get one immediately with a clear error

### Assembly scan
- Add `AdditionalAssemblies` property to client `CommunicationOptions`
- Pass additional assemblies to `HandlerTypeService.GetHandlerTypes` in both client and server registrations
- `GetHandlerTypes` already accepts an `assemblies` parameter — just need to expose it

## Acceptance criteria
- [ ] Missing handler on client produces immediate error response to server (not 60s timeout)
- [ ] `CommunicationOptions` accepts additional assemblies for handler scanning
- [ ] Additional assemblies are passed through to `HandlerTypeService.GetHandlerTypes`
- [ ] Tests cover both features
- [ ] README documents additional assemblies configuration

## Done condition
All acceptance criteria are met and user confirms the feature is complete.
