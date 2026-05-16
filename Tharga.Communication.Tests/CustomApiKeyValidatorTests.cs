using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Communication.Server;
using Xunit;

namespace Tharga.Communication.Tests;

public class CustomApiKeyValidatorTests
{
    [Fact]
    public void NoValidatorRegistered_FallsBackToDefault()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterClientStateService<NoOpStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });

        var app = builder.Build();
        var validator = app.Services.GetRequiredService<IApiKeyValidator>();

        validator.Should().BeOfType<DefaultApiKeyValidator>();
    }

    [Fact]
    public void CustomValidator_RegisteredViaConcreteType_IsUsed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterApiKeyValidator<TestValidator>();
            options.RegisterClientStateService<NoOpStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });

        var app = builder.Build();
        var validator = app.Services.GetRequiredService<IApiKeyValidator>();

        validator.Should().BeOfType<TestValidator>();
    }

    [Fact]
    public async Task CustomValidator_ProducesKeyIdAndKeyName()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterApiKeyValidator<TestValidator>();
            options.RegisterClientStateService<NoOpStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });

        var app = builder.Build();
        var validator = app.Services.GetRequiredService<IApiKeyValidator>();

        var result = await validator.ValidateAsync("hello", TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
        result.KeyId.Should().Be("test-key-id");
        result.KeyName.Should().Be("Test Friendly Name");
    }

    private sealed class TestValidator : IApiKeyValidator
    {
        public Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ApiKeyValidationResult
            {
                IsValid = true,
                KeyId = "test-key-id",
                KeyName = "Test Friendly Name"
            });
        }
    }

    private sealed class NoOpStateService : ClientStateServiceBase
    {
        public override Task ConnectAsync(ClientConnection clientConnection) => Task.CompletedTask;
        public override Task DisconnectedAsync(string connectionId) => Task.CompletedTask;
        public override async IAsyncEnumerable<IClientConnectionInfo> GetConnectionInfosAsync()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
