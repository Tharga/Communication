namespace Tharga.Communication.Server.Communication;

public interface IServerCommunication
{
    event EventHandler<PendingRequestEventArgs> PendingRequestEvent;

    Task PostAsync<T>(string connectionId, T message);
    Task PostToAllAsync<T>(T message);

    Task<Response<TResponse>> SendMessageAsync<TRequest, TResponse>(string connectionId, TRequest message, TimeSpan? timeout = null);
    Dictionary<string, DateTime> GetPendingAsync();
}