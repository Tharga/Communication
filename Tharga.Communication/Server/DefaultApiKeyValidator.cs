using Microsoft.Extensions.Options;

namespace Tharga.Communication.Server;

/// <summary>
/// Default <see cref="IApiKeyValidator"/> implementation. Accepts the API key if it matches any entry
/// in <see cref="CommunicationOptions.ApiKeys"/>. If <c>ApiKeys</c> is null or empty, all connections
/// are accepted (no auth configured).
/// </summary>
internal sealed class DefaultApiKeyValidator : IApiKeyValidator
{
    private readonly CommunicationOptions _options;

    public DefaultApiKeyValidator(IOptions<CommunicationOptions> options)
    {
        _options = options.Value;
    }

    public Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var keys = _options.ApiKeys;

        if (keys is null || keys.Length == 0)
        {
            return Task.FromResult(new ApiKeyValidationResult { IsValid = true });
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            return Task.FromResult(new ApiKeyValidationResult { IsValid = false });
        }

        for (var i = 0; i < keys.Length; i++)
        {
            if (keys[i] == apiKey)
            {
                return Task.FromResult(new ApiKeyValidationResult
                {
                    IsValid = true,
                    KeyId = $"key-{i}"
                });
            }
        }

        return Task.FromResult(new ApiKeyValidationResult { IsValid = false });
    }
}
