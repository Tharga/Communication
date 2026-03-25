namespace Tharga.Communication.Client;

/// <summary>
/// Provides a unique identifier for the current client application instance.
/// </summary>
public interface IInstanceService
{
    /// <summary>Gets the unique key identifying this running instance of the client.</summary>
    Guid AgentInstanceKey { get; }
}