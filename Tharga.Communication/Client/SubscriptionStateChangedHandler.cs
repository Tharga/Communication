using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;

namespace Tharga.Communication.Client;

/// <summary>
/// Built-in handler that receives <see cref="SubscriptionStateChanged"/> messages from the server
/// and updates the local <see cref="SubscriptionStateTracker"/>.
/// </summary>
internal class SubscriptionStateChangedHandler : PostMessageHandlerBase<SubscriptionStateChanged>
{
    private readonly SubscriptionStateTracker _tracker;

    public SubscriptionStateChangedHandler(SubscriptionStateTracker tracker)
    {
        _tracker = tracker;
    }

    public override Task Handle(SubscriptionStateChanged message)
    {
        _tracker.Update(message);
        return Task.CompletedTask;
    }
}
