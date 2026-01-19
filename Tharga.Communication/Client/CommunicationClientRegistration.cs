using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tharga.Communication.Client.Communication;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;

namespace Tharga.Communication.Client;

public static class CommunicationClientRegistration
{
    public static void AddThargaCommunicationClient(this IHostApplicationBuilder builder, Action<CommunicationOptions> options = default)
    {
        var configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
        var value = configuration.GetSection("Tharga:Communication").Get<CommunicationOptions>();

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

        builder.Services.AddSingleton<IClientCommunication, Communication.ClientCommunication>();
        builder.Services.AddTransient<IMessageExecutor, MessageExecutor>();
        var handlerTypes = HandlerTypeService.GetHandlerTypes(builder.Services);
        builder.Services.AddSingleton<IHandlerTypeService>(_ => new HandlerTypeService(handlerTypes));
    }
}