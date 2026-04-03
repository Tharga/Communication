using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Tharga.Communication.Client;
using Tharga.Communication.Client.Communication;
using Tharga.Communication.Contract;
using Xunit;
using ClientOptions = Tharga.Communication.Client.CommunicationOptions;

namespace Tharga.Communication.Tests;

public class ClientSendMessageTests
{
    private readonly Mock<ISignalRHostedService> _signalR = new();
    private readonly ClientResponseMediator _mediator = new();
    private readonly ClientCommunication _sut;

    public ClientSendMessageTests()
    {
        _signalR.Setup(x => x.State).Returns(HubConnectionState.Connected);
        _signalR.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<object>())).Returns(Task.CompletedTask);
        _sut = new ClientCommunication(
            _signalR.Object,
            _mediator,
            new SubscriptionStateTracker(),
            Options.Create(new ClientOptions { SendMessageTimeout = TimeSpan.FromSeconds(5) }),
            NullLogger<ClientCommunication>.Instance);
    }

    public record PingRequest(string Message);
    public record PingResponse(string Reply);

    [Fact]
    public async Task SendMessage_WithResponse_ReturnsDeserialized()
    {
        var sendTask = _sut.SendMessage<PingRequest, PingResponse>(new PingRequest("hello"));

        // Simulate server responding
        var responseWrapper = new RequestWrapper
        {
            Type = typeof(PingResponse).AssemblyQualifiedName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(new PingResponse("pong"))
        };
        _mediator.Deliver(responseWrapper);

        var result = await sendTask;

        result.Should().NotBeNull();
        result.Reply.Should().Be("pong");
    }

    [Fact]
    public async Task SendMessage_Timeout_ThrowsTimeoutException()
    {
        var act = () => _sut.SendMessage<PingRequest, PingResponse>(
            new PingRequest("hello"),
            TimeSpan.FromMilliseconds(50));

        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("*No response received*");
    }

    [Fact]
    public async Task SendMessage_ConcurrentRequest_ThrowsInvalidOperation()
    {
        // Start a request that won't complete
        var firstTask = _sut.SendMessage<PingRequest, PingResponse>(new PingRequest("first"), TimeSpan.FromSeconds(5));

        // Try to send a second concurrent request
        var act = () => _sut.SendMessage<PingRequest, PingResponse>(new PingRequest("second"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already pending*");

        // Clean up first request
        _mediator.Deliver(new RequestWrapper
        {
            Type = typeof(PingResponse).AssemblyQualifiedName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(new PingResponse("done"))
        });
        await firstTask;
    }

    [Fact]
    public async Task SendMessage_SendsCorrectWrapperToServer()
    {
        var sendTask = _sut.SendMessage<PingRequest, PingResponse>(new PingRequest("test"));

        _signalR.Verify(x => x.SendAsync(Constants.SendMessage, It.Is<RequestWrapper>(w =>
            w.Type == typeof(PingRequest).AssemblyQualifiedName)), Times.Once);

        // Complete the request
        _mediator.Deliver(new RequestWrapper
        {
            Type = typeof(PingResponse).AssemblyQualifiedName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(new PingResponse("ok"))
        });
        await sendTask;
    }
}
