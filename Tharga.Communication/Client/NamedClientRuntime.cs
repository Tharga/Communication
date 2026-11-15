using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tharga.Communication.Client.Communication;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;

namespace Tharga.Communication.Client;

/// <summary>
/// Everything one named client owns: its options, connection, reconnect state, subscription state and handlers.
/// </summary>
internal sealed class NamedClientRuntime
{
    public NamedClientRuntime(CommunicationOptions options, IHandlerTypeService handlerTypes, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        var optionsAccessor = Options.Create(options);
        var responseMediator = new ClientResponseMediator();
        var tracker = new SubscriptionStateTracker();
        var executor = new SubscriptionAwareMessageExecutor(new MessageExecutor(serviceProvider, handlerTypes), tracker);

        OptionsAccessor = optionsAccessor;
        HandlerTypes = handlerTypes;
        Tracker = tracker;
        HostedService = new SignalRHostedService(new InstanceService(), executor, optionsAccessor, responseMediator, loggerFactory.CreateLogger<SignalRHostedService>());
        Communication = new ClientCommunication(HostedService, responseMediator, tracker, optionsAccessor, loggerFactory.CreateLogger<ClientCommunication>());
    }

    public IOptions<CommunicationOptions> OptionsAccessor { get; }
    public IHandlerTypeService HandlerTypes { get; }
    public SubscriptionStateTracker Tracker { get; }
    public SignalRHostedService HostedService { get; }
    public IClientCommunication Communication { get; }

    private sealed class SubscriptionAwareMessageExecutor : IMessageExecutor
    {
        private readonly IMessageExecutor _inner;
        private readonly SubscriptionStateTracker _tracker;

        public SubscriptionAwareMessageExecutor(IMessageExecutor inner, SubscriptionStateTracker tracker)
        {
            _inner = inner;
            _tracker = tracker;
        }

        public Task<IMessageWrapper> ExecuteAsync(string connectionId, IMessageWrapper wrapper)
        {
            if (wrapper.Type?.Split(',')[0].Trim() == typeof(SubscriptionStateChanged).FullName)
            {
                _tracker.Update(JsonSerializer.Deserialize<SubscriptionStateChanged>(wrapper.Payload));
                return Task.FromResult<IMessageWrapper>(null);
            }

            return _inner.ExecuteAsync(connectionId, wrapper);
        }
    }
}
