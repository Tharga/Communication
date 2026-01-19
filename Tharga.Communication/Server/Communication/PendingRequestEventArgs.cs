namespace Tharga.Communication.Server.Communication;

public class PendingRequestEventArgs : EventArgs
{
    public PendingRequestEventArgs(string connectionId, bool added, DateTime createDate)
    {
        ConnectionId = connectionId;
        Added = added;
        CreateDate = createDate;
    }

    public string ConnectionId { get; }
    public bool Added { get; }
    public DateTime CreateDate { get; }
}