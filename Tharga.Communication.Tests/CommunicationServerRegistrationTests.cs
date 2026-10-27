using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Communication.Mcp;
using Tharga.Communication.Server;
using Xunit;

namespace Tharga.Communication.Tests;

public class CommunicationServerRegistrationTests
{
    [Fact]
    public void AddThargaCommunicationServer_WithoutClientStateService_ThrowsInvalidOperation()
    {
        var builder = WebApplication.CreateBuilder();

        var act = () => builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Client Service Type*");
    }

    [Fact]
    public void AddThargaCommunicationServer_WithoutClientRepository_ThrowsInvalidOperation()
    {
        var builder = WebApplication.CreateBuilder();

        var act = () => builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterClientStateService<TestClientStateService>();
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Client Repository Type*");
    }

    [Fact]
    public void AddThargaCommunicationServer_WithRequiredServices_DoesNotThrow()
    {
        var builder = WebApplication.CreateBuilder();

        var act = () => builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterClientStateService<TestClientStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void AddThargaCommunicationServer_ClientStateServiceBase_ResolvesRegisteredService()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterClientStateService<TestClientStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });
        using var app = builder.Build();

        var baseService = app.Services.GetService<ClientStateServiceBase>();

        baseService.Should().NotBeNull();
        baseService.Should().BeSameAs(app.Services.GetRequiredService<TestClientStateService>());
    }

    [Fact]
    public void AddThargaCommunicationServer_WithInterface_ClientStateServiceBase_ResolvesRegisteredService()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterClientStateService<ITestClientStateService, TestClientStateServiceWithInterface>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });
        using var app = builder.Build();

        var baseService = app.Services.GetService<ClientStateServiceBase>();

        baseService.Should().NotBeNull();
        baseService.Should().BeSameAs(app.Services.GetRequiredService<ITestClientStateService>());
    }

    [Fact]
    public void AddThargaCommunicationServer_McpResourceProvider_CanBeActivated()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterClientStateService<TestClientStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });
        using var app = builder.Build();

        var act = () => ActivatorUtilities.CreateInstance<CommunicationResourceProvider>(app.Services);

        act.Should().NotThrow();
    }

    private interface ITestClientStateService;

    private class TestClientStateServiceWithInterface : TestClientStateService, ITestClientStateService;

    private class TestClientStateService : ClientStateServiceBase
    {
        public override Task ConnectAsync(ClientConnection clientConnection) => Task.CompletedTask;
        public override Task DisconnectedAsync(string connectionId) => Task.CompletedTask;
        public override async IAsyncEnumerable<IClientConnectionInfo> GetConnectionInfosAsync()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
