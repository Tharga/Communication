using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    private const string ConfigurationSection = "Tharga:Communication";

    /// <summary>
    /// Registers all required client-side communication services including SignalR connection,
    /// message handlers, and the <see cref="Communication.IClientCommunication"/> service.
    /// Every option in <see cref="CommunicationOptions"/> except <see cref="CommunicationOptions.AdditionalAssemblies"/>
    /// can be set in the <c>Tharga:Communication</c> configuration section. The <paramref name="options"/> callback runs after configuration is bound,
    /// so values set in code take precedence.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="options">Optional callback to override configuration values.</param>
    public static void AddThargaCommunicationClient(this IHostApplicationBuilder builder, Action<CommunicationOptions> options = default)
    {
        builder.Services.AddThargaCommunicationClient(builder.Configuration, options);
    }

    /// <summary>
    /// Registers all required client-side communication services on an <see cref="IServiceCollection"/>,
    /// for hosts that are not built with an <see cref="IHostApplicationBuilder"/> and for libraries that register their own services.
    /// No service provider is built during registration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// Optional configuration. When given, the whole <c>Tharga:Communication</c> section is bound to <see cref="CommunicationOptions"/>,
    /// except <see cref="CommunicationOptions.AdditionalAssemblies"/>, which is set in code only.
    /// </param>
    /// <param name="options">Optional callback that runs after the configuration is bound, so values set in code take precedence.</param>
    public static void AddThargaCommunicationClient(this IServiceCollection services, IConfiguration configuration = null, Action<CommunicationOptions> options = null)
    {
        var o = new CommunicationOptions();
        configuration?.GetSection(ConfigurationSection).Bind(o);
        o.Pattern ??= Constants.DefaultPattern;
        o.ReconnectDelays ??= DefaultReconnectDelays();
        options?.Invoke(o);
        services.AddSingleton(Options.Create(o));

        services.AddSingleton<IInstanceService, InstanceService>();
        services.AddSingleton<SignalRHostedService>();
        services.AddSingleton<ISignalRHostedService>(sp => sp.GetRequiredService<SignalRHostedService>());
        services.AddHostedService(sp => sp.GetRequiredService<SignalRHostedService>());

        services.AddSingleton<ClientResponseMediator>();
        services.TryAddSingleton<SubscriptionStateTracker>();
        services.AddSingleton<IClientCommunication, ClientCommunication>();
        services.AddTransient<IMessageExecutor, MessageExecutor>();
        var handlerTypes = HandlerTypeService.GetHandlerTypes(services, o.AdditionalAssemblies);
        services.AddSingleton<IHandlerTypeService>(_ => new HandlerTypeService(handlerTypes));
    }

    private static TimeSpan[] DefaultReconnectDelays()
    {
        return [TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)];
    }
}
