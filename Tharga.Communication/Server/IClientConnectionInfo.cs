namespace Tharga.Communication.Server;

public interface IClientConnectionInfo
{
    Guid Instance { get; }
    string ConnectionId { get; }
    string Machine { get; }
    string Type { get; }
    string Version { get; }
    bool IsConnected { get; init; }
    DateTime ConnectTime { get; }
    DateTime? DisconnectTime { get; init; }
}