namespace Tharga.Communication.Server;

/// <summary>
/// Validates the API key presented by a client during connection negotiation.
/// Implementations are registered via <see cref="CommunicationOptions.RegisterApiKeyValidator{T}"/>.
/// If no validator is registered, a default implementation that checks <see cref="CommunicationOptions.ApiKeys"/> is used.
/// </summary>
/// <remarks>
/// If a validator needs access to the incoming <see cref="Microsoft.AspNetCore.Http.HttpContext"/>
/// (for example, to enforce IP allowlists or inspect custom headers), inject
/// <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/> via constructor injection.
/// The framework intentionally does not pass <c>HttpContext</c> to keep the interface narrow.
/// </remarks>
public interface IApiKeyValidator
{
    /// <summary>
    /// Validates the provided API key. Return <c>IsValid = true</c> to accept the connection,
    /// <c>IsValid = false</c> to reject it.
    /// </summary>
    /// <param name="apiKey">The API key provided by the client, or <c>null</c>/empty if none was sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);
}
