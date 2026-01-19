namespace Tharga.Communication.MessageHandler;

public abstract class PostMessageHandlerBase<T>
{
    protected string ConnectionId { get; private set; }

    public abstract Task Handle(T message);
}