namespace Tharga.Communication.Server.Communication;

/// <summary>
/// Event arguments raised when a pending request is added or completed.
/// </summary>
public class PendingRequestEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance with request tracking details.
    /// </summary>
    /// <param name="connectionId">The connection ID associated with the request.</param>
    /// <param name="added"><c>true</c> if the request was added; <c>false</c> if it was completed or removed.</param>
    /// <param name="createDate">The UTC time the request was created.</param>
    public PendingRequestEventArgs(string connectionId, bool added, DateTime createDate)
    {
        ConnectionId = connectionId;
        Added = added;
        CreateDate = createDate;
    }

    /// <summary>Gets the connection ID associated with the request.</summary>
    public string ConnectionId { get; }

    /// <summary>Gets whether the request was added (<c>true</c>) or completed/removed (<c>false</c>).</summary>
    public bool Added { get; }

    /// <summary>Gets the UTC time the request was created.</summary>
    public DateTime CreateDate { get; }
}