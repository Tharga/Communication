using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tharga.Communication.Client;
using Xunit;
using ClientOptions = Tharga.Communication.Client.CommunicationOptions;

namespace Tharga.Communication.Tests;

public class CommunicationClientRegistrationTests
{
    [Fact]
    public void AddThargaCommunicationClient_WithNoConfigSection_DoesNotThrow()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true
        });

        var act = () => builder.AddThargaCommunicationClient(o =>
        {
            o.ServerAddress = "https://localhost:5001";
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void AddThargaCommunicationClient_WithNoConfigSection_AppliesOptionsCallback()
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
        var options = sp.GetRequiredService<IOptions<ClientOptions>>().Value;

        options.ServerAddress.Should().Be("https://localhost:5001");
    }

    [Fact]
    public void AddThargaCommunicationClient_WithNoConfigSection_AppliesDefaults()
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
        var options = sp.GetRequiredService<IOptions<ClientOptions>>().Value;

        options.Pattern.Should().Be("hub");
        options.ReconnectDelays.Should().Equal(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddThargaCommunicationClient_WithPartialConfig_MergesWithDefaults()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Tharga:Communication:ServerAddress"] = "https://partial:5001"
            })
            .Build();

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true
        });
        builder.Configuration.AddConfiguration(config);

        builder.AddThargaCommunicationClient();

        var sp = builder.Build().Services;
        var options = sp.GetRequiredService<IOptions<ClientOptions>>().Value;

        options.ServerAddress.Should().Be("https://partial:5001");
        options.Pattern.Should().Be("hub");
    }

    [Fact]
    public void AddThargaCommunicationClient_WithFullConfig_UsesConfigValues()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Tharga:Communication:ServerAddress"] = "https://full:5001",
                ["Tharga:Communication:Pattern"] = "custom-hub"
            })
            .Build();

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true
        });
        builder.Configuration.AddConfiguration(config);

        builder.AddThargaCommunicationClient();

        var sp = builder.Build().Services;
        var options = sp.GetRequiredService<IOptions<ClientOptions>>().Value;

        options.ServerAddress.Should().Be("https://full:5001");
        options.Pattern.Should().Be("custom-hub");
    }
}
