namespace Tharga.Communication.Server;

/// <summary>
/// Represents the metadata sent by a client when it connects to the server hub.
/// </summary>
public record ClientConnection
{
    /// <summary>Gets the unique instance identifier for the client application.</summary>
    public required Guid Instance { get; init; }

    /// <summary>Gets the SignalR connection ID.</summary>
    public required string ConnectionId { get; init; }

    /// <summary>Gets the machine name the client is running on.</summary>
    public required string Machine { get; init; }

    /// <summary>Gets the client application type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the client application version.</summary>
    public required string Version { get; init; }
}