# Feature: subscription-messaging

## Request
From Tharga.MongoDB — see `.claude/requests.md`

## Goal
Allow the server to signal clients whether subscribers are active, so clients can skip sending data when no one is consuming it. Support both type-based subscriptions (all data of a type) and data-based subscriptions (specific key within a type).

## Design decisions

### Topic model
- A subscription is a **topic** = `TypeName` + optional **key**
- Type-based: `Subscribe<CollectionDto>()` — matches all keys
- Data-based: `Subscribe<FarmDetailsDto>("1")` — matches only key "1"
- Matching: a keyless (wildcard) subscription makes `HasSubscribers<T>(anyKey)` return true

### Server-side API (consumer/dashboard)
- `IServerCommunication.SubscribeAsync<T>(string? key = null)` → returns `IAsyncDisposable`
- Disposing the handle unsubscribes automatically (fits Blazor page lifecycle)
- `IServerCommunication.GetSubscriptions()` → `IReadOnlyDictionary<string, int>` for monitoring active topics and subscriber counts

### Client-side API (agent/producer)
- `IClientCommunication.HasSubscribers<T>(string? key = null)` — check before sending
- `IClientCommunication.PostIfSubscribedAsync<T>(T message, string? key = null)` — convenience no-op when no subscribers
- `IClientCommunication.SubscriptionChanged` event for reacting to state changes

### Protocol
- Server tracks reference counts per topic in a `ConcurrentDictionary<string, int>`
- When count crosses 0↔1 or 1↔0, server pushes `SubscriptionStateChanged` message to all clients via `PostToAllAsync`
- `SubscriptionStateChanged` is a regular message — can be handled with a standard `PostMessageHandlerBase<T>` for custom monitoring/logging

## Scope

### Server side
- `SubscriptionManager` service tracking subscriber counts per topic (type + optional key)
- `SubscribeAsync<T>(key?)` on `IServerCommunication` returning `IAsyncDisposable`
- Push `SubscriptionStateChanged` to clients on 0↔1 boundary crossings
- `GetSubscriptions()` method exposing active topics and counts for monitoring

### Client side
- Local mirror of active subscription topics, updated from server messages
- `HasSubscribers<T>(key?)` property to check before sending
- `SubscriptionChanged` event for reacting to state changes
- `PostIfSubscribedAsync<T>(message, key?)` convenience method

### Contract
- `SubscriptionStateChanged` message type (topic, key, hasSubscribers)

## Steps
- [ ] Design and add `SubscriptionStateChanged` message type to contracts
- [ ] Implement `SubscriptionManager` on the server to track reference counts per topic
- [ ] Add `SubscribeAsync<T>(key?)` to `IServerCommunication` returning `IAsyncDisposable`
- [ ] Push subscription state to clients when count crosses 0↔1 boundary
- [ ] Add `GetSubscriptions()` to `IServerCommunication` for monitoring
- [ ] Add `HasSubscribers<T>(key?)` and `SubscriptionChanged` event to `IClientCommunication`
- [ ] Add built-in handler on client side for `SubscriptionStateChanged` messages
- [ ] Add `PostIfSubscribedAsync<T>(message, key?)` convenience method to `IClientCommunication`
- [ ] Write tests for subscribe/unsubscribe lifecycle, wildcard vs keyed matching, multi-subscriber scenarios, and client-side state tracking
- [ ] Update README with subscription messaging examples
- [ ] Commit and notify Tharga.MongoDB via requests.md

## Acceptance criteria
- [ ] Server can subscribe with type-only (wildcard) or type+key (specific)
- [ ] `IAsyncDisposable` handle correctly unsubscribes on dispose
- [ ] Clients receive automatic updates when subscribers join or leave
- [ ] `HasSubscribers` respects wildcard matching (keyless subscription matches all keys)
- [ ] `GetSubscriptions()` returns active topics and counts for monitoring
- [ ] No breaking changes to existing API (additive only)
- [ ] Tests cover the full lifecycle
- [ ] README documents the feature

## Notes
- This adds new public API surface — additive, no version bump needed
- `SubscriptionStateChanged` flows through the standard message routing, so consumers can handle it like any other message for custom monitoring
