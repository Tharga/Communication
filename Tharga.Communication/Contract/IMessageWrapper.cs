namespace Tharga.Communication.Contract;

/// <summary>
/// Wraps a serialized message with its type information for routing and deserialization.
/// </summary>
public interface IMessageWrapper
{
    /// <summary>Gets the assembly-qualified type name of the message payload.</summary>
    string Type { get; }

    /// <summary>Gets the JSON-serialized message payload.</summary>
    string Payload { get; }
}