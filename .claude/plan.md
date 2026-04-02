# Plan: fix-handler-auto-discovery

## Steps
- [ ] Register `SubscriptionStateTracker` as singleton in `CommunicationServerRegistration`
- [ ] Use `TryAddSingleton` in both registrations to avoid double-registration when both are called
- [ ] Write test: server-only registration does not crash
- [ ] Write test: client-only registration still works
- [ ] Write test: both registrations together still works
