using System.Text.Json;
using Tharga.Communication.MessageHandler;
using Tharga.Communication.Server;
using Tharga.Communication.Server.Communication;
using Tharga.Mcp;

namespace Tharga.Communication.Mcp;

/// <summary>
/// Exposes Tharga.Communication runtime data as MCP resources on the System scope.
/// Resources: connected clients, active subscriptions, registered handlers.
/// </summary>
public sealed class CommunicationResourceProvider : IMcpResourceProvider
{
    internal const string ClientsUri = "communication://clients";
    internal const string SubscriptionsUri = "communication://subscriptions";
    internal const string HandlersUri = "communication://handlers";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly IServerCommunication _serverCommunication;
    private readonly ClientStateServiceBase _clientStateService;
    private readonly IHandlerTypeService _handlerTypeService;

    public CommunicationResourceProvider(
        IServerCommunication serverCommunication,
        ClientStateServiceBase clientStateService,
        IHandlerTypeService handlerTypeService)
    {
        _serverCommunication = serverCommunication;
        _clientStateService = clientStateService;
        _handlerTypeService = handlerTypeService;
    }

    public McpScope Scope => McpScope.System;

    public Task<IReadOnlyList<McpResourceDescriptor>> ListResourcesAsync(IMcpContext context, CancellationToken cancellationToken)
    {
        IReadOnlyList<McpResourceDescriptor> resources =
        [
            new McpResourceDescriptor
            {
                Uri = ClientsUri,
                Name = "Communication Clients",
                Description = "Connected SignalR clients with metadata (instance, machine, type, version, connect time).",
                MimeType = "application/json",
            },
            new McpResourceDescriptor
            {
                Uri = SubscriptionsUri,
                Name = "Communication Subscriptions",
                Description = "Active server subscriptions with subscriber counts, keyed by message type and optional data key.",
                MimeType = "application/json",
            },
            new McpResourceDescriptor
            {
                Uri = HandlersUri,
                Name = "Communication Handlers",
                Description = "Registered post and send message handlers with payload and handler types.",
                MimeType = "application/json",
            },
        ];
        return Task.FromResult(resources);
    }

    public async Task<McpResourceContent> ReadResourceAsync(string uri, IMcpContext context, CancellationToken cancellationToken)
    {
        return uri switch
        {
            ClientsUri => await BuildClientsAsync(cancellationToken),
            SubscriptionsUri => BuildSubscriptions(),
            HandlersUri => BuildHandlers(),
            _ => new McpResourceContent { Uri = uri, Text = $"Unknown resource: {uri}" },
        };
    }

    private async Task<McpResourceContent> BuildClientsAsync(CancellationToken cancellationToken)
    {
        var items = new List<object>();
        await foreach (var info in _clientStateService.GetConnectionInfosAsync().WithCancellation(cancellationToken))
        {
            items.Add(new
            {
                instance = info.Instance,
                connectionId = info.ConnectionId,
                machine = info.Machine,
                type = info.Type,
                version = info.Version,
                isConnected = info.IsConnected,
                connectTime = info.ConnectTime,
                disconnectTime = info.DisconnectTime,
            });
        }

        return new McpResourceContent
        {
            Uri = ClientsUri,
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new { clients = items }, JsonOptions),
        };
    }

    private McpResourceContent BuildSubscriptions()
    {
        var subs = _serverCommunication.GetSubscriptions();
        var items = subs.Select(s => new { topic = s.Key, subscriberCount = s.Value }).ToArray();

        return new McpResourceContent
        {
            Uri = SubscriptionsUri,
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new { subscriptions = items }, JsonOptions),
        };
    }

    private McpResourceContent BuildHandlers()
    {
        var items = _handlerTypeService.GetAll()
            .Select(h => new
            {
                payloadType = h.PayloadType.FullName,
                handlerType = h.HandlerType.FullName,
            })
            .ToArray();

        return new McpResourceContent
        {
            Uri = HandlersUri,
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new { handlers = items }, JsonOptions),
        };
    }
}
