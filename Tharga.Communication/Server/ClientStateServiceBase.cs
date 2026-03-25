using Microsoft.Extensions.Options;
using Tharga.Communication.Server.Communication;

namespace Tharga.Communication.Server;

/// <summary>
/// Base class for tracking client connection state on the server.
/// </summary>
public abstract class ClientStateServiceBase
{
    /// <summary>Handles a new client connection.</summary>
    /// <param name="clientConnection">The connection metadata.</param>
    public abstract Task ConnectAsync(ClientConnection clientConnection);

    /// <summary>Handles a client disconnection.</summary>
    /// <param name="connectionId">The SignalR connection ID that disconnected.</param>
    public abstract Task DisconnectedAsync(string connectionId);
}

/// <summary>
/// Generic base class for tracking client connection state with a custom connection info type.
/// Inherit from this class and implement <see cref="Build"/> and <see cref="BuildDisconnect"/>
/// to map connection data to your custom <typeparamref name="T"/> type.
/// </summary>
/// <typeparam name="T">The client connection info type, must implement <see cref="IClientConnectionInfo"/>.</typeparam>
public abstract class ClientStateServiceBase<T> : ClientStateServiceBase
    where T : IClientConnectionInfo
{
    /// <inheritdoc cref="ClientStateServiceBase{T}"/>
    protected ClientStateServiceBase(IServiceProvider serviceProvider, IOptions<CommunicationOptions> options)
    {
        var repository = serviceProvider.GetService(options.Value._clientRepositoryType.Interface);
        _repository = repository as ClientRepositoryBase<T> ?? throw new NullReferenceException($"Cannot cast {repository?.GetType()} to {typeof(ClientRepositoryBase<T>)}.");
    }

    private readonly ClientRepositoryBase<T> _repository;

    /// <summary>Raised when a client connects or reconnects.</summary>
    public event EventHandler<ConnectionChangedEventArgs> ConnectionChangedEvent;

    /// <summary>Raised when a client disconnects.</summary>
    public event EventHandler<DisconnectedEventArgs> DisconnectedEvent;

    /// <summary>
    /// Creates a <typeparamref name="T"/> instance from raw connection info.
    /// </summary>
    /// <param name="clientConnectionInfo">The base connection info.</param>
    /// <returns>A custom client connection info instance.</returns>
    protected abstract T Build(IClientConnectionInfo clientConnectionInfo);

    /// <summary>
    /// Creates an updated <typeparamref name="T"/> instance representing a disconnected client.
    /// </summary>
    /// <param name="clientConnectionInfo">The existing connection info.</param>
    /// <param name="disconnectTime">The UTC time of disconnection.</param>
    /// <returns>An updated client connection info instance.</returns>
    protected abstract T BuildDisconnect(T clientConnectionInfo, DateTime disconnectTime);

    public override async Task ConnectAsync(ClientConnection clientConnection)
    {
        var clientConnectionInfo = new ClientConnectionInfo
        {
            Instance = clientConnection.Instance,
            ConnectionId = clientConnection.ConnectionId,
            Machine = clientConnection.Machine,
            Type = clientConnection.Type,
            Version = clientConnection.Version,
            IsConnected = true,
            ConnectTime = DateTime.UtcNow
        };

        var info = Build(clientConnectionInfo);

        await UpsertAsync(info);

        ConnectionChangedEvent?.Invoke(this, new ConnectionChangedEventArgs(info));
    }

    public override async Task DisconnectedAsync(string connectionId)
    {
        var updated = await _repository.DeleteAsync(connectionId);
        DisconnectedEvent?.Invoke(this, new DisconnectedEventArgs(updated));
    }

    public async IAsyncEnumerable<T> GetAsync()
    {
        await foreach (var item in _repository.GetAsync())
        {
            yield return item;
        }
    }

    protected async Task UpsertAsync(T client)
    {
        await _repository.UpsertAsync(client);
    }
}
