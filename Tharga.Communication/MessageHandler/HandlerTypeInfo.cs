using System.Reflection;

namespace Tharga.Communication.MessageHandler;

/// <summary>
/// Describes a registered message handler, mapping a payload type to its handler type and method.
/// </summary>
public record HandlerTypeInfo
{
    /// <summary>Gets the message payload type this handler processes.</summary>
    public Type PayloadType { get; init; }

    /// <summary>Gets the handler class type.</summary>
    public Type HandlerType { get; init; }

    /// <summary>Gets the <c>Handle</c> method to invoke on the handler.</summary>
    public MethodInfo HandlerMethod { get; init; }
}