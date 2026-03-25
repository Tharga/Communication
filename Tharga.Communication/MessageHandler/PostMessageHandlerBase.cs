namespace Tharga.Communication.MessageHandler;

/// <summary>
/// Base class for handling fire-and-forget messages. Inherit from this class and
/// implement <see cref="Handle"/> to process incoming post messages of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The message payload type this handler processes.</typeparam>
public abstract class PostMessageHandlerBase<T>
{
    /// <summary>Gets the SignalR connection ID of the client that sent the message.</summary>
    protected string ConnectionId { get; private set; }

    /// <summary>
    /// Processes the incoming message.
    /// </summary>
    /// <param name="message">The deserialized message payload.</param>
    public abstract Task Handle(T message);
}