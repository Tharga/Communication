using FluentAssertions;
using Microsoft.AspNetCore.Builder;
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

    private class TestClientStateService : ClientStateServiceBase
    {
        public override Task ConnectAsync(ClientConnection clientConnection) => Task.CompletedTask;
        public override Task DisconnectedAsync(string connectionId) => Task.CompletedTask;
    }
}
