# Feature: Pluggable API key validator and client identity overrides

## Request
GitHub issue [#11](https://github.com/Tharga/Communication/issues/11) — "Optional mixed-mode auth and surface validation result + per-key usage". Driven by [Tharga/MongoDB#100](https://github.com/Tharga/MongoDB/issues/100) — per-agent auth posture and per-key usage in admin UIs.

## Goal
Replace the hard-coded `PrimaryApiKey`/`SecondaryApiKey` check with a pluggable `IApiKeyValidator` interface, registered the same way as `ClientStateServiceBase`. Default implementation checks an array of configured keys. Consumers (e.g. Tharga.MongoDB.Monitor.Server) can register a custom validator that delegates to Tharga.Platform's `IApiKeyAdministrationService` or any other backend.

## Scope

### New contract (Tharga.Communication public API)
- `IApiKeyValidator` interface with `Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken ct = default)`
- `ApiKeyValidationResult` record:
  - `bool IsValid` — required
  - `string KeyId` — stable, machine-friendly identifier of the matched key (null = anonymous when validator allows empty)
  - `string FriendlyName` — human-readable label for admin UIs / audit logs (null if not provided)

### Default implementation
- `DefaultApiKeyValidator` (internal) checks `CommunicationOptions.ApiKeys` (string[]):
  - `ApiKeys` empty/null → accept all (returns `IsValid = true, KeyId = null`)
  - `ApiKeys` set, key matches → `IsValid = true, KeyId = "key-{index}"`
  - `ApiKeys` set, key empty or no match → `IsValid = false`

### Registration
- `CommunicationOptions.RegisterApiKeyValidator<T>()` and `RegisterApiKeyValidator<TInterface, TService>()` (matches `RegisterClientStateService<>` pattern)
- If not called, default validator is wired up automatically

### Breaking changes (minor version bump 0.1 → 0.2)
- Remove `CommunicationOptions.PrimaryApiKey`
- Remove `CommunicationOptions.SecondaryApiKey`
- Remove `CommunicationOptions.ValidateApiKey()`
- Existing `ApiKeyValidationTests` rewritten against `DefaultApiKeyValidator`

### Flow-through to `IClientConnectionInfo`
- `ClientConnection` gains `KeyId` and `FriendlyName` (both nullable strings)
- `IClientConnectionInfo` gets the same fields
- Default `ClientConnectionInfo` includes them
- `SignalRHub.OnConnectedAsync` passes them from the validation result into the constructed `ClientConnection`

### SignalRHub change
- Replace inline `ValidateApiKey()` call with resolved `IApiKeyValidator.ValidateAsync(apiKey, ct)`
- On `IsValid = false` → log + `Context.Abort()` (today's behavior)
- On `IsValid = true` → continue, pass `KeyId`/`FriendlyName` to state service

### Documentation
- README: register a custom validator example
- **Hint about injecting `IHttpContextAccessor`** into custom validators — if a validator needs IP, headers, or other HTTP context (for rate limiting, IP allowlists, etc.), it should take `IHttpContextAccessor` via constructor injection. The framework intentionally does not pass `HttpContext` to keep the interface narrow.
- Example: a Platform-backed validator that calls `IApiKeyAdministrationService.GetAsync(apiKey)` and returns `IsValid = true, KeyId = key.Id.ToString(), FriendlyName = key.Name`

### Client identity overrides (client-side `CommunicationOptions`)
Separate concern bundled into the same release. Today the client sends `X-Client-Machine`, `X-Client-Type`, `X-Client-Version` derived from `Environment.MachineName` and `Assembly.GetEntryAssembly()`. The defaults aren't always useful (multiple roles in one assembly, hash-named container hosts, generic entry assembly).

Add to client `CommunicationOptions`:
- `string ClientType` — overrides `X-Client-Type`; null/empty → falls back to `Assembly.GetEntryAssembly().Name`
- `string ClientMachine` — overrides `X-Client-Machine`; null/empty → falls back to `Environment.MachineName`

Do **not** add a `ClientVersion` override — version should reflect what's actually running.
Do **not** expose `Instance` for override — it's specifically the runtime-unique identifier.

### Pipeline
- Bump `MAJOR_MINOR` in `.github/workflows/build.yml` from `0.1` → `0.2`

## Steps
- [ ] Add `IApiKeyValidator` and `ApiKeyValidationResult` to `Tharga.Communication.Server` namespace
- [ ] Add `ApiKeys` (string[]) to `CommunicationOptions`; remove `PrimaryApiKey`, `SecondaryApiKey`, `ValidateApiKey()`
- [ ] Implement `DefaultApiKeyValidator` (internal, default fallback)
- [ ] Add `RegisterApiKeyValidator<T>()` and `RegisterApiKeyValidator<TInterface, TService>()` to `CommunicationOptions`
- [ ] Wire registration in `CommunicationServerRegistration`: if no validator registered, register `DefaultApiKeyValidator`
- [ ] Replace `ValidateApiKey()` call in `SignalRHub.OnConnectedAsync` with `IApiKeyValidator.ValidateAsync`
- [ ] Add `KeyId` and `FriendlyName` (nullable strings) to `ClientConnection`, `IClientConnectionInfo`, `ClientConnectionInfo`
- [ ] Populate `KeyId`/`FriendlyName` from validation result in `OnConnectedAsync`
- [ ] Rewrite `ApiKeyValidationTests` against `DefaultApiKeyValidator`
- [ ] Add tests: custom validator wins over default, validator rejection aborts connection, KeyId/FriendlyName flow through to state service
- [ ] Update README with custom-validator example and `IHttpContextAccessor` injection hint
- [ ] Add `ClientType` and `ClientMachine` to client `CommunicationOptions` (with fallback to existing defaults)
- [ ] Use the overrides in `SignalRHostedService.BuildConnection()` when setting `X-Client-Type` / `X-Client-Machine` headers
- [ ] Tests for client identity overrides (override applied vs default fallback)
- [ ] Bump `MAJOR_MINOR` to `0.2` in `.github/workflows/build.yml`
- [ ] Close GH issue #11 with a comment pointing at the released version

## Acceptance criteria
- [ ] Consumers can register a custom `IApiKeyValidator` via `options.RegisterApiKeyValidator<T>()`
- [ ] Default validator checks `ApiKeys` array; backwards-compatible behavior when `ApiKeys` is empty (no auth)
- [ ] Validation rejection aborts the connection (today's behavior preserved)
- [ ] `KeyId` and `FriendlyName` surface on `IClientConnectionInfo`
- [ ] Custom validator can inject `IHttpContextAccessor` for HTTP-level context (documented)
- [ ] Client can override `ClientType` and `ClientMachine` via options
- [ ] No `PrimaryApiKey`/`SecondaryApiKey`/`ValidateApiKey` in public API
- [ ] All tests pass; new tests cover custom validator + flow-through
- [ ] README updated
- [ ] Version bumped to 0.2 in GHA workflow

## Notes
- Minor version bump (0.1.x → 0.2.x) for the breaking change. Pre-1.0 semver: minor bumps allowed for breaking changes.
- This single extension point covers all three sub-asks of issue #11:
  - **AllowUnauthenticated** — validator decides whether empty keys are accepted (no framework flag needed)
  - **Surface auth result** — `KeyId`/`FriendlyName` on `IClientConnectionInfo`
  - **Per-key usage tracking** — the validator's concern (Platform validator naturally records usage in its DB)
