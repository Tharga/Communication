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

    /// <summary>
    /// Optional consumer-defined values resolved together with the key (e.g. the tenant or team that owns it).
    /// They are carried to <see cref="ClientConnection.Metadata"/> and <see cref="IClientConnectionInfo.Metadata"/>
    /// so that <see cref="ClientStateServiceBase{T}"/> can use them when it builds a connection. Null when none are supplied.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
}
