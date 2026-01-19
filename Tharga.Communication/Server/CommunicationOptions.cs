using Tharga.Communication.Server;

public record CommunicationOptions
{
    internal (Type Interface, Type Service) _clientStateServiceType;
    internal (Type Interface, Type Service) _clientRepositoryType;

    public void RegisterClientStateService<TService>()
        where TService : ClientStateServiceBase
    {
        _clientStateServiceType = (typeof(TService), typeof(TService));
    }

    public void RegisterClientStateService<TInterface, TService>()
        where TService : ClientStateServiceBase, TInterface
    {
        _clientStateServiceType = (typeof(TInterface), typeof(TService));
    }

    public void RegisterClientRepository<TService, TEntity>()
        where TService : ClientRepositoryBase<TEntity>
        where TEntity : IClientConnectionInfo
    {
        _clientRepositoryType = (typeof(TService), typeof(TService));
    }

    public void RegisterClientRepository<TInterface, TService, TEntity>()
        where TService : ClientRepositoryBase<TEntity>, TInterface
        where TEntity : IClientConnectionInfo
    {
        _clientRepositoryType = (typeof(TInterface), typeof(TService));
    }
}