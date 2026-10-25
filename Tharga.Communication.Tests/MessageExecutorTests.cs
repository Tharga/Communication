using System.Text.Json;
using AwesomeAssertions;
using Moq;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;
using Xunit;

namespace Tharga.Communication.Tests;

public class MessageExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WithUnknownHandlerType_ThrowsInvalidOperationException()
    {
        var handlerService = new Mock<IHandlerTypeService>();
        handlerService
            .Setup(x => x.TryGetHandler(It.IsAny<Type>(), out It.Ref<HandlerTypeInfo>.IsAny))
            .Returns(false);

        var serviceProvider = new Mock<IServiceProvider>();
        var executor = new MessageExecutor(serviceProvider.Object, handlerService.Object);

        var wrapper = new RequestWrapper
        {
            Type = typeof(UnregisteredMessage).AssemblyQualifiedName!,
            Payload = "{}"
        };

        var act = () => executor.ExecuteAsync("conn-1", wrapper);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot find handler*");
    }

    [Fact]
    public async Task ExecuteAsync_SendHandlerWithConnectionId_ReturnsResponse()
    {
        var executor = CreateExecutor<EchoRequest>(new EchoHandler());
        var wrapper = new RequestWrapper
        {
            Type = typeof(EchoRequest).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new EchoRequest("hello"))
        };

        var result = await executor.ExecuteAsync("conn-1", wrapper);

        result.Type.Should().Be(typeof(EchoResponse).AssemblyQualifiedName);
        JsonSerializer.Deserialize<EchoResponse>(result.Payload)!.Reply.Should().Be("Echo: hello");
    }

    [Fact]
    public async Task ExecuteAsync_PostHandlerWithConnectionId_SetsConnectionId()
    {
        var handler = new NoteHandler();
        var executor = CreateExecutor<Note>(handler);
        var wrapper = new RequestWrapper
        {
            Type = typeof(Note).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new Note("hello"))
        };

        await executor.ExecuteAsync("conn-1", wrapper);

        handler.SeenConnectionId.Should().Be("conn-1");
        handler.SeenText.Should().Be("hello");
    }

    private static MessageExecutor CreateExecutor<TPayload>(object handler)
    {
        var handlerType = handler.GetType();
        var handlerTypeService = new HandlerTypeService(new Dictionary<Type, HandlerTypeInfo>
        {
            [typeof(TPayload)] = new()
            {
                PayloadType = typeof(TPayload),
                HandlerType = handlerType,
                HandlerMethod = handlerType.GetMethod("Handle")
            }
        });
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(handlerType)).Returns(handler);
        return new MessageExecutor(serviceProvider.Object, handlerTypeService);
    }

    private record UnregisteredMessage;

    public record EchoRequest(string Message);
    public record EchoResponse(string Reply);
    public record Note(string Text);

    public class EchoHandler : SendMessageHandlerBase<EchoRequest, EchoResponse>
    {
        public override Task<EchoResponse> Handle(EchoRequest message) =>
            Task.FromResult(new EchoResponse($"Echo: {message.Message}"));
    }

    public class NoteHandler : PostMessageHandlerBase<Note>
    {
        public string SeenConnectionId { get; private set; }
        public string SeenText { get; private set; }

        public override Task Handle(Note message)
        {
            SeenConnectionId = ConnectionId;
            SeenText = message.Text;
            return Task.CompletedTask;
        }
    }
}
