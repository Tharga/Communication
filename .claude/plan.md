# Plan: handler-discovery

## Steps

### Part 1: Fail fast on missing handler
- [x] **Step 1:** Write tests for fail-fast error response
  - Added `MessageExecutorTests` confirming `InvalidOperationException` for missing handlers
- [x] **Step 2:** Fix `SignalRHostedService.BuildConnection` SendMessage handler to catch and respond with error
  - Wrapped in try/catch, sends error response wrapper back to server
- [x] **Step 3:** Build and test — 70/70 pass
- [x] **Step 4:** Committed: `fix: send error response when client has no handler for SendMessage`

### Part 2: Additional assembly scanning
- [x] **Step 5:** Write tests for additional assembly scanning
  - Added `HandlerTypeServiceTests` with 4 tests covering discovery with/without additional assemblies
- [x] **Step 6:** Add `AdditionalAssemblies` to client `CommunicationOptions` and wire through registration
- [x] **Step 7:** Add `AdditionalAssemblies` to server `CommunicationOptions` and wire through registration
- [x] **Step 8:** Build and test — 74/74 pass
- [x] **Step 9:** Update README with handler discovery section
- [~] **Step 10:** Commit and notify Tharga.MongoDB via Requests.md
