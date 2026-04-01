using FluentAssertions;
using Tharga.Communication.Server;
using Xunit;

namespace Tharga.Communication.Tests;

public class ApiKeyValidationTests
{
    [Fact]
    public void ValidateApiKey_NoKeysConfigured_AcceptsAll()
    {
        var options = new CommunicationOptions();

        var result = options.ValidateApiKey(null);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateApiKey_NoKeysConfigured_AcceptsAnyKey()
    {
        var options = new CommunicationOptions();

        var result = options.ValidateApiKey("some-random-key");

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateApiKey_PrimaryKeyConfigured_AcceptsMatchingKey()
    {
        var options = new CommunicationOptions { PrimaryApiKey = "my-secret-key" };

        var result = options.ValidateApiKey("my-secret-key");

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateApiKey_SecondaryKeyConfigured_AcceptsMatchingKey()
    {
        var options = new CommunicationOptions { SecondaryApiKey = "secondary-key" };

        var result = options.ValidateApiKey("secondary-key");

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateApiKey_BothKeysConfigured_AcceptsPrimaryKey()
    {
        var options = new CommunicationOptions
        {
            PrimaryApiKey = "primary-key",
            SecondaryApiKey = "secondary-key"
        };

        var result = options.ValidateApiKey("primary-key");

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateApiKey_BothKeysConfigured_AcceptsSecondaryKey()
    {
        var options = new CommunicationOptions
        {
            PrimaryApiKey = "primary-key",
            SecondaryApiKey = "secondary-key"
        };

        var result = options.ValidateApiKey("secondary-key");

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateApiKey_KeyConfigured_RejectsInvalidKey()
    {
        var options = new CommunicationOptions { PrimaryApiKey = "correct-key" };

        var result = options.ValidateApiKey("wrong-key");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateApiKey_KeyConfigured_RejectsMissingKey()
    {
        var options = new CommunicationOptions { PrimaryApiKey = "correct-key" };

        var result = options.ValidateApiKey(null);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateApiKey_KeyConfigured_RejectsEmptyKey()
    {
        var options = new CommunicationOptions { PrimaryApiKey = "correct-key" };

        var result = options.ValidateApiKey("");

        result.Should().BeFalse();
    }
}
