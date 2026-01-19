namespace Tharga.Communication.MessageHandler;

public abstract class SendMessageHandlerBase<TRequest, TResponse>
{
    public abstract Task<TResponse> Handle(TRequest message);
}