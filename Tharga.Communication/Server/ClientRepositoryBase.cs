namespace Tharga.Communication.Server;

/// <summary>
/// Abstract repository for storing and retrieving client connection information.
/// Implement this class to provide custom storage (e.g. database-backed).
/// </summary>
/// <typeparam name="T">The client connection info type, must implement <see cref="IClientConnectionInfo"/>.</typeparam>
public abstract class ClientRepositoryBase<T> where T : IClientConnectionInfo
{
    /// <summary>Gets all stored client connections.</summary>
    public abstract IAsyncEnumerable<T> GetAsync();

    /// <summary>Gets a client connection by its SignalR connection ID.</summary>
    /// <param name="connectionId">The connection ID to look up.</param>
    public abstract Task<T> GetAsync(string connectionId);

    /// <summary>Inserts or updates a client connection record.</summary>
    /// <param name="clientConnectionInfo">The connection info to upsert.</param>
    public abstract Task UpsertAsync(T clientConnectionInfo);

    /// <summary>Removes a client connection record and returns the removed entry.</summary>
    /// <param name="connectionId">The connection ID to remove.</param>
    public abstract Task<T> DeleteAsync(string connectionId);
}