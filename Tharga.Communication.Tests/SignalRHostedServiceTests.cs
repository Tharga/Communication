using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Tharga.Communication.Client;
using Tharga.Communication.MessageHandler;
using Xunit;
using ClientOptions = Tharga.Communication.Client.CommunicationOptions;

namespace Tharga.Communication.Tests;

public class SignalRHostedServiceTests
{
    [Fact]
    public void Constructor_NullServerAddress_DoesNotThrow()
    {
        var options = Options.Create(new ClientOptions
        {
            ServerAddress = null,
            Pattern = "hub"
        });

        var act = () => new SignalRHostedService(
            new Mock<IInstanceService>().Object,
            new Mock<IMessageExecutor>().Object,
            options,
            new ClientResponseMediator(),
            new Mock<ILogger<SignalRHostedService>>().Object);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_EmptyServerAddress_DoesNotThrow()
    {
        var options = Options.Create(new ClientOptions
        {
            ServerAddress = "",
            Pattern = "hub"
        });

        var act = () => new SignalRHostedService(
            new Mock<IInstanceService>().Object,
            new Mock<IMessageExecutor>().Object,
            options,
            new ClientResponseMediator(),
            new Mock<ILogger<SignalRHostedService>>().Object);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task ExecuteAsync_NullServerAddress_ReturnsImmediately()
    {
        var options = Options.Create(new ClientOptions
        {
            ServerAddress = null,
            Pattern = "hub"
        });

        var sut = new SignalRHostedService(
            new Mock<IInstanceService>().Object,
            new Mock<IMessageExecutor>().Object,
            options,
            new ClientResponseMediator(),
            new Mock<ILogger<SignalRHostedService>>().Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await sut.StartAsync(cts.Token);

        // Give it a moment to run ExecuteAsync
        await Task.Delay(100);

        // Should not be connected
        sut.State.Should().Be(Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected);

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void SendAsync_NoConnection_ThrowsClearException()
    {
        var options = Options.Create(new ClientOptions
        {
            ServerAddress = null,
            Pattern = "hub"
        });

        var sut = new SignalRHostedService(
            new Mock<IInstanceService>().Object,
            new Mock<IMessageExecutor>().Object,
            options,
            new ClientResponseMediator(),
            new Mock<ILogger<SignalRHostedService>>().Object);

        var act = () => sut.SendAsync("PostMessage", new object());

        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no server address*");
    }

    [Fact]
    public void ClientRegistration_WithoutServerAddress_DoesNotThrow()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true
        });

        var act = () => builder.AddThargaCommunicationClient();

        act.Should().NotThrow();
    }
}
