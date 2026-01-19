using Tharga.Communication.Contract;

namespace Tharga.Communication.Server;

internal class ResponseEventArgs : EventArgs
{
    public ResponseEventArgs(string connectionId, IMessageWrapper response)
    {
        ConnectionId = connectionId;
        Response = response;
    }

    public string ConnectionId { get; }
    public IMessageWrapper Response { get; }
}