# Fix: SubscriptionStateChangedHandler registered without AddThargaCommunicationClient

## Goal
Prevent runtime crash when `SubscriptionStateChangedHandler` is auto-discovered by assembly scanning but `AddThargaCommunicationClient()` hasn't been called.

## Originating branch
develop

## Fix
Register `SubscriptionStateTracker` as singleton in `CommunicationServerRegistration` so the dependency is always available regardless of which registration method is called.

## Acceptance criteria
- [ ] `AddThargaCommunicationServer()` alone does not crash due to missing `SubscriptionStateTracker`
- [ ] `AddThargaCommunicationClient()` alone still works
- [ ] Both registered together still works
- [ ] Tests pass

## Done condition
All acceptance criteria met, all tests pass.
