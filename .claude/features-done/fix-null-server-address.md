# Fix: NullReferenceException in SignalRHostedService when ServerAddress is null

## Goal
Prevent crash when `AddThargaCommunicationClient()` is called without configuring `ServerAddress`.

## Originating branch
develop

## Fix
Guard against null `ServerAddress` in constructor and skip connection loop in `ExecuteAsync`.

## Acceptance criteria
- [ ] `AddThargaCommunicationClient()` without `ServerAddress` does not crash
- [ ] `SignalRHostedService` stays idle when no address configured
- [ ] Existing behavior unchanged when `ServerAddress` is provided
- [ ] Tests pass

## Done condition
All acceptance criteria met, all tests pass.
