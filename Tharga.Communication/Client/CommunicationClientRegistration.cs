using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tharga.Communication.Client.Communication;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;

namespace Tharga.Communication.Client;

/// <summary>
/// Extension methods for registering Tharga.Communication client services.
/// </summary>
public static class CommunicationClientRegistration
{
    /// <summary>
    /// Registers all required client-side communication services including SignalR connection,
    /// message handlers, and the <see cref="Communication.IClientCommunication"/> service.
    /// Configuration is read from the <c>Tharga:Communication</c> configuration section.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="options">Optional callback to override configuration values.</param>
    public static void AddThargaCommunicationClient(this IHostApplicationBuilder builder, Action<CommunicationOptions> options = default)
    {
        var configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
        var value = configuration.GetSection("Tharga:Communication").Get<CommunicationOptions>() ?? new CommunicationOptions();

        var o = new CommunicationOptions
        {
            ServerAddress = value.ServerAddress,
            Pattern = value.Pattern ?? Constants.DefaultPattern,
            ReconnectDelays = [TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)]
        };
        options?.Invoke(o);
        builder.Services.AddSingleton(Options.Create(o));

        builder.Services.AddSingleton<IInstanceService, InstanceService>();
        builder.Services.AddSingleton<SignalRHostedService>();
        builder.Services.AddSingleton<ISignalRHostedService>(sp => sp.GetRequiredService<SignalRHostedService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<SignalRHostedService>());

        builder.Services.AddSingleton<SubscriptionStateTracker>();
        builder.Services.AddSingleton<IClientCommunication, Communication.ClientCommunication>();
        builder.Services.AddTransient<IMessageExecutor, MessageExecutor>();
        var handlerTypes = HandlerTypeService.GetHandlerTypes(builder.Services);
        builder.Services.AddSingleton<IHandlerTypeService>(_ => new HandlerTypeService(handlerTypes));
    }
}