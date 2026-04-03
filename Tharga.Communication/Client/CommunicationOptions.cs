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

    /// <summary>Gets or sets the default timeout for <see cref="Communication.IClientCommunication.SendMessage{TRequest,TResponse}"/>. Defaults to 60 seconds.</summary>
    public TimeSpan SendMessageTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets additional assemblies to scan for message handlers.
    /// Use this when handlers are defined in external packages that are not discovered by the default assembly scan.
    /// </summary>
    public System.Reflection.Assembly[] AdditionalAssemblies { get; set; }
}