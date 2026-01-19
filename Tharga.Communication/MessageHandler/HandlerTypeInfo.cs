using System.Reflection;

namespace Tharga.Communication.MessageHandler;

public record HandlerTypeInfo
{
    public Type PayloadType { get; init; }
    public Type HandlerType { get; init; }
    public MethodInfo HandlerMethod { get; init; }
}