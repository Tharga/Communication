# Feature: subscription-messaging

## Request
From Tharga.MongoDB — see `.claude/requests.md`

## Goal
Allow the server to signal clients whether subscribers are active, so clients can skip sending data when no one is consuming it.

## Scope

### Server side
- Add a subscription manager service that tracks active subscribers per message type or channel
- Provide methods to subscribe/unsubscribe (e.g. `IServerCommunication.SubscribeAsync` / `UnsubscribeAsync`)
- When subscriber count changes from 0→1 or 1→0, notify connected clients via a built-in post message
- Expose `HasSubscribers` or subscriber count on the server side

### Client side
- Add `IClientCommunication.HasSubscribers` property or event that tracks server-sent subscription state
- Provide a `SubscriptionChangedEvent` so clients can react to subscriber state changes
- Optionally provide a `PostIfSubscribedAsync<T>` convenience method that no-ops when no subscribers

### Protocol
- Define built-in message types for subscription state notifications (e.g. `SubscriptionStateMessage`)
- Use the existing `PostMessage` mechanism to push state from server to clients

## Steps
- [ ] Design the subscription state message type and add to contracts
- [ ] Implement `SubscriptionManager` on the server to track subscriber counts
- [ ] Add `Subscribe`/`Unsubscribe` methods to `IServerCommunication`
- [ ] Push subscription state to clients when count crosses 0↔1 boundary
- [ ] Add `HasSubscribers` property and `SubscriptionChangedEvent` to `IClientCommunication`
- [ ] Add built-in handler on client side for subscription state messages
- [ ] Add `PostIfSubscribedAsync<T>` convenience method to `IClientCommunication`
- [ ] Write tests for subscribe/unsubscribe lifecycle, multi-subscriber scenarios, and client-side state tracking
- [ ] Update README with subscription messaging examples
- [ ] Commit and notify Tharga.MongoDB via requests.md

## Acceptance criteria
- [ ] Server can signal clients that subscribers are active
- [ ] Clients can check subscription state before sending
- [ ] Client receives automatic updates when subscribers join or leave
- [ ] No breaking changes to existing API (additive only)
- [ ] Tests cover the full lifecycle
- [ ] README documents the feature

## Notes
- This adds new public API surface — additive, no version bump needed
- Consider whether subscription state should be per-message-type or global (start with global, extend later if needed)
