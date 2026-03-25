namespace Tharga.Communication.MessageHandler;

/// <summary>
/// Base class for handling request-response messages. Inherit from this class and
/// implement <see cref="Handle"/> to process requests of type <typeparamref name="TRequest"/>
/// and return a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The request payload type.</typeparam>
/// <typeparam name="TResponse">The response payload type.</typeparam>
public abstract class SendMessageHandlerBase<TRequest, TResponse>
{
    /// <summary>
    /// Processes the incoming request and returns a response.
    /// </summary>
    /// <param name="message">The deserialized request payload.</param>
    /// <returns>The response to send back to the caller.</returns>
    public abstract Task<TResponse> Handle(TRequest message);
}