# Tharga.Communication.Mcp

[![GitHub repo](https://img.shields.io/github/repo-size/Tharga/Communication?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Tharga/Communication)

Exposes Tharga.Communication runtime data via MCP (Model Context Protocol). Plugs into [Tharga.Mcp](https://www.nuget.org/packages/Tharga.Mcp).

## Resources

| URI | Description |
|---|---|
| `communication://clients` | Connected SignalR clients with metadata (instance, machine, type, version, connect time) |
| `communication://subscriptions` | Active server subscriptions with subscriber counts |
| `communication://handlers` | Registered post and send message handlers |

All resources are exposed on the **System** scope (read-only diagnostic data).

## Usage

```csharp
builder.AddThargaMcp(mcp =>
{
    mcp.AddCommunication();
});

app.UseThargaMcp();
```

The `Tharga.Communication` server (`AddThargaCommunicationServer()`) must be registered for the MCP provider to resolve its dependencies.

For full documentation and examples, see the [GitHub repository](https://github.com/Tharga/Communication).
