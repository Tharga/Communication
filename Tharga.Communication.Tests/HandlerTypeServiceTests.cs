using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Communication.MessageHandler;
using Xunit;

namespace Tharga.Communication.Tests;

public class HandlerTypeServiceTests
{
    public record TestMessage(string Value);
    public record TestRequest(string Value);
    public record TestResponse(string Result);

    public class TestPostHandler : PostMessageHandlerBase<TestMessage>
    {
        public override Task Handle(TestMessage message) => Task.CompletedTask;
    }

    public class TestSendHandler : SendMessageHandlerBase<TestRequest, TestResponse>
    {
        public override Task<TestResponse> Handle(TestRequest message) =>
            Task.FromResult(new TestResponse("ok"));
    }

    [Fact]
    public void GetHandlerTypes_WithAdditionalAssembly_DiscoversHandlers()
    {
        var services = new ServiceCollection();
        var testAssembly = Assembly.GetExecutingAssembly();

        var handlers = HandlerTypeService.GetHandlerTypes(services, [testAssembly]);

        handlers.Should().ContainKey(typeof(TestMessage));
        handlers.Should().ContainKey(typeof(TestRequest));
    }

    [Fact]
    public void GetHandlerTypes_WithoutAdditionalAssembly_MayNotDiscoverTestHandlers()
    {
        var services = new ServiceCollection();

        var handlers = HandlerTypeService.GetHandlerTypes(services);

        // Test handlers are in a different assembly prefix than the entry assembly,
        // so they may not be discovered by default assembly scanning.
        // This test documents the default behavior.
        handlers.Should().NotBeNull();
    }

    [Fact]
    public void TryGetHandler_WithDiscoveredHandler_ReturnsTrue()
    {
        var services = new ServiceCollection();
        var testAssembly = Assembly.GetExecutingAssembly();

        var handlers = HandlerTypeService.GetHandlerTypes(services, [testAssembly]);
        var service = new HandlerTypeService(handlers);

        service.TryGetHandler(typeof(TestMessage), out var info).Should().BeTrue();
        info.HandlerType.Should().Be(typeof(TestPostHandler));
    }

    [Fact]
    public void TryGetHandler_WithUnregisteredType_ReturnsFalse()
    {
        var services = new ServiceCollection();
        var handlers = HandlerTypeService.GetHandlerTypes(services);
        var service = new HandlerTypeService(handlers);

        service.TryGetHandler(typeof(string), out _).Should().BeFalse();
    }
}
