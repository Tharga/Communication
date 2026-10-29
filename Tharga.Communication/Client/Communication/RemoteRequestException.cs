namespace Tharga.Communication.Client.Communication;

/// <summary>
/// Thrown by <see cref="IClientCommunication.SendMessage{TRequest, TResponse}"/> when the server could not handle the request,
/// for example because its handler threw or because no handler is registered for the request type.
/// </summary>
public class RemoteRequestException : Exception
{
    /// <summary>
    /// Initializes a new instance with the reason the server gave.
    /// </summary>
    /// <param name="message">The error message.</param>
    public RemoteRequestException(string message)
        : base(message)
    {
    }
}
