using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.SignalR.Client;
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
        await Task.Delay(100, TestContext.Current.CancellationToken);

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
    public async Task ExecuteAsync_ServerUnreachable_DisposesThePreviousConnectionBeforeRetrying()
    {
        var instanceService = new Mock<IInstanceService>();
        instanceService.Setup(x => x.AgentInstanceKey).Returns(Guid.NewGuid());
        var sut = new SignalRHostedService(
            instanceService.Object,
            new Mock<IMessageExecutor>().Object,
            Options.Create(new ClientOptions { ServerAddress = $"http://127.0.0.1:{UnusedPort()}", Pattern = "hub", ReconnectDelays = [TimeSpan.FromSeconds(1)] }),
            new ClientResponseMediator(),
            new Mock<ILogger<SignalRHostedService>>().Object)
        {
            RetryDelay = TimeSpan.FromMilliseconds(50)
        };
        var field = typeof(SignalRHostedService).GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var ct = TestContext.Current.CancellationToken;

        await sut.StartAsync(ct);
        var first = await WaitForAsync(() => (HubConnection)field.GetValue(sut), null, ct);
        await WaitForAsync(() => (HubConnection)field.GetValue(sut), first, ct);
        await sut.StopAsync(CancellationToken.None);

        var restart = () => first.StartAsync(ct);
        await restart.Should().ThrowAsync<ObjectDisposedException>();
    }

    private static int UnusedPort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<HubConnection> WaitForAsync(Func<HubConnection> read, HubConnection differentFrom, CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(TimeSpan.FromSeconds(10));
        while (true)
        {
            var current = read();
            if (current is not null && !ReferenceEquals(current, differentFrom)) return current;
            await Task.Delay(10, limit.Token);
        }
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
