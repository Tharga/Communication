namespace Tharga.Communication.Server.Communication;

/// <summary>
/// Event arguments raised when a client disconnects from the server.
/// </summary>
public class DisconnectedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance with the connection info of the disconnected client.
    /// </summary>
    /// <param name="item">The connection information of the disconnected client.</param>
    public DisconnectedEventArgs(IClientConnectionInfo item)
    {
        Item = item;
    }

    /// <summary>Gets the connection information of the disconnected client.</summary>
    public IClientConnectionInfo Item { get; }
}