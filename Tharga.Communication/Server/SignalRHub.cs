using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;
using Tharga.Communication.Server.Communication;

namespace Tharga.Communication.Server;

internal sealed class SignalRHub : Hub
{
    private readonly IServerCommunication _serverCommunication;
    private readonly IMessageExecutor _messageExecutor;
    private readonly ClientStateServiceBase _clientStateService;
    private readonly CommunicationOptions _communicationOptions;
    private readonly ILogger<SignalRHub> _logger;

    public SignalRHub(IServerCommunication serverCommunication, IMessageExecutor messageExecutor, IServiceProvider serviceProvider, IOptions<CommunicationOptions> options, ILogger<SignalRHub> logger)
    {
        _serverCommunication = serverCommunication;
        _messageExecutor = messageExecutor;
        _communicationOptions = options.Value;
        var type = _communicationOptions._clientStateServiceType.Interface;
        _clientStateService = serviceProvider.GetService(type) as ClientStateServiceBase;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();

        var apiKey = httpContext!.Request.Headers[Constants.Header.ApiKey].ToString();
        if (!_communicationOptions.ValidateApiKey(apiKey))
        {
            _logger.LogWarning("Client connection rejected: invalid or missing API key from {RemoteIp}.", httpContext.Connection.RemoteIpAddress);
            Context.Abort();
            return;
        }

        var instance = httpContext!.Request.Headers[Constants.Header.Instance];
        var machine = httpContext!.Request.Headers[Constants.Header.Machine];
        var type = httpContext!.Request.Headers[Constants.Header.Type];
        var version = httpContext!.Request.Headers[Constants.Header.Version];

        var clientConnection = new ClientConnection
        {
            ConnectionId = Context.ConnectionId,
            Instance = Guid.Parse(instance),
            Machine = machine,
            Type = type,
            Version = version
        };

        _logger.LogInformation("Client '{instance}' connected '{connectionId}' on machine {machine} version {version}.", clientConnection.Instance, clientConnection.ConnectionId, clientConnection.Machine, version);

        await _clientStateService.ConnectAsync(clientConnection);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogInformation("Client '{connectionId}' disconnected.", Context.ConnectionId);

        await _clientStateService.DisconnectedAsync(Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    [HubMethodName(Constants.PostMessage)]
    public async Task PostMessageAsync(object payload)
    {
        var connectionId = Context.ConnectionId;

        _logger.LogTrace($"{nameof(PostMessageAsync)} response from agent '{{connectionId}}'. [{{payload}}]", connectionId, payload);

        var json = payload.ToString() ?? throw new NullReferenceException("Cannot get json payload.");
        var wrapper = JsonSerializer.Deserialize<RequestWrapper>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        await _messageExecutor.ExecuteAsync(connectionId, wrapper);
    }

    [HubMethodName(Constants.SendMessage)]
    public async Task SendMessageAsync(object payload)
    {
        var connectionId = Context.ConnectionId;

        _logger.LogTrace("{Method} request from client '{ConnectionId}'. [{Payload}]", nameof(SendMessageAsync), connectionId, payload);

        var json = payload.ToString() ?? throw new NullReferenceException("Cannot get json payload.");
        var wrapper = JsonSerializer.Deserialize<RequestWrapper>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        try
        {
            var response = await _messageExecutor.ExecuteAsync(connectionId, wrapper);
            await Clients.Client(connectionId).SendAsync(Constants.ResponseMessage, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle SendMessage for type '{Type}' from client '{ConnectionId}'.", wrapper.Type, connectionId);
            var errorResponse = wrapper with
            {
                Payload = JsonSerializer.Serialize(new { Error = ex.Message })
            };
            await Clients.Client(connectionId).SendAsync(Constants.ResponseMessage, errorResponse);
        }
    }

    [HubMethodName(Constants.ResponseMessage)]
    public async Task ResponseMessageAsync(object payload)
    {
        var connectionId = Context.ConnectionId;

        _logger.LogTrace($"{nameof(ResponseMessageAsync)} response from agent '{{connectionId}}'. [{{payload}}]", connectionId, payload);

        var json = payload.ToString() ?? throw new NullReferenceException("Cannot get json payload.");
        var wrapper = JsonSerializer.Deserialize<RequestWrapper>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        ((ServerCommunication)_serverCommunication).OnResponseEvent(this, new ResponseEventArgs(connectionId, wrapper));
    }
}
