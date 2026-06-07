# Authentication

Tharga.Communication uses an **`IApiKeyValidator`** to authenticate the client on connection. The default validator checks against a simple `ApiKeys` array; consumers can register a custom validator to delegate to a database (e.g. [Tharga.Platform](https://platform.tharga.net) API key administration) or any other backend.

## Default: keys configured on the server

The simplest setup — set `ApiKeys` on the server, send a matching key from the client:

```csharp
// Server
builder.AddThargaCommunicationServer(options =>
{
    options.ApiKeys = ["my-secret-key", "rotation-key"];
    options.RegisterClientStateService<MyClientStateService>();
    options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
});
```

```csharp
// Client
builder.AddThargaCommunicationClient(o => o.ApiKey = "my-secret-key");
```

If `ApiKeys` is null or empty, the default validator **accepts all connections** (backwards-compatible default for unsecured setups).

## Custom validator

For real-world scenarios — per-key audit, lookup against a DB, IP allowlists — register your own `IApiKeyValidator`:

```csharp
public class PlatformApiKeyValidator : IApiKeyValidator
{
    private readonly IApiKeyAdministrationService _keys;

    public PlatformApiKeyValidator(IApiKeyAdministrationService keys) => _keys = keys;

    public async Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken ct = default)
    {
        var key = await _keys.GetByValueAsync(apiKey, ct);
        return key is null
            ? new() { IsValid = false }
            : new() { IsValid = true, KeyId = key.Id.ToString(), KeyName = key.Name };
    }
}

builder.AddThargaCommunicationServer(options =>
{
    options.RegisterApiKeyValidator<PlatformApiKeyValidator>();
    // …
});
```

The validator returns an `ApiKeyValidationResult` with:

- **`IsValid`** — whether to accept the connection (`false` → connection aborted)
- **`KeyId`** — stable identifier of the matched key (e.g. a Guid). Surfaces on `IClientConnectionInfo.KeyId` so admin UIs can correlate connections to keys
- **`KeyName`** — optional human-readable label for the key (e.g. *"production-monitor"*). Surfaces on `IClientConnectionInfo.KeyName`

The validator decides everything — including whether to accept empty/missing keys. To allow anonymous connections, return `{ IsValid = true, KeyId = null }` for empty input.

### Need HTTP context?

Inject `IHttpContextAccessor` into your validator if you need access to the request (IP allowlists, custom headers, etc.):

```csharp
public class IpAllowlistValidator : IApiKeyValidator
{
    private readonly IHttpContextAccessor _http;
    private readonly HashSet<IPAddress> _allowed;

    public IpAllowlistValidator(IHttpContextAccessor http, IOptions<MyOptions> opts)
    {
        _http = http;
        _allowed = opts.Value.AllowedIps.ToHashSet();
    }

    public Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken ct = default)
    {
        var ip = _http.HttpContext?.Connection.RemoteIpAddress;
        var ok = ip is not null && _allowed.Contains(ip);
        return Task.FromResult(new ApiKeyValidationResult { IsValid = ok });
    }
}
```

The framework intentionally does not pass `HttpContext` into `IApiKeyValidator.ValidateAsync` — it keeps the interface narrow and testable. Inject what you need.

## Client identity overrides

The client sends four self-reported identity headers when it connects: `Instance` (Guid generated per process run), `Machine`, `Type` (assembly name), and `Version`. Two of those — `Machine` and `Type` — can be overridden via options:

```csharp
builder.AddThargaCommunicationClient(o =>
{
    o.ServerAddress = "https://localhost:5001";
    o.ClientType = "monitor-agent";        // override; defaults to entry assembly name
    o.ClientMachine = "us-east-prod-1";    // override; defaults to Environment.MachineName
});
```

Useful when one assembly hosts multiple roles, or when containerized hosts have meaningless hash-based hostnames. `Version` and `Instance` are not overridable — `Version` should reflect what's actually running, and `Instance` is specifically the runtime-unique identifier.
