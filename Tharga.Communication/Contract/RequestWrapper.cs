namespace Tharga.Communication.Contract;

/// <summary>
/// Default implementation of <see cref="IMessageWrapper"/> used to wrap serialized messages for transport.
/// </summary>
public record RequestWrapper : IMessageWrapper
{
    /// <inheritdoc />
    public required string Type { get; init; }

    /// <inheritdoc />
    public required string Payload { get; init; }
}