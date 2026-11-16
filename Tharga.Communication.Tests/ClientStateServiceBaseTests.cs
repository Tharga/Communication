using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Tharga.Communication.Server;
using Tharga.Communication.Server.Communication;
using Xunit;
using ServerOptions = CommunicationOptions;

namespace Tharga.Communication.Tests;

public class ClientStateServiceBaseTests
{
    [Fact]
    public async Task GetConnectionInfosAsync_ReturnsBaseTypeForGenericService()
    {
        var serverOptions = new ServerOptions();
        serverOptions.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        var sp = new ServiceProviderStub(new MemoryClientRepository<ClientConnectionInfo>());
        var sut = new TestStateService(sp, Options.Create(serverOptions));

        await sut.ConnectAsync(new ClientConnection
        {
            Instance = Guid.NewGuid(),
            ConnectionId = "conn-1",
            Machine = "m1",
            Type = "t1",
            Version = "1.0"
        });
        await sut.ConnectAsync(new ClientConnection
        {
            Instance = Guid.NewGuid(),
            ConnectionId = "conn-2",
            Machine = "m2",
            Type = "t2",
            Version = "1.0"
        });

        var infos = new List<IClientConnectionInfo>();
        await foreach (var info in sut.GetConnectionInfosAsync())
            infos.Add(info);

        infos.Should().HaveCount(2);
        infos.Should().Contain(i => i.ConnectionId == "conn-1");
        infos.Should().Contain(i => i.ConnectionId == "conn-2");
    }

    [Fact]
    public async Task ConnectAsync_PassesKeyIdAndKeyNameToBuild()
    {
        var serverOptions = new ServerOptions();
        serverOptions.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        var sp = new ServiceProviderStub(new MemoryClientRepository<ClientConnectionInfo>());
        var sut = new TestStateService(sp, Options.Create(serverOptions));

        await sut.ConnectAsync(new ClientConnection
        {
            Instance = Guid.NewGuid(),
            ConnectionId = "conn-1",
            Machine = "m1",
            Type = "t1",
            Version = "1.0",
            KeyId = "key-1",
            KeyName = "Agent install"
        });

        sut.Built.Should().ContainSingle();
        sut.Built[0].KeyId.Should().Be("key-1");
        sut.Built[0].KeyName.Should().Be("Agent install");
    }

    [Fact]
    public async Task ConnectAsync_WithoutKey_PassesNullKeyIdAndKeyNameToBuild()
    {
        var serverOptions = new ServerOptions();
        serverOptions.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        var sp = new ServiceProviderStub(new MemoryClientRepository<ClientConnectionInfo>());
        var sut = new TestStateService(sp, Options.Create(serverOptions));

        await sut.ConnectAsync(new ClientConnection
        {
            Instance = Guid.NewGuid(),
            ConnectionId = "conn-1",
            Machine = "m1",
            Type = "t1",
            Version = "1.0"
        });

        sut.Built.Should().ContainSingle();
        sut.Built[0].KeyId.Should().BeNull();
        sut.Built[0].KeyName.Should().BeNull();
    }

    [Fact]
    public async Task ConnectAsync_PassesMetadataToBuild()
    {
        var serverOptions = new ServerOptions();
        serverOptions.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        var sp = new ServiceProviderStub(new MemoryClientRepository<ClientConnectionInfo>());
        var sut = new TestStateService(sp, Options.Create(serverOptions));

        await sut.ConnectAsync(new ClientConnection
        {
            Instance = Guid.NewGuid(),
            ConnectionId = "conn-1",
            Machine = "m1",
            Type = "t1",
            Version = "1.0",
            Metadata = new Dictionary<string, string> { ["tenant"] = "acme" }
        });

        sut.Built[0].Metadata.Should().ContainKey("tenant").WhoseValue.Should().Be("acme");
    }

