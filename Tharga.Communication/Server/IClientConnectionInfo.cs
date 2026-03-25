namespace Tharga.Communication.Server;

/// <summary>
/// Describes the connection state and metadata of a connected client.
/// </summary>
public interface IClientConnectionInfo
{
    /// <summary>Gets the unique instance identifier for the client application.</summary>
    Guid Instance { get; }

    /// <summary>Gets the SignalR connection ID.</summary>
    string ConnectionId { get; }

    /// <summary>Gets the machine name the client is running on.</summary>
    string Machine { get; }

    /// <summary>Gets the client application type.</summary>
    string Type { get; }

    /// <summary>Gets the client application version.</summary>
    string Version { get; }

    /// <summary>Gets or initializes whether the client is currently connected.</summary>
    bool IsConnected { get; init; }

    /// <summary>Gets the UTC time when the client connected.</summary>
    DateTime ConnectTime { get; }

    /// <summary>Gets or initializes the UTC time when the client disconnected, if applicable.</summary>
    DateTime? DisconnectTime { get; init; }
}