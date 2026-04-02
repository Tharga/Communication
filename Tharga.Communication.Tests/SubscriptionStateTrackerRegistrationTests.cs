using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tharga.Communication.Client;
using Xunit;

namespace Tharga.Communication.Tests;

public class SubscriptionStateTrackerRegistrationTests
{
    [Fact]
    public void ClientRegistration_RegistersSubscriptionStateTracker()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true
        });

        builder.AddThargaCommunicationClient(o =>
        {
            o.ServerAddress = "https://localhost:5001";
        });

        var sp = builder.Build().Services;
        var tracker = sp.GetService<SubscriptionStateTracker>();

        tracker.Should().NotBeNull();
    }

    [Fact]
    public void ClientRegistration_CalledTwice_DoesNotThrow()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true
        });

        var act = () =>
        {
            builder.AddThargaCommunicationClient(o => o.ServerAddress = "https://localhost:5001");
            builder.AddThargaCommunicationClient(o => o.ServerAddress = "https://localhost:5001");
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void SubscriptionStateTracker_ResolvableFromServiceCollection_WithoutClientRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<SubscriptionStateTracker>();

        var sp = services.BuildServiceProvider();
        var tracker = sp.GetService<SubscriptionStateTracker>();

        tracker.Should().NotBeNull();
    }
}
