using FluentAssertions;
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

    private record UnregisteredMessage;
}
