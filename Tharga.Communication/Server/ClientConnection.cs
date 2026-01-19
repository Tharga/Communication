namespace Tharga.Communication.Server;

public record ClientConnection
{
    public required Guid Instance { get; init; }
    public required string ConnectionId { get; init; }
    public required string Machine { get; init; }
    public required string Type { get; init; }
    public required string Version { get; init; }
}