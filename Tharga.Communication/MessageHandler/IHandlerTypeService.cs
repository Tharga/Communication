namespace Tharga.Communication.MessageHandler;

public interface IHandlerTypeService
{
    bool TryGetHandler(Type type, out HandlerTypeInfo handlerTypeInfo);
}