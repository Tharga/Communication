# Feature: Tharga.Communication.Mcp — MCP Provider

## Request
From "All products using Communication" (Neurolito, Eplicta Core) — see central Requests.md

## Goal
Expose Tharga.Communication's runtime data via MCP (Model Context Protocol) so AI assistants and diagnostic tools can introspect connected clients, active subscriptions, and registered handlers without UI access.

## Scope (v1, read-only)

### Resources
- **`communication://clients`** — connected clients with metadata (instance, connection id, machine, type, version, connect time)
- **`communication://subscriptions`** — active server subscriptions and subscriber counts (from `IServerCommunication.GetSubscriptions()`)
- **`communication://handlers`** — registered Post and Send message handlers (payload types and handler types)

### Tools
None in v1. (Original request mentioned "send message" — skipped due to safety; "list dead letters" — skipped because Communication is SignalR-only with no message persistence.)

## Design decisions

### New project: `Tharga.Communication.Mcp`
- Sibling to `Tharga.Communication`, follows the `Tharga.MongoDB.Mcp` pattern
- TargetFrameworks `net8.0;net9.0` (matches Communication, Mcp supports these)
- Depends on `Tharga.Mcp` and project-references `Tharga.Communication`
- Single resource provider class: `CommunicationResourceProvider : IMcpResourceProvider`
- Single extension method: `IThargaMcpBuilder.AddCommunication()`
- Scope: `McpScope.System` (read-only diagnostic data)

### Required changes in Tharga.Communication
1. **Client enumeration** — `ClientStateServiceBase<T>` is generic, MCP doesn't know `T`. Add a non-generic abstract method on the base `ClientStateServiceBase`:
   ```csharp
   public abstract IAsyncEnumerable<IClientConnectionInfo> GetConnectionInfosAsync();
   ```
   Override in `ClientStateServiceBase<T>` to delegate to the existing `GetAsync()` and cast.

2. **Handler enumeration** — `IHandlerTypeService` only has `TryGetHandler`. Add:
   ```csharp
   IReadOnlyCollection<HandlerTypeInfo> GetAll();
   ```
   Implement in `HandlerTypeService` by exposing `_handlerTypes.Values`.

These are additive, non-breaking interface changes (additions to existing interfaces).

## Steps

### 1. Expose enumeration APIs in Tharga.Communication
- [ ] Add abstract `GetConnectionInfosAsync()` to `ClientStateServiceBase` returning `IAsyncEnumerable<IClientConnectionInfo>`
- [ ] Implement in `ClientStateServiceBase<T>` by iterating `GetAsync()` and casting to `IClientConnectionInfo`
- [ ] Add `GetAll()` to `IHandlerTypeService` returning `IReadOnlyCollection<HandlerTypeInfo>`
- [ ] Implement in `HandlerTypeService`
- [ ] Tests for both new APIs

### 2. Create Tharga.Communication.Mcp project
- [ ] Create `Tharga.Communication.Mcp/Tharga.Communication.Mcp.csproj` (TargetFrameworks `net8.0;net9.0`, Version 1.0.0, references Tharga.Mcp + project ref to Tharga.Communication)
- [ ] Add to solution file `Tharga.Communication.sln`
- [ ] Add `InternalsVisibleTo` for tests if needed

### 3. Implement CommunicationResourceProvider
- [ ] Class `CommunicationResourceProvider : IMcpResourceProvider`
- [ ] Constructor injects `IServerCommunication`, `ClientStateServiceBase`, `IHandlerTypeService`
- [ ] `Scope => McpScope.System`
- [ ] `ListResourcesAsync` returns the 3 descriptors with stable URIs
- [ ] `ReadResourceAsync(uri)` switches on URI to build JSON for clients / subscriptions / handlers

### 4. Implement ThargaMcpBuilderExtensions
- [ ] Static class with `AddCommunication(this IThargaMcpBuilder builder)` extension
- [ ] Calls `builder.AddResourceProvider<CommunicationResourceProvider>()`
- [ ] Returns the builder for chaining

### 5. Tests (new project Tharga.Communication.Mcp.Tests, or share with Tharga.Communication.Tests)
- [ ] `ListResourcesAsync` returns 3 descriptors with expected URIs
- [ ] `ReadResourceAsync("communication://clients")` returns connected clients as JSON
- [ ] `ReadResourceAsync("communication://subscriptions")` returns active subscriptions
- [ ] `ReadResourceAsync("communication://handlers")` returns registered handlers
- [ ] Unknown URI returns clear error content

### 6. Update GHA pipeline to pack the new package
- [ ] Update `.github/workflows/build.yml` Pack steps to also pack `Tharga.Communication.Mcp/Tharga.Communication.Mcp.csproj`
- [ ] Update NuGet release URL in summary

### 7. README and notifications
- [ ] Add a `README.md` in Tharga.Communication.Mcp explaining usage (`builder.AddThargaMcp().AddCommunication()`)
- [ ] Update central Requests.md to mark this request as Done

## Acceptance criteria
- [ ] `Tharga.Communication.Mcp` package builds and packs cleanly
- [ ] All 3 resources return well-formed JSON when read
- [ ] Consumer can call `builder.AddThargaMcp().AddCommunication()` and see the resources via `/mcp`
- [ ] No breaking changes to Tharga.Communication public API
- [ ] All tests pass
- [ ] GHA produces both packages on release

## Notes
- Start with read-only resources only. Tools (e.g. send message, force disconnect) can be added later if a clear use case emerges.
- The clients resource exposes raw `IClientConnectionInfo` — if consumers register a richer custom type, only the base fields are visible via MCP. That's intentional to keep the contract stable.
- `Tharga.Mcp` version: latest stable (currently 0.1.3 per MongoDB.Mcp).
