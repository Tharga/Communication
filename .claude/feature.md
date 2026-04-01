# Feature: subscription-messaging

## Goal
Allow the server to signal clients whether subscribers are active, so clients can skip sending data when no one is consuming it. Support both type-based subscriptions (all data of a type) and data-based subscriptions (specific key within a type).

## Originating branch
develop

## Scope
- Server-side: `SubscribeAsync<T>(key?)` returning `IAsyncDisposable`, `GetSubscriptions()` for monitoring
- Client-side: `HasSubscribers<T>(key?)`, `PostIfSubscribedAsync<T>`, `SubscriptionChanged` event
- Contract: `SubscriptionStateChanged` message type
- Wildcard matching: keyless subscription matches all keys

## Acceptance criteria
- [ ] Server can subscribe with type-only (wildcard) or type+key (specific)
- [ ] `IAsyncDisposable` handle correctly unsubscribes on dispose
- [ ] Clients receive automatic updates when subscribers join or leave
- [ ] `HasSubscribers` respects wildcard matching (keyless subscription matches all keys)
- [ ] `GetSubscriptions()` returns active topics and counts for monitoring
- [ ] No breaking changes to existing API (additive only)
- [ ] Tests cover the full lifecycle
- [ ] README documents the feature

## Done condition
All acceptance criteria met, all tests pass, README updated.
