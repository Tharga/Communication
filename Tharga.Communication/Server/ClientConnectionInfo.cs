using System.Text.Json.Serialization;

namespace Tharga.Communication.Server;

/// <summary>
/// Default implementation of <see cref="IClientConnectionInfo"/> that extends <see cref="ClientConnection"/>
/// with connection state and timing information.
/// </summary>
public record ClientConnectionInfo : ClientConnection, IClientConnectionInfo
{
    /// <inheritdoc />
    public required bool IsConnected { get; init; }

    /// <inheritdoc />
    public required DateTime ConnectTime { get; init; }

    /// <inheritdoc />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime? DisconnectTime { get; init; }
}