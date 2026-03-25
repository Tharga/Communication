using Tharga.Communication.Server;

/// <summary>
/// Configuration options for the Tharga.Communication server.
/// </summary>
public record CommunicationOptions
{
    internal (Type Interface, Type Service) _clientStateServiceType;
    internal (Type Interface, Type Service) _clientRepositoryType;

    /// <summary>
    /// Registers a client state service type using the concrete type as both interface and implementation.
    /// </summary>
    /// <typeparam name="TService">The client state service type.</typeparam>
    public void RegisterClientStateService<TService>()
        where TService : ClientStateServiceBase
    {
        _clientStateServiceType = (typeof(TService), typeof(TService));
    }

    /// <summary>
    /// Registers a client state service with a separate interface and implementation type.
    /// </summary>
    /// <typeparam name="TInterface">The service interface.</typeparam>
    /// <typeparam name="TService">The service implementation.</typeparam>
    public void RegisterClientStateService<TInterface, TService>()
        where TService : ClientStateServiceBase, TInterface
    {
        _clientStateServiceType = (typeof(TInterface), typeof(TService));
    }

    /// <summary>
    /// Registers a client repository type using the concrete type as both interface and implementation.
    /// </summary>
    /// <typeparam name="TService">The repository type.</typeparam>
    /// <typeparam name="TEntity">The client connection info entity type.</typeparam>
    public void RegisterClientRepository<TService, TEntity>()
        where TService : ClientRepositoryBase<TEntity>
        where TEntity : IClientConnectionInfo
    {
        _clientRepositoryType = (typeof(TService), typeof(TService));
    }

    /// <summary>
    /// Registers a client repository with a separate interface and implementation type.
    /// </summary>
    /// <typeparam name="TInterface">The repository interface.</typeparam>
    /// <typeparam name="TService">The repository implementation.</typeparam>
    /// <typeparam name="TEntity">The client connection info entity type.</typeparam>
    public void RegisterClientRepository<TInterface, TService, TEntity>()
        where TService : ClientRepositoryBase<TEntity>, TInterface
        where TEntity : IClientConnectionInfo
    {
        _clientRepositoryType = (typeof(TInterface), typeof(TService));
    }
}