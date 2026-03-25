namespace Tharga.Communication.MessageHandler;

/// <summary>
/// Resolves message handler registrations by payload type.
/// </summary>
public interface IHandlerTypeService
{
    /// <summary>
    /// Attempts to find a registered handler for the specified payload type.
    /// </summary>
    /// <param name="type">The payload type to look up.</param>
    /// <param name="handlerTypeInfo">When found, contains the handler metadata.</param>
    /// <returns><c>true</c> if a handler was found; otherwise <c>false</c>.</returns>
    bool TryGetHandler(Type type, out HandlerTypeInfo handlerTypeInfo);
}