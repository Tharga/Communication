namespace Tharga.Communication.Contract;

public record RequestWrapper : IMessageWrapper
{
    public required string Type { get; init; }
    public required string Payload { get; init; }
}