using FluentAssertions;
using Microsoft.Extensions.Options;
using Tharga.Communication.Server;
using Tharga.Communication.Server.Communication;
using Xunit;
using ServerOptions = global::CommunicationOptions;

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

    private class TestStateService : ClientStateServiceBase<ClientConnectionInfo>
    {
        public TestStateService(IServiceProvider sp, IOptions<ServerOptions> options) : base(sp, options) { }

        protected override ClientConnectionInfo Build(IClientConnectionInfo info) => new()
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
