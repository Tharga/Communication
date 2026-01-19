using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;
using Tharga.Communication.Server.Communication;

namespace Tharga.Communication.Server;

public static class CommunicationServerRegistration
{
    public static void AddThargaCommunicationServer(this WebApplicationBuilder builder, Action<CommunicationOptions> options = default)
    {
        var o = new CommunicationOptions
        {
        };
        options?.Invoke(o);
        builder.Services.AddSingleton(Options.Create(o));

        builder.Services.AddSignalR(c =>
        {
            if (Debugger.IsAttached)
            {
                // Hur länge servern väntar på livstecken från klienten innan den stänger anslutningen.
                c.ClientTimeoutInterval = TimeSpan.FromMinutes(10);

                // Hur ofta servern pingar klienten.
                c.KeepAliveInterval = TimeSpan.FromSeconds(30);

                // Om du har en LB/proxy kan det hjälpa att öka detta också.
                c.HandshakeTimeout = TimeSpan.FromSeconds(30);
            }
        });

        if (o._clientStateServiceType.Service == null) throw new InvalidOperationException($"Client Service Type has to be provided in {nameof(CommunicationOptions)}.{nameof(CommunicationOptions.RegisterClientStateService)}<>.");
        builder.Services.AddSingleton(o._clientStateServiceType.Interface, o._clientStateServiceType.Service);

        if (o._clientRepositoryType.Service == null) throw new InvalidOperationException($"Client Repository Type has to be provided in {nameof(CommunicationOptions)}.{nameof(CommunicationOptions.RegisterClientRepository)}<>.");
        builder.Services.AddSingleton(o._clientRepositoryType.Interface, o._clientRepositoryType.Service);

        builder.Services.AddSingleton<IServerCommunication, ServerCommunication>();
        builder.Services.AddTransient<IMessageExecutor, MessageExecutor>();
        var handlerTypes = HandlerTypeService.GetHandlerTypes(builder.Services);
        builder.Services.AddSingleton<IHandlerTypeService>(_ => new HandlerTypeService(handlerTypes));
    }

    public static void UseThargaCommunicationServer(this WebApplication app, string pattern = default)
    {
        pattern ??= Constants.DefaultPattern;
        app.MapHub<SignalRHub>($"/{pattern.TrimStart('/')}");
    }
}