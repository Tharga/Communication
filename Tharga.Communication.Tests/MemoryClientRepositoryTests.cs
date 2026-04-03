using FluentAssertions;
using Tharga.Communication.Server;
using Xunit;

namespace Tharga.Communication.Tests;

public class MemoryClientRepositoryTests
{
    private readonly MemoryClientRepository<ClientConnectionInfo> _sut = new();

    private static ClientConnectionInfo CreateClient(Guid? instance = null, string connectionId = "conn-1") => new()
    {
        Instance = instance ?? Guid.NewGuid(),
        ConnectionId = connectionId,
        Machine = "test-machine",
        Type = "test-app",
        Version = "1.0.0",
        IsConnected = true,
        ConnectTime = DateTime.UtcNow
    };

    [Fact]
    public async Task UpsertAsync_ThenGetAsync_ReturnsAll()
    {
        var client = CreateClient();
        await _sut.UpsertAsync(client);

        var results = new List<ClientConnectionInfo>();
        await foreach (var item in _sut.GetAsync())
            results.Add(item);

        results.Should().ContainSingle().Which.Should().BeEquivalentTo(client);
    }

    [Fact]
    public async Task GetAsync_ByConnectionId_ReturnsCorrectClient()
    {
        var client = CreateClient(connectionId: "conn-42");
        await _sut.UpsertAsync(client);

        var result = await _sut.GetAsync("conn-42");

        result.ConnectionId.Should().Be("conn-42");
    }

    [Fact]
    public async Task GetAsync_ByConnectionId_UnknownId_ReturnsDefault()
    {
        var result = await _sut.GetAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_SameInstance_UpdatesExisting()
    {
        var instance = Guid.NewGuid();
        var original = CreateClient(instance, "conn-1");
        var updated = CreateClient(instance, "conn-2");

        await _sut.UpsertAsync(original);
        await _sut.UpsertAsync(updated);

        var results = new List<ClientConnectionInfo>();
        await foreach (var item in _sut.GetAsync())
            results.Add(item);

        results.Should().ContainSingle().Which.ConnectionId.Should().Be("conn-2");
    }

    [Fact]
    public async Task DeleteAsync_RemovesClient()
    {
        var client = CreateClient(connectionId: "conn-del");
        await _sut.UpsertAsync(client);

        var deleted = await _sut.DeleteAsync("conn-del");

        deleted.ConnectionId.Should().Be("conn-del");

        var results = new List<ClientConnectionInfo>();
        await foreach (var item in _sut.GetAsync())
            results.Add(item);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_UnknownConnectionId_ReturnsDefault()
    {
        var result = await _sut.DeleteAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task MultipleClients_TrackedIndependently()
    {
        var client1 = CreateClient(connectionId: "conn-a");
        var client2 = CreateClient(connectionId: "conn-b");

        await _sut.UpsertAsync(client1);
        await _sut.UpsertAsync(client2);

        var results = new List<ClientConnectionInfo>();
        await foreach (var item in _sut.GetAsync())
            results.Add(item);

        results.Should().HaveCount(2);
    }
}
