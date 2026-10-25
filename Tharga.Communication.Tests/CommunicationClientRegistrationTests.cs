using AwesomeAssertions;
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

    [Fact]
    public void AddThargaCommunicationClient_WithApiKeyInConfig_UsesConfiguredKey()
    {
        var options = RegisterWithConfig(new Dictionary<string, string>
        {
            ["Tharga:Communication:ServerAddress"] = "https://localhost:5001",
            ["Tharga:Communication:ApiKey"] = "configured-key"
        });

        options.ApiKey.Should().Be("configured-key");
    }

    [Fact]
    public void AddThargaCommunicationClient_WithEveryOptionInConfig_BindsEveryOption()
    {
        var options = RegisterWithConfig(new Dictionary<string, string>
        {
            ["Tharga:Communication:ServerAddress"] = "https://localhost:5001",
            ["Tharga:Communication:Pattern"] = "custom-hub",
            ["Tharga:Communication:ApiKey"] = "configured-key",
            ["Tharga:Communication:ClientType"] = "configured-type",
            ["Tharga:Communication:ClientMachine"] = "configured-machine",
            ["Tharga:Communication:SendMessageTimeout"] = "00:00:15",
            ["Tharga:Communication:ReconnectDelays:0"] = "00:00:01",
            ["Tharga:Communication:ReconnectDelays:1"] = "00:00:05"
        });

        options.ServerAddress.Should().Be("https://localhost:5001");
        options.Pattern.Should().Be("custom-hub");
        options.ApiKey.Should().Be("configured-key");
        options.ClientType.Should().Be("configured-type");
        options.ClientMachine.Should().Be("configured-machine");
        options.SendMessageTimeout.Should().Be(TimeSpan.FromSeconds(15));
        options.ReconnectDelays.Should().Equal(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AddThargaCommunicationClient_WithoutOptionalValuesInConfig_KeepsDefaults()
    {
        var options = RegisterWithConfig(new Dictionary<string, string>
        {
            ["Tharga:Communication:ServerAddress"] = "https://localhost:5001"
        });

        options.ApiKey.Should().BeNull();
        options.SendMessageTimeout.Should().Be(TimeSpan.FromSeconds(60));
        options.ReconnectDelays.Should().Equal(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddThargaCommunicationClient_WithConfigAndCallback_CallbackWins()
    {
        var options = RegisterWithConfig(new Dictionary<string, string>
        {
            ["Tharga:Communication:ServerAddress"] = "https://localhost:5001",
            ["Tharga:Communication:ApiKey"] = "configured-key",
            ["Tharga:Communication:ClientType"] = "configured-type"
        }, o => o.ApiKey = "code-key");

        options.ApiKey.Should().Be("code-key");
        options.ClientType.Should().Be("configured-type");
    }

    private static ClientOptions RegisterWithConfig(Dictionary<string, string> values, Action<ClientOptions> callback = null)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true
        });
        builder.Configuration.AddInMemoryCollection(values);

        builder.AddThargaCommunicationClient(callback);

        return builder.Build().Services.GetRequiredService<IOptions<ClientOptions>>().Value;
    }
}
