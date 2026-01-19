namespace Tharga.Communication.Client.Communication;

public interface IClientCommunication
{
    Task PostAsync<T>(T message);
    Task<TResponse> SendMessage<TRequest, TResponse>(TRequest message);
    bool IsConnected { get; }
}