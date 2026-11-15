using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// Registers a named client with its own options, connection, reconnect state, subscription state and handlers,
    /// next to the default client. The configuration section <c>Tharga:Communication:&lt;name&gt;</c> is bound, then the <paramref name="options"/> callback runs.
    /// Resolve the client with <c>[FromKeyedServices("name")]</c> as <see cref="Communication.IClientCommunication"/>,
    /// <see cref="ISignalRHostedService"/> (connection state events) or <see cref="IHandlerTypeService"/>.
    /// A named client scans only the Tharga.Communication assembly and <see cref="CommunicationOptions.AdditionalAssemblies"/> for handlers,
    /// not the application's own assemblies, so a handler registered for one client does not run for messages on another.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="name">The name of the client.</param>
    /// <param name="options">Optional callback to override configuration values.</param>
    public static void AddThargaCommunicationClient(this IHostApplicationBuilder builder, string name, Action<CommunicationOptions> options = null)
    {
        builder.Services.AddThargaCommunicationClient(name, builder.Configuration, options);
    }

    /// <summary>
    /// Registers a named client on an <see cref="IServiceCollection"/>. See
    /// <see cref="AddThargaCommunicationClient(IHostApplicationBuilder,string,Action{CommunicationOptions})"/> for what a named client owns.
    /// No configuration is bound.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the client.</param>
    /// <param name="options">Callback that sets the client options.</param>
    public static void AddThargaCommunicationClient(this IServiceCollection services, string name, Action<CommunicationOptions> options)
    {
        services.AddThargaCommunicationClient(name, null, options);
    }

    /// <summary>
    /// Registers a named client on an <see cref="IServiceCollection"/>. See
    /// <see cref="AddThargaCommunicationClient(IHostApplicationBuilder,string,Action{CommunicationOptions})"/> for what a named client owns.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the client.</param>
    /// <param name="configuration">Optional configuration. When given, the <c>Tharga:Communication:&lt;name&gt;</c> section is bound.</param>
    /// <param name="options">Optional callback that runs after the configuration is bound, so values set in code take precedence.</param>
    public static void AddThargaCommunicationClient(this IServiceCollection services, string name, IConfiguration configuration, Action<CommunicationOptions> options = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A client name is required.", nameof(name));
        if (services.Any(x => x.ServiceType == typeof(NamedClientRuntime) && Equals(x.ServiceKey, name)))
        {
            throw new InvalidOperationException($"A Tharga communication client named '{name}' is already registered.");
        }

        var o = new CommunicationOptions();
        configuration?.GetSection($"{ConfigurationSection}:{name}").Bind(o);
        o.Pattern ??= Constants.DefaultPattern;
        o.ReconnectDelays ??= DefaultReconnectDelays();
        options?.Invoke(o);

        var handlerTypes = new HandlerTypeService(HandlerTypeService.GetHandlerTypes(services, o.AdditionalAssemblies, false));

        services.AddKeyedSingleton(name, (sp, _) => new NamedClientRuntime(o, handlerTypes, sp, sp.GetService<ILoggerFactory>()));
        services.AddKeyedSingleton<IOptions<CommunicationOptions>>(name, (sp, key) => sp.GetRequiredKeyedService<NamedClientRuntime>(key).OptionsAccessor);
        services.AddKeyedSingleton<IHandlerTypeService>(name, (sp, key) => sp.GetRequiredKeyedService<NamedClientRuntime>(key).HandlerTypes);
        services.AddKeyedSingleton<ISignalRHostedService>(name, (sp, key) => sp.GetRequiredKeyedService<NamedClientRuntime>(key).HostedService);
        services.AddKeyedSingleton<IClientCommunication>(name, (sp, key) => sp.GetRequiredKeyedService<NamedClientRuntime>(key).Communication);
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredKeyedService<NamedClientRuntime>(name).HostedService);
    }

    private static TimeSpan[] DefaultReconnectDelays()
    {
        return [TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)];
    }
}