    [Fact]
    public async Task ConnectAsync_WithoutMetadata_PassesNullToBuild()
    {
        var serverOptions = new ServerOptions();
        serverOptions.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        var sp = new ServiceProviderStub(new MemoryClientRepository<ClientConnectionInfo>());
        var sut = new TestStateService(sp, Options.Create(serverOptions));

        await sut.ConnectAsync(new ClientConnection
        {
            Instance = Guid.NewGuid(),
            ConnectionId = "conn-1",
            Machine = "m1",
            Type = "t1",
            Version = "1.0"
        });

        sut.Built[0].Metadata.Should().BeNull();
    }

    [Fact]
    public async Task DisconnectedAsync_ConnectionThatWasNeverRegistered_DoesNotRaiseDisconnectedEvent()
    {
        var serverOptions = new ServerOptions();
        serverOptions.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        var sut = new TestStateService(new ServiceProviderStub(new MemoryClientRepository<ClientConnectionInfo>()), Options.Create(serverOptions));
        var raised = new List<DisconnectedEventArgs>();
        sut.DisconnectedEvent += (_, e) => raised.Add(e);

        await sut.DisconnectedAsync("never-registered");

        raised.Should().BeEmpty();
    }

    [Fact]
    public async Task DisconnectedAsync_RegisteredConnection_RaisesDisconnectedEventWithTheClient()
    {
        var serverOptions = new ServerOptions();
        serverOptions.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        var sut = new TestStateService(new ServiceProviderStub(new MemoryClientRepository<ClientConnectionInfo>()), Options.Create(serverOptions));
        var raised = new List<DisconnectedEventArgs>();
        sut.DisconnectedEvent += (_, e) => raised.Add(e);
        await sut.ConnectAsync(new ClientConnection { Instance = Guid.NewGuid(), ConnectionId = "conn-1", Machine = "m1", Type = "t1", Version = "1.0" });

        await sut.DisconnectedAsync("conn-1");

        raised.Should().ContainSingle();
        raised[0].Item.ConnectionId.Should().Be("conn-1");
    }

    [Fact]
    public void IClientConnectionInfo_ImplementationWithoutMetadata_ReportsNull()
    {
        IClientConnectionInfo info = new LegacyConnectionInfo();

        info.Metadata.Should().BeNull();
    }

    private sealed class LegacyConnectionInfo : IClientConnectionInfo
    {
        public Guid Instance => Guid.Empty;
        public string ConnectionId => null;
        public string Machine => null;
        public string Type => null;
        public string Version => null;
        public string KeyId => null;
        public string KeyName => null;
        public bool IsConnected { get; init; }
        public DateTime ConnectTime => default;
        public DateTime? DisconnectTime { get; init; }
    }

    private class TestStateService : ClientStateServiceBase<ClientConnectionInfo>
    {
        public TestStateService(IServiceProvider sp, IOptions<ServerOptions> options) : base(sp, options) { }

        public List<IClientConnectionInfo> Built { get; } = [];

        protected override ClientConnectionInfo Build(IClientConnectionInfo info)
        {
            Built.Add(info);
            return Map(info);
        }

        private static ClientConnectionInfo Map(IClientConnectionInfo info) => new()
        {
            Instance = info.Instance,
            ConnectionId = info.ConnectionId,
            Machine = info.Machine,
            Type = info.Type,
            Version = info.Version,
            IsConnected = info.IsConnected,
            ConnectTime = info.ConnectTime
        };

        protected override ClientConnectionInfo BuildDisconnect(ClientConnectionInfo info, DateTime disconnectTime) =>
            info with { IsConnected = false, DisconnectTime = disconnectTime };
    }

    private class ServiceProviderStub : IServiceProvider
    {
        private readonly object _repo;
        public ServiceProviderStub(object repo) { _repo = repo; }
        public object GetService(Type serviceType) => serviceType.IsInstanceOfType(_repo) ? _repo : null;
    }
}
