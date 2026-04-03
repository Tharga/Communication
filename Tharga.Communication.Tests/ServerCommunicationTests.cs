using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Moq.AutoMock;
using Tharga.Communication.Contract;
using Tharga.Communication.Server;
using Tharga.Communication.Server.Communication;
using Xunit;

namespace Tharga.Communication.Tests;

public class ServerCommunicationTests
{
    private readonly AutoMocker _mocker = new();
    private readonly Mock<ISingleClientProxy> _clientProxy = new();
    private readonly Mock<IClientProxy> _allClientsProxy = new();
    private readonly ServerCommunication _sut;

    public record TestNotification(string Text);
    public record TestRequest(string Value);
    public record TestResponse(string Result);

    public ServerCommunicationTests()
    {
        _mocker.Use(new SubscriptionManager());

        _clientProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _allClientsProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(x => x.Client("conn-1")).Returns(_clientProxy.Object);
        hubClients.Setup(x => x.All).Returns(_allClientsProxy.Object);

        var hubContext = new Mock<IHubContext<SignalRHub>>();
        hubContext.Setup(x => x.Clients).Returns(hubClients.Object);
        _mocker.Use(hubContext.Object);

        _sut = _mocker.CreateInstance<ServerCommunication>();
    }

    [Fact]
    public async Task PostAsync_SendsToSpecificClient()
    {
        await _sut.PostAsync("conn-1", new TestNotification("hello"));

        _clientProxy.Verify(x => x.SendCoreAsync(
            Constants.PostMessage,
            It.Is<object[]>(args => args.Length == 1 && args[0] is RequestWrapper),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostAsync_SerializesPayloadCorrectly()
    {
        RequestWrapper captured = null;
        _clientProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((_, args, _) => captured = args[0] as RequestWrapper)
            .Returns(Task.CompletedTask);

        await _sut.PostAsync("conn-1", new TestNotification("hello"));

        captured.Should().NotBeNull();
        captured.Type.Should().Contain(nameof(TestNotification));
        var deserialized = JsonSerializer.Deserialize<TestNotification>(captured.Payload);
        deserialized.Text.Should().Be("hello");
    }

    [Fact]
    public async Task PostToAllAsync_BroadcastsToAllClients()
    {
        await _sut.PostToAllAsync(new TestNotification("broadcast"));

        _allClientsProxy.Verify(x => x.SendCoreAsync(
            Constants.PostMessage,
            It.Is<object[]>(args => args.Length == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithResponse_ReturnsDeserializedValue()
    {
        // Capture the send and simulate a response
        _clientProxy
            .Setup(x => x.SendCoreAsync(Constants.SendMessage, It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((_, _, _) =>
            {
                // Simulate the client responding
                var responseWrapper = new RequestWrapper
                {
                    Type = typeof(TestResponse).AssemblyQualifiedName!,
                    Payload = JsonSerializer.Serialize(new TestResponse("pong"))
                };
                _sut.OnResponseEvent(this, new ResponseEventArgs("conn-1", responseWrapper));
            })
            .Returns(Task.CompletedTask);

        var result = await _sut.SendMessageAsync<TestRequest, TestResponse>("conn-1", new TestRequest("ping"), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().Be("pong");
    }

    [Fact]
    public async Task SendMessageAsync_Timeout_ReturnsFailResponse()
    {
        var result = await _sut.SendMessageAsync<TestRequest, TestResponse>(
            "conn-1", new TestRequest("ping"), TimeSpan.FromMilliseconds(50));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be("TIMEOUT");
    }

    [Fact]
    public async Task SendMessageAsync_DuplicatePending_ThrowsInvalidOperation()
    {
        // Start a request that won't complete
        var firstTask = _sut.SendMessageAsync<TestRequest, TestResponse>(
            "conn-1", new TestRequest("first"), TimeSpan.FromSeconds(5));

        var act = () => _sut.SendMessageAsync<TestRequest, TestResponse>(
            "conn-1", new TestRequest("second"), null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already pending*");

        // Clean up - let first request timeout
        await firstTask;
    }

    [Fact]
    public async Task GetPendingAsync_TracksPendingRequests()
    {
        _sut.GetPendingAsync().Should().BeEmpty();

        // Start a request that won't complete
        var task = _sut.SendMessageAsync<TestRequest, TestResponse>(
            "conn-1", new TestRequest("ping"), TimeSpan.FromSeconds(5));

        _sut.GetPendingAsync().Should().ContainKey("conn-1");

        // Simulate response to clean up
        _sut.OnResponseEvent(this, new ResponseEventArgs("conn-1", new RequestWrapper
        {
            Type = typeof(TestResponse).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new TestResponse("done"))
        }));

        await task;

        _sut.GetPendingAsync().Should().BeEmpty();
    }

    [Fact]
    public async Task PendingRequestEvent_RaisedOnAddAndRemove()
    {
        var events = new List<PendingRequestEventArgs>();
        _sut.PendingRequestEvent += (_, e) => events.Add(e);

        var task = _sut.SendMessageAsync<TestRequest, TestResponse>(
            "conn-1", new TestRequest("ping"), TimeSpan.FromSeconds(5));

        events.Should().ContainSingle(e => e.Added && e.ConnectionId == "conn-1");

        // Simulate response
        _sut.OnResponseEvent(this, new ResponseEventArgs("conn-1", new RequestWrapper
        {
            Type = typeof(TestResponse).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new TestResponse("done"))
        }));

        await task;

        events.Should().HaveCount(2);
        events[1].Added.Should().BeFalse();
    }
}
