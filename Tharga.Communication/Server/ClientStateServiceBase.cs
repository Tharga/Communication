using Microsoft.Extensions.Options;
using Tharga.Communication.Server.Communication;

namespace Tharga.Communication.Server;

public abstract class ClientStateServiceBase
{
    public abstract Task ConnectAsync(ClientConnection clientConnection);
    public abstract Task DisconnectedAsync(string connectionId);
}

public abstract class ClientStateServiceBase<T> : ClientStateServiceBase
    where T : IClientConnectionInfo
{
    protected ClientStateServiceBase(IServiceProvider serviceProvider, IOptions<CommunicationOptions> options)
    {
        var repository = serviceProvider.GetService(options.Value._clientRepositoryType.Interface);
        _repository = repository as ClientRepositoryBase<T> ?? throw new NullReferenceException($"Cannot cast {repository?.GetType()} to {typeof(ClientRepositoryBase<T>)}.");
    }

    private readonly ClientRepositoryBase<T> _repository;

    public event EventHandler<ConnectionChangedEventArgs> ConnectionChangedEvent;
    public event EventHandler<DisconnectedEventArgs> DisconnectedEvent;

    protected abstract T Build(IClientConnectionInfo clientConnectionInfo);
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
