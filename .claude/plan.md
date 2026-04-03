# Plan: handler-discovery

## Steps

### Part 1: Fail fast on missing handler
- [ ] **Step 1:** Write tests for fail-fast error response
- [ ] **Step 2:** Fix `SignalRHostedService.BuildConnection` SendMessage handler to catch and respond with error
- [ ] **Step 3:** Build and test
- [ ] **Step 4:** Commit

### Part 2: Additional assembly scanning
- [ ] **Step 5:** Write tests for additional assembly scanning
- [ ] **Step 6:** Add `AdditionalAssemblies` to client `CommunicationOptions` and wire through registration
- [ ] **Step 7:** Add `AdditionalAssemblies` to server `CommunicationOptions` and wire through registration
- [ ] **Step 8:** Build and test
- [ ] **Step 9:** Update README with additional assemblies example
- [ ] **Step 10:** Commit and notify Tharga.MongoDB via Requests.md
