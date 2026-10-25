using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tharga.Communication.Contract;
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

    [Fact]
    public async Task CustomValidator_KeyIdAndKeyNameReachBuild_OverTheHub()
    {
        var ct = TestContext.Current.CancellationToken;
        var builder = WebApplication.CreateBuilder();
        builder.AddThargaCommunicationServer(options =>
        {
            options.RegisterApiKeyValidator<TestValidator>();
            options.RegisterClientStateService<RecordingStateService>();
            options.RegisterClientRepository<MemoryClientRepository<ClientConnectionInfo>, ClientConnectionInfo>();
        });

        var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.UseThargaCommunicationServer();
        await app.StartAsync(ct);

        try
        {
            var connection = new HubConnectionBuilder()
                .WithUrl($"{app.Urls.First()}/hub", options =>
                {
                    options.Headers.Add(Constants.Header.Instance, Guid.NewGuid().ToString());
                    options.Headers.Add(Constants.Header.Machine, "test-machine");
                    options.Headers.Add(Constants.Header.Type, "test-app");
                    options.Headers.Add(Constants.Header.Version, "1.0.0");
                    options.Headers.Add(Constants.Header.ApiKey, "hello");
                })
                .Build();

            await connection.StartAsync(ct);

            var stateService = app.Services.GetRequiredService<RecordingStateService>();
            var built = await stateService.Built.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);

            built.KeyId.Should().Be("test-key-id");
            built.KeyName.Should().Be("Test Friendly Name");

            await connection.DisposeAsync();
        }
        finally
        {
            await app.StopAsync(ct);
            await app.DisposeAsync();
        }
    }

    private sealed class RecordingStateService : ClientStateServiceBase<ClientConnectionInfo>
    {
        public RecordingStateService(IServiceProvider serviceProvider, IOptions<CommunicationOptions> options)
            : base(serviceProvider, options) { }

        public TaskCompletionSource<IClientConnectionInfo> Built { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override ClientConnectionInfo Build(IClientConnectionInfo info)
        {
            Built.TrySetResult(info);
            return new ClientConnectionInfo
            {
                Instance = info.Instance,
                ConnectionId = info.ConnectionId,
                Machine = info.Machine,
                Type = info.Type,
                Version = info.Version,
                KeyId = info.KeyId,
                KeyName = info.KeyName,
                IsConnected = info.IsConnected,
                ConnectTime = info.ConnectTime
            };
        }

        protected override ClientConnectionInfo BuildDisconnect(ClientConnectionInfo info, DateTime disconnectTime) =>
            info with { IsConnected = false, DisconnectTime = disconnectTime };
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
