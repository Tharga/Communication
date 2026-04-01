using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;
using System.Text.Json;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;
using Tharga.Communication.Server;
using Tharga.Communication.Server.Communication;
using Xunit;

namespace Tharga.Communication.Tests;

public class ServerCommunicationSubscriptionTests
{
    private readonly AutoMocker _mocker = new();
    private readonly Mock<IClientProxy> _allClients = new();
    private readonly ServerCommunication _sut;

    public ServerCommunicationSubscriptionTests()
    {
        _mocker.Use(new SubscriptionManager());

        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(x => x.All).Returns(_allClients.Object);

        var hubContext = new Mock<IHubContext<SignalRHub>>();
        hubContext.Setup(x => x.Clients).Returns(hubClients.Object);
        _mocker.Use(hubContext.Object);

        _sut = _mocker.CreateInstance<ServerCommunication>();
    }

    [Fact]
    public async Task SubscribeAsync_FirstSubscriber_NotifiesClients()
    {
        await _sut.SubscribeAsync<FakeMessage>("1");

        _allClients.Verify(x => x.SendCoreAsync(
            Constants.PostMessage,
            It.Is<object[]>(args => VerifySubscriptionState(args, typeof(FakeMessage).FullName!, "1", true)),
            default), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_SecondSubscriber_DoesNotNotify()
    {
        await _sut.SubscribeAsync<FakeMessage>("1");
        _allClients.Invocations.Clear();

        await _sut.SubscribeAsync<FakeMessage>("1");

        _allClients.Verify(x => x.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            default), Times.Never);
    }

    [Fact]
    public async Task DisposeHandle_LastSubscriber_NotifiesClients()
    {
        var handle = await _sut.SubscribeAsync<FakeMessage>("1");
        _allClients.Invocations.Clear();

        await handle.DisposeAsync();

        _allClients.Verify(x => x.SendCoreAsync(
            Constants.PostMessage,
            It.Is<object[]>(args => VerifySubscriptionState(args, typeof(FakeMessage).FullName!, "1", false)),
            default), Times.Once);
    }

    [Fact]
    public async Task DisposeHandle_StillHasSubscribers_DoesNotNotify()
    {
        var handle1 = await _sut.SubscribeAsync<FakeMessage>("1");
        await _sut.SubscribeAsync<FakeMessage>("1");
        _allClients.Invocations.Clear();

        await handle1.DisposeAsync();

        _allClients.Verify(x => x.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            default), Times.Never);
    }

    [Fact]
    public async Task DisposeHandle_DoubleDispose_OnlyNotifiesOnce()
    {
        var handle = await _sut.SubscribeAsync<FakeMessage>("1");
        _allClients.Invocations.Clear();

        await handle.DisposeAsync();
        await handle.DisposeAsync();

        _allClients.Verify(x => x.SendCoreAsync(
            Constants.PostMessage,
            It.IsAny<object[]>(),
            default), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_WithoutKey_NotifiesWithNullKey()
    {
        await _sut.SubscribeAsync<FakeMessage>();

        _allClients.Verify(x => x.SendCoreAsync(
            Constants.PostMessage,
            It.Is<object[]>(args => VerifySubscriptionState(args, typeof(FakeMessage).FullName!, null, true)),
            default), Times.Once);
    }

    [Fact]
    public async Task GetSubscriptions_ReturnsActiveSubscriptions()
    {
        await _sut.SubscribeAsync<FakeMessage>("1");
        await _sut.SubscribeAsync<FakeMessage>("2");

        var subs = _sut.GetSubscriptions();

        subs.Should().HaveCount(2);
        var expectedKey1 = $"{typeof(FakeMessage).FullName}:1";
        var expectedKey2 = $"{typeof(FakeMessage).FullName}:2";
        subs[expectedKey1].Should().Be(1);
        subs[expectedKey2].Should().Be(1);
    }

    private static bool VerifySubscriptionState(object[] args, string expectedTopic, string expectedKey, bool expectedHasSubscribers)
    {
        if (args.Length != 1) return false;
        var wrapper = args[0] as RequestWrapper;
        if (wrapper == null) return false;
        var message = JsonSerializer.Deserialize<SubscriptionStateChanged>(wrapper.Payload);
        return message != null
            && message.Topic == expectedTopic
            && message.Key == expectedKey
            && message.HasSubscribers == expectedHasSubscribers;
    }

    public record FakeMessage;
}
