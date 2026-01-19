using System.Text.Json.Serialization;

namespace Tharga.Communication.Server;

public record ClientConnectionInfo : ClientConnection, IClientConnectionInfo
{
    public required bool IsConnected { get; init; }
    public required DateTime ConnectTime { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime? DisconnectTime { get; init; }
}