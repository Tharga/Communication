# Plan: api-key-auth

## Steps

- [x] **Step 1:** Write tests for API key validation scenarios
  - 9 tests covering: no keys (accept all, accept any), valid primary, valid secondary, both keys accept either, invalid key, missing key, empty key
- [x] **Step 2:** Add `ApiKey` header constant to `Constants.Header`
- [x] **Step 3:** Add `ApiKey` property to client `CommunicationOptions` and send header in `SignalRHostedService.BuildConnection`
- [x] **Step 4:** Add `PrimaryApiKey`, `SecondaryApiKey`, and `ValidateApiKey` to server `CommunicationOptions`
- [x] **Step 5:** Validate API key in `SignalRHub.OnConnectedAsync` — abort connection if invalid
- [x] **Step 6:** Verify build and all tests pass — 15/15 pass
- [x] **Step 7:** Update README with authentication examples and server options table
  - Also bumped version to 1.1.0
- [~] **Step 8:** Commit and notify Tharga.MongoDB via requests.md
