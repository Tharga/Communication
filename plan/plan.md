# Plan: pluggable-api-key-validator

## Steps

### 1. Contract: IApiKeyValidator + ApiKeyValidationResult
- [x] Create `IApiKeyValidator` in `Tharga.Communication.Server`
- [x] Create `ApiKeyValidationResult` record (IsValid, KeyId, KeyName)

### 2. Default validator + options
- [x] Add `ApiKeys` (string[]) to server `CommunicationOptions`; remove `PrimaryApiKey`, `SecondaryApiKey`, `ValidateApiKey()`
- [x] Implement `DefaultApiKeyValidator` (internal)
- [x] Add `RegisterApiKeyValidator<T>()` / `RegisterApiKeyValidator<TInterface, TService>()` to options
- [x] Rewrite `ApiKeyValidationTests` against `DefaultApiKeyValidator`

### 3. DI registration
- [x] In `CommunicationServerRegistration`, register `DefaultApiKeyValidator` if no custom validator registered
- [x] Custom validator registered under both `IApiKeyValidator` and its concrete interface (for direct resolution)

### 4. SignalRHub integration
- [x] Replace `_communicationOptions.ValidateApiKey(apiKey)` with `IApiKeyValidator.ValidateAsync`
- [x] On `IsValid = false` → abort connection
- [x] On `IsValid = true` → continue with `KeyId` and `KeyName` from result

### 5. ClientConnection / IClientConnectionInfo flow-through
- [x] Add `KeyId` and `KeyName` (nullable strings) to `ClientConnection`
- [x] Add `KeyId` and `KeyName` to `IClientConnectionInfo`
- [x] `ClientConnectionInfo` inherits from `ClientConnection` so picks up automatically
- [x] Populate from validation result in `OnConnectedAsync`
- [x] Tests for custom validator wins over default + KeyId/KeyName flow-through (3 tests)

### 6. Client identity overrides
- [x] Add `ClientType` and `ClientMachine` to client `CommunicationOptions`
- [x] Apply in `SignalRHostedService.BuildConnection()` — override if set, else fall back to defaults
- [x] Tests for default fallback + override behavior (4 tests, end-to-end via real server)

### 7. Documentation
- [x] README: API key section with `ApiKeys` + custom validator example
- [x] README: `IHttpContextAccessor` injection hint
- [x] README: Client identity override example
- [x] Updated feature list

### 8. Pipeline + closing
- [x] Bump `MAJOR_MINOR` in `.github/workflows/build.yml` from `0.1` → `0.2`
- [x] All tests pass (120), 0 warnings

## Status
All implementation steps done. Ready for review and merge.
