namespace Tharga.Communication.Server;

/// <summary>
/// The outcome of an <see cref="IApiKeyValidator.ValidateAsync"/> call.
/// </summary>
public record ApiKeyValidationResult
{
    /// <summary>Whether the connection should be accepted.</summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Stable, machine-friendly identifier of the matched key (e.g. a Guid or "key-0").
    /// Null when the validator accepted an unauthenticated/anonymous connection,
    /// or when the validator did not match a specific key.
    /// </summary>
    public string KeyId { get; init; }

    /// <summary>
    /// Optional human-readable label for the key (e.g. an admin-assigned name like "production-monitor").
    /// Useful for admin UIs and audit logs. Null if the validator does not surface a name.
    /// </summary>
    public string KeyName { get; init; }
}
