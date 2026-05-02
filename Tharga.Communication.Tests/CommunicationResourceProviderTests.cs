using System.Text.Json;
using FluentAssertions;
using Moq;
using Tharga.Communication.Mcp;
using Tharga.Communication.MessageHandler;
using Tharga.Communication.Server;
using Tharga.Communication.Server.Communication;
using Tharga.Mcp;
using Xunit;

namespace Tharga.Communication.Tests;

public class CommunicationResourceProviderTests
{
    private readonly Mock<IServerCommunication> _serverComm = new();
    private readonly Mock<ClientStateServiceBase> _clientState = new();
    private readonly Mock<IHandlerTypeService> _handlerTypes = new();
    private readonly CommunicationResourceProvider _sut;

    public CommunicationResourceProviderTests()
    {
        _sut = new CommunicationResourceProvider(_serverComm.Object, _clientState.Object, _handlerTypes.Object);
    }

    [Fact]
    public void Scope_IsSystem()
    {
        _sut.Scope.Should().Be(McpScope.System);
    }

    [Fact]
    public async Task ListResourcesAsync_Returns3Descriptors()
    {
        var resources = await _sut.ListResourcesAsync(Mock.Of<IMcpContext>(), CancellationToken.None);

        resources.Should().HaveCount(3);
        resources.Select(r => r.Uri).Should().BeEquivalentTo(new[]
        {
            "communication://clients",
            "communication://subscriptions",
            "communication://handlers"
        });
        resources.Should().AllSatisfy(r => r.MimeType.Should().Be("application/json"));
    }

    [Fact]
    public async Task ReadResourceAsync_Clients_ReturnsConnectedClientsAsJson()
    {
        var instance = Guid.NewGuid();
        var connectTime = DateTime.UtcNow;

        _clientState
            .Setup(x => x.GetConnectionInfosAsync())
            .Returns(AsyncEnumerable(new ClientConnectionInfo
            {
                Instance = instance,
                ConnectionId = "conn-1",
                Machine = "test-machine",
                Type = "test-app",
                Version = "1.0",
                IsConnected = true,
                ConnectTime = connectTime
            }));

        var content = await _sut.ReadResourceAsync("communication://clients", Mock.Of<IMcpContext>(), CancellationToken.None);

        content.Uri.Should().Be("communication://clients");
        content.MimeType.Should().Be("application/json");

        using var doc = JsonDocument.Parse(content.Text!);
        var clients = doc.RootElement.GetProperty("clients");
        clients.GetArrayLength().Should().Be(1);
        clients[0].GetProperty("connectionId").GetString().Should().Be("conn-1");
        clients[0].GetProperty("machine").GetString().Should().Be("test-machine");
        clients[0].GetProperty("isConnected").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ReadResourceAsync_Subscriptions_ReturnsSubscriberCounts()
    {
        _serverComm.Setup(x => x.GetSubscriptions()).Returns(new Dictionary<string, int>
        {
            { "FarmDetailsDto:1", 2 },
            { "CollectionDto", 1 }
        });

        var content = await _sut.ReadResourceAsync("communication://subscriptions", Mock.Of<IMcpContext>(), CancellationToken.None);

        using var doc = JsonDocument.Parse(content.Text!);
        var subs = doc.RootElement.GetProperty("subscriptions");
        subs.GetArrayLength().Should().Be(2);
        subs.EnumerateArray().Should().Contain(s =>
            s.GetProperty("topic").GetString() == "FarmDetailsDto:1" &&
            s.GetProperty("subscriberCount").GetInt32() == 2);
    }

    [Fact]
    public async Task ReadResourceAsync_Handlers_ReturnsRegisteredHandlers()
    {
        _handlerTypes.Setup(x => x.GetAll()).Returns(new[]
        {
            new HandlerTypeInfo
            {
                PayloadType = typeof(string),
                HandlerType = typeof(int),
                HandlerMethod = null
            }
        });

        var content = await _sut.ReadResourceAsync("communication://handlers", Mock.Of<IMcpContext>(), CancellationToken.None);

        using var doc = JsonDocument.Parse(content.Text!);
        var handlers = doc.RootElement.GetProperty("handlers");
        handlers.GetArrayLength().Should().Be(1);
        handlers[0].GetProperty("payloadType").GetString().Should().Be("System.String");
        handlers[0].GetProperty("handlerType").GetString().Should().Be("System.Int32");
    }

    [Fact]
    public async Task ReadResourceAsync_UnknownUri_ReturnsClearError()
    {
        var content = await _sut.ReadResourceAsync("communication://unknown", Mock.Of<IMcpContext>(), CancellationToken.None);

        content.Uri.Should().Be("communication://unknown");
        content.Text.Should().Contain("Unknown resource");
    }

    private static async IAsyncEnumerable<IClientConnectionInfo> AsyncEnumerable(params ClientConnectionInfo[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }
}
