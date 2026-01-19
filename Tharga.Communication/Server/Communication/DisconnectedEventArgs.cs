namespace Tharga.Communication.Server.Communication;

public class DisconnectedEventArgs : EventArgs
{
    public DisconnectedEventArgs(IClientConnectionInfo item)
    {
        Item = item;
    }

    public IClientConnectionInfo Item { get; }
}