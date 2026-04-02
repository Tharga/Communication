# Fix: SubscriptionStateChangedHandler registered without AddThargaCommunicationClient

## Request
From Tharga.Starter, Tharga.MongoDB — see central Requests.md

## Problem
`HandlerTypeService.GetHandlerTypes()` uses assembly scanning (`AssemblyService.GetTypes`) to find all `PostMessageHandlerBase<T>` implementations and registers them in DI. This picks up `SubscriptionStateChangedHandler` even when `AddThargaCommunicationClient()` hasn't been called — for example, when only the server side is registered, or when MongoDB.Blazor references the Communication assembly.

`SubscriptionStateChangedHandler` depends on `SubscriptionStateTracker`, which is only registered by `AddThargaCommunicationClient()`. Result: runtime crash at `builder.Build()`.

## Fix approach
Exclude `SubscriptionStateChangedHandler` from assembly scanning by marking it so `HandlerTypeService` skips it. Instead, register it explicitly inside `AddThargaCommunicationClient()`.

Options:
- **Option A**: Filter by namespace/assembly — skip handlers in the `Tharga.Communication` assembly during scanning, register them explicitly in registration methods. Fragile.
- **Option B**: Add an attribute (e.g. `[ExcludeFromAutoDiscovery]`) and filter in `GetHandlerTypes`. Clean but adds public API surface.
- **Option C**: Register `SubscriptionStateTracker` unconditionally alongside the handler scan, so the dependency is always available. Simplest — the tracker is lightweight (empty ConcurrentDictionary) and harmless when unused.

**Recommended: Option C** — register `SubscriptionStateTracker` in both `AddThargaCommunicationClient` and `AddThargaCommunicationServer` (or during handler scanning). No behavior change, just ensures the dependency exists.

## Steps
- [ ] Register `SubscriptionStateTracker` as singleton in `CommunicationServerRegistration` alongside handler scanning
- [ ] Verify that `AddThargaCommunicationServer()` without `AddThargaCommunicationClient()` no longer crashes
- [ ] Write test: server-only registration resolves handlers without error
- [ ] Write test: handler still works when both client and server are registered

## Acceptance criteria
- [ ] `AddThargaCommunicationServer()` alone does not crash due to missing `SubscriptionStateTracker`
- [ ] `AddThargaCommunicationClient()` alone still works
- [ ] Both registered together still works
- [ ] No breaking changes
- [ ] Tests pass
