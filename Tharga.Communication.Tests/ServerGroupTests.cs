using System.Collections.Concurrent;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tharga.Communication.Client;
using Tharga.Communication.MessageHandler;
using Tharga.Communication.Server;
using Tharga.Communication.Server.Communication;
using Xunit;

namespace Tharga.Communication.Tests;

public class ServerGroupTests : IAsyncLifetime
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(500);

    private readonly List<IHost> _clients = [];
    private WebApplication _server;

    public record GroupNotice(string Text);

    public class Inbox
    {
        public ConcurrentQueue<string> Received { get; } = new();
    }

    public class GroupNoticeHandler : PostMessageHandlerBase<GroupNotice>
    {
        private readonly Inbox _inbox;

        public GroupNoticeHandler(Inbox inbox)
        {
            _inbox = inbox;
        }

        public override Task Handle(GroupNotice message)
        {
            _inbox.Received.Enqueue(message.Text);
            return Task.CompletedTask;
        }
    }

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterClientStateService<IntegrationTests.TestClientStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });

        _server = builder.Build();
        _server.Urls.Add("http://127.0.0.1:0");
        _server.UseThargaCommunicationServer();
        await _server.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            await client.StopAsync();
            client.Dispose();
        }

        await _server.StopAsync();
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task PostToGroupAsync_ReachesOnlyMembers()
    {
        var (memberInbox, memberId) = await StartClientAsync("member");
        var (outsiderInbox, _) = await StartClientAsync("outsider");
        var server = _server.Services.GetRequiredService<IServerCommunication>();
        await server.AddToGroupAsync(memberId, "team-a");

        await server.PostToGroupAsync("team-a", new GroupNotice("hello"));

        await WaitForAsync(memberInbox, 1);
        await Task.Delay(QuietPeriod, TestContext.Current.CancellationToken);
        memberInbox.Received.Should().Equal("hello");
        outsiderInbox.Received.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveFromGroupAsync_StopsDelivery()
    {
        var (inbox, connectionId) = await StartClientAsync("member");
        var server = _server.Services.GetRequiredService<IServerCommunication>();
        await server.AddToGroupAsync(connectionId, "team-a");
        await server.RemoveFromGroupAsync(connectionId, "team-a");

        await server.PostToGroupAsync("team-a", new GroupNotice("hello"));

        await Task.Delay(QuietPeriod, TestContext.Current.CancellationToken);
        inbox.Received.Should().BeEmpty();
    }

    [Fact]
    public async Task PostToGroupsAsync_ReachesMembersOfEveryListedGroup()
    {
        var (inboxA, idA) = await StartClientAsync("a");
        var (inboxB, idB) = await StartClientAsync("b");
        var (inboxC, _) = await StartClientAsync("c");
        var server = _server.Services.GetRequiredService<IServerCommunication>();
        await server.AddToGroupAsync(idA, "team-a");
        await server.AddToGroupAsync(idB, "team-b");

        await server.PostToGroupsAsync(["team-a", "team-b"], new GroupNotice("hello"));

        await WaitForAsync(inboxA, 1);
        await WaitForAsync(inboxB, 1);
        await Task.Delay(QuietPeriod, TestContext.Current.CancellationToken);
        inboxA.Received.Should().Equal("hello");
        inboxB.Received.Should().Equal("hello");
        inboxC.Received.Should().BeEmpty();
    }

    [Fact]
    public async Task ServerCommunicationImplementationWithoutGroups_ThrowsNotSupported()
    {
        IServerCommunication legacy = new LegacyServerCommunication();

        var act = () => legacy.PostToGroupAsync("g", new GroupNotice("x"));

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    private static async Task WaitForAsync(Inbox inbox, int count)
    {
        using var limit = new CancellationTokenSource(WaitLimit);
        while (inbox.Received.Count < count)
        {
            await Task.Delay(20, limit.Token);
        }
    }

    private async Task<(Inbox Inbox, string ConnectionId)> StartClientAsync(string machine)
    {
        var inbox = new Inbox();
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.AddThargaCommunicationClient(o =>
        {
            o.ServerAddress = _server.Urls.First();
            o.ClientMachine = machine;
            o.AdditionalAssemblies = [GetType().Assembly];
        });
        builder.Services.AddSingleton(inbox);

        var client = builder.Build();
        _clients.Add(client);
        await client.StartAsync(TestContext.Current.CancellationToken);

        var stateService = _server.Services.GetRequiredService<IntegrationTests.TestClientStateService>();
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        limit.CancelAfter(WaitLimit);
        while (true)
        {
            await foreach (var connection in stateService.GetAsync())
            {
                if (connection.Machine == machine) return (inbox, connection.ConnectionId);
            }

            await Task.Delay(20, limit.Token);
        }
    }

    private sealed class LegacyServerCommunication : IServerCommunication
    {
        public event EventHandler<PendingRequestEventArgs> PendingRequestEvent { add { } remove { } }
        public Task PostAsync<T>(string connectionId, T message) => Task.CompletedTask;
        public Task PostToAllAsync<T>(T message) => Task.CompletedTask;
        public Task<Response<TResponse>> SendMessageAsync<TRequest, TResponse>(string connectionId, TRequest message, TimeSpan? timeout = null) => throw new NotImplementedException();
        public Dictionary<string, DateTime> GetPendingAsync() => [];
        public Task<IAsyncDisposable> SubscribeAsync<T>(string key = null) => throw new NotImplementedException();
        public IReadOnlyDictionary<string, int> GetSubscriptions() => new Dictionary<string, int>();
    }
}
