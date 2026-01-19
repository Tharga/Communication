namespace Tharga.Communication.Server.Communication;

public class ConnectionChangedEventArgs : EventArgs
{
    public ConnectionChangedEventArgs(IClientConnectionInfo clientConnectionInfo)
    {
        ClientConnectionInfo = clientConnectionInfo;
    }

    public IClientConnectionInfo ClientConnectionInfo { get; }
}