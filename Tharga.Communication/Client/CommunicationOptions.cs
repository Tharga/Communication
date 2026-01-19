namespace Tharga.Communication.Client;

public record CommunicationOptions
{
    public string ServerAddress { get; set; }
    public string Pattern { get; set; }
    public TimeSpan[] ReconnectDelays { get; set; }
}