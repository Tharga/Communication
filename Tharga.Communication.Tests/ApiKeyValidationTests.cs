using FluentAssertions;
using Microsoft.Extensions.Options;
using Tharga.Communication.Server;
using Xunit;
using ServerOptions = global::CommunicationOptions;

namespace Tharga.Communication.Tests;

public class ApiKeyValidationTests
{
    [Fact]
    public async Task DefaultValidator_NoKeysConfigured_AcceptsAll()
    {
        var sut = CreateSut(new ServerOptions());

        var result = await sut.ValidateAsync(null, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
        result.KeyId.Should().BeNull();
    }

    [Fact]
    public async Task DefaultValidator_NoKeysConfigured_AcceptsAnyKey()
    {
        var sut = CreateSut(new ServerOptions());

        var result = await sut.ValidateAsync("some-random-key", TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DefaultValidator_KeyConfigured_AcceptsMatchingKey()
    {
        var sut = CreateSut(new ServerOptions { ApiKeys = ["my-secret-key"] });

        var result = await sut.ValidateAsync("my-secret-key", TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
        result.KeyId.Should().Be("key-0");
    }

    [Fact]
    public async Task DefaultValidator_MultipleKeysConfigured_AcceptsAnyMatchAndIndexesEach()
    {
        var sut = CreateSut(new ServerOptions { ApiKeys = ["primary-key", "secondary-key", "tertiary-key"] });
        var ct = TestContext.Current.CancellationToken;

        var primary = await sut.ValidateAsync("primary-key", ct);
        var secondary = await sut.ValidateAsync("secondary-key", ct);
        var tertiary = await sut.ValidateAsync("tertiary-key", ct);

        primary.IsValid.Should().BeTrue();
        primary.KeyId.Should().Be("key-0");
        secondary.IsValid.Should().BeTrue();
        secondary.KeyId.Should().Be("key-1");
        tertiary.IsValid.Should().BeTrue();
        tertiary.KeyId.Should().Be("key-2");
    }

    [Fact]
    public async Task DefaultValidator_KeyConfigured_RejectsInvalidKey()
    {
        var sut = CreateSut(new ServerOptions { ApiKeys = ["correct-key"] });

        var result = await sut.ValidateAsync("wrong-key", TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DefaultValidator_KeyConfigured_RejectsMissingKey()
    {
        var sut = CreateSut(new ServerOptions { ApiKeys = ["correct-key"] });

        var result = await sut.ValidateAsync(null, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DefaultValidator_KeyConfigured_RejectsEmptyKey()
    {
        var sut = CreateSut(new ServerOptions { ApiKeys = ["correct-key"] });

        var result = await sut.ValidateAsync("", TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
    }

    private static DefaultApiKeyValidator CreateSut(ServerOptions options)
    {
        return new DefaultApiKeyValidator(Options.Create(options));
    }
}
