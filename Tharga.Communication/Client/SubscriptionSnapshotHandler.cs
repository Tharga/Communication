using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;

namespace Tharga.Communication.Client;

/// <summary>
/// Built-in handler that receives <see cref="SubscriptionSnapshot"/> messages from the server
/// and replaces the state of the local <see cref="SubscriptionStateTracker"/>.
/// </summary>
internal class SubscriptionSnapshotHandler : PostMessageHandlerBase<SubscriptionSnapshot>
{
    private readonly SubscriptionStateTracker _tracker;

    public SubscriptionSnapshotHandler(SubscriptionStateTracker tracker)
    {
        _tracker = tracker;
    }

    public override Task Handle(SubscriptionSnapshot message)
    {
        _tracker.Replace(message);
        return Task.CompletedTask;
    }
}
