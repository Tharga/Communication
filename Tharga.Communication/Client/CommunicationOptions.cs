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
}