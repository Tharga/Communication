using Tharga.Communication.Contract;

namespace Tharga.Communication.Client;

/// <summary>
/// Internal mediator that allows <see cref="SignalRHostedService"/> to deliver
/// ResponseMessage payloads to <see cref="Communication.ClientCommunication"/>
/// without a direct dependency between them.
/// </summary>
internal class ClientResponseMediator
{
    internal event EventHandler<IMessageWrapper> ResponseReceived;

    internal void Deliver(IMessageWrapper response)
    {
        ResponseReceived?.Invoke(this, response);
    }
}
