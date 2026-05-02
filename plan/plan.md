# Plan: mcp-provider

## Steps

### 1. Expose enumeration APIs in Tharga.Communication
- [x] Add abstract `GetConnectionInfosAsync()` to `ClientStateServiceBase`
- [x] Implement in `ClientStateServiceBase<T>` by iterating `GetAsync()`
- [x] Add `GetAll()` to `IHandlerTypeService`
- [x] Implement in `HandlerTypeService`
- [x] Tests for both APIs (3 new tests, 109 total pass)

### 2. Create Tharga.Communication.Mcp project
- [x] Create `Tharga.Communication.Mcp/Tharga.Communication.Mcp.csproj` (net8.0;net9.0, v1.0.0)
- [x] Add to solution
- [x] Project references Tharga.Communication, package reference Tharga.Mcp 0.1.3
- [x] Cleaned up dangling AzDo solution items

### 3. Implement CommunicationResourceProvider
- [x] Class with 3 stable URIs (clients, subscriptions, handlers)
- [x] System scope
- [x] JSON output with sensible field projection

### 4. Implement ThargaMcpBuilderExtensions
- [x] Static `AddCommunication(this IThargaMcpBuilder)` extension

### 5. Tests
- [x] Scope is System
- [x] List resources returns 3 descriptors
- [x] Read each resource returns expected JSON shape (clients, subscriptions, handlers)
- [x] Unknown URI returns clear error
- [x] 6 new tests, 115 total

### 6. Update GHA pipeline to pack the new package
- [x] Add second pack step (stable + prerelease) for Tharga.Communication.Mcp.csproj
- [x] Update release/prerelease summary URLs

### 7. README and notifications
- [x] Add Tharga.Communication.Mcp/README.md
- [ ] Mark request as Done in central Requests.md (after user confirms)
