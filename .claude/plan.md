# Plan: subscription-messaging

## Steps

### 1. Contract: SubscriptionStateChanged message type
- [x] Create `Contract/SubscriptionStateChanged.cs` — record with `Topic` (string, the type name), `Key` (string?, optional qualifier), `HasSubscribers` (bool)
- [x] Write tests for serialization round-trip (3 tests: round-trip with key, without key, RequestWrapper wrapping)

### 2. Server: SubscriptionManager
- [x] Create `Server/SubscriptionManager.cs` — internal service tracking reference counts per topic string in a `ConcurrentDictionary<string, int>`
- [x] `Subscribe(topic, key)` increments count, returns whether it crossed 0→1
- [x] `Unsubscribe(topic, key)` decrements count, returns whether it crossed 1→0
- [x] `GetSubscriptions()` returns snapshot of active topics and counts
- [x] Write tests (11 tests: boundary crossing, multi-subscriber, negative protection, wildcard vs keyed separation, BuildKey)
- [x] Added `InternalsVisibleTo` to csproj for test access

### 3. Server: IServerCommunication additions
- [x] Add `SubscribeAsync<T>(string? key = null)` to `IServerCommunication` — returns `IAsyncDisposable`
- [x] Add `GetSubscriptions()` to `IServerCommunication` — returns `IReadOnlyDictionary<string, int>`
- [x] Implement in `ServerCommunication`: subscribe calls `SubscriptionManager`, on boundary crossing pushes `SubscriptionStateChanged` to all clients via `PostToAllAsync`
- [x] The `IAsyncDisposable` handle calls unsubscribe on dispose, pushes state if boundary crossed; double-dispose safe via `Interlocked`
- [x] Write tests (7 tests: first/second subscriber notification, dispose last/non-last, double dispose, null key, GetSubscriptions)
- [x] Added `DynamicProxyGenAssembly2` to InternalsVisibleTo for Moq proxy generation

### 4. Server: DI registration
- [x] Register `SubscriptionManager` as singleton in `CommunicationServerRegistration`

### 5. Client: subscription state tracking
- [x] Create `Client/SubscriptionStateTracker.cs` — internal service maintaining a `ConcurrentDictionary` mirror of active topics
- [x] `Update(SubscriptionStateChanged)` method
- [x] `HasSubscribers(topic, key)` method with wildcard matching: if a keyless entry exists for the topic, all keys match
- [x] `SubscriptionChanged` event
- [x] Write tests (10 tests: no subs, exact match, different key, wildcard matches any key, wildcard matches no key, keyed doesn't match wildcard query, unsubscribe, different topics, events on subscribe/unsubscribe)

### 6. Client: IClientCommunication additions
- [x] Add `HasSubscribers<T>(string? key = null)` to `IClientCommunication`
- [x] Add `PostIfSubscribedAsync<T>(T message, string? key = null)` returning `Task<bool>` to `IClientCommunication`
- [x] Add `SubscriptionChanged` event to `IClientCommunication`
- [x] Implement in `ClientCommunication` by delegating to `SubscriptionStateTracker`
- [x] Register `SubscriptionStateTracker` in `CommunicationClientRegistration`
- [x] Write tests (6 tests: no subs, after notification, wildcard matching, post-if-subscribed skip/send, event forwarding)

### 7. Client: built-in handler for SubscriptionStateChanged
- [x] Create `Client/SubscriptionStateChangedHandler.cs` — a `PostMessageHandlerBase<SubscriptionStateChanged>` that updates `SubscriptionStateTracker`
- [x] Auto-discovered by `HandlerTypeService.GetHandlerTypes` (no manual registration needed)
- [x] Write tests (2 tests: subscribe updates tracker, unsubscribe updates tracker)

### 8. Full integration tests
- [x] End-to-end lifecycle: subscribe → client sees HasSubscribers → unsubscribe → client sees no subscribers
- [x] Multiple subscribers: count goes to 2, unsubscribe one, still has subscribers
- [x] Wildcard: subscribe without key, client checks with key → true
- [x] Keyed: subscribe with key "1", client checks key "2" → false
- [x] Navigation scenario: Farm 1 → Farm 2 page switch
- [x] GetSubscriptions monitoring
- [x] SubscriptionChanged event on client (7 integration tests total)

### 9. README and cleanup
- [x] Update README with subscription messaging examples (server, client, Blazor, matching rules)
- [x] Notify Tharga.MongoDB via requests.md (status updated + notification written)
