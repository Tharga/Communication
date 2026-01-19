namespace Tharga.Communication.Server;

public abstract class ClientRepositoryBase<T> where T : IClientConnectionInfo
{
    public abstract IAsyncEnumerable<T> GetAsync();
    public abstract Task<T> GetAsync(string connectionId);
    public abstract Task UpsertAsync(T clientConnectionInfo);
    public abstract Task<T> DeleteAsync(string connectionId);
}