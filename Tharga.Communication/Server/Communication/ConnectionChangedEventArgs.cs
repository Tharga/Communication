namespace Tharga.Communication.Server.Communication;

/// <summary>
/// Event arguments raised when a client connection state changes.
/// </summary>
public class ConnectionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance with the connection info of the affected client.
    /// </summary>
    /// <param name="clientConnectionInfo">The client connection information.</param>
    public ConnectionChangedEventArgs(IClientConnectionInfo clientConnectionInfo)
    {
        ClientConnectionInfo = clientConnectionInfo;
    }

    /// <summary>Gets the connection information of the affected client.</summary>
    public IClientConnectionInfo ClientConnectionInfo { get; }
}