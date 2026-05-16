namespace Tharga.Communication.Client;

/// <summary>
/// Configuration options for the Tharga.Communication client.
/// Bound from the <c>Tharga:Communication</c> configuration section.
/// </summary>
public record CommunicationOptions
{
    /// <summary>Gets or sets the server URL to connect to (e.g. <c>https://localhost:5001</c>).</summary>
    public string ServerAddress { get; set; }

    /// <summary>Gets or sets the hub endpoint pattern. Defaults to <c>"hub"</c>.</summary>
    public string Pattern { get; set; }

    /// <summary>Gets or sets the delays between reconnection attempts.</summary>
    public TimeSpan[] ReconnectDelays { get; set; }

    /// <summary>Gets or sets the API key sent to the server for authentication. When set, the key is sent as an <c>X-Api-Key</c> header during SignalR negotiation.</summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// Gets or sets an override for the client identity sent as <c>X-Client-Type</c>. When null or empty,
    /// the entry assembly name is used. Useful when one assembly hosts multiple roles, or the entry assembly
    /// name is generic (e.g. a generic host).
    /// </summary>
    public string ClientType { get; set; }

    /// <summary>
    /// Gets or sets an override for the machine name sent as <c>X-Client-Machine</c>. When null or empty,
    /// <see cref="Environment.MachineName"/> is used. Useful in containerized environments where the OS
    /// hostname is meaningless (e.g. a generated hash).
    /// </summary>
    public string ClientMachine { get; set; }

    /// <summary>Gets or sets the default timeout for <see cref="Communication.IClientCommunication.SendMessage{TRequest,TResponse}"/>. Defaults to 60 seconds.</summary>
    public TimeSpan SendMessageTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets additional assemblies to scan for message handlers.
    /// Use this when handlers are defined in external packages that are not discovered by the default assembly scan.
    /// </summary>
    public System.Reflection.Assembly[] AdditionalAssemblies { get; set; }
}