using System.Collections.Concurrent;

namespace Tharga.Communication.Server;

public class MemoryClientRepository<T> : ClientRepositoryBase<T>
    where T : IClientConnectionInfo
{
    private readonly ConcurrentDictionary<Guid, T> _clients = new();

    public override async IAsyncEnumerable<T> GetAsync()
    {
        foreach (var item in _clients.Values)
        {
            yield return item;
        }
    }

    public override async Task<T> GetAsync(string connectionId)
    {
        var item = _clients.FirstOrDefault(x => x.Value.ConnectionId == connectionId);
        return item.Value;
    }

    public override Task UpsertAsync(T clientConnectionInfo)
    {
        _clients[clientConnectionInfo.Instance] = clientConnectionInfo;
        return Task.CompletedTask;
    }

    public override async Task<T> DeleteAsync(string connectionId)
    {
        var item = _clients.FirstOrDefault(x => x.Value.ConnectionId == connectionId);
        if (_clients.TryRemove(item.Key, out var deleted)) return deleted;
        return default;
    }
}