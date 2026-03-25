namespace Tharga.Communication.Contract;

/// <summary>
/// Constants used by the Tharga.Communication protocol.
/// </summary>
public static class Constants
{
    /// <summary>Default SignalR hub URL pattern.</summary>
    public const string DefaultPattern = "hub";

    /// <summary>Hub method name for fire-and-forget messages.</summary>
    public const string PostMessage = "PostMessage";

    /// <summary>Hub method name for request-response messages.</summary>
    public const string SendMessage = "SendMessage";

    /// <summary>Hub method name for response messages.</summary>
    public const string ResponseMessage = "ResponseMessage";

    /// <summary>Default model identifier.</summary>
    public const string DefaultModel = "tinyllama";

    /// <summary>
    /// HTTP header names sent by the client during hub connection.
    /// </summary>
    public static class Header
    {
        /// <summary>Header containing the client instance GUID.</summary>
        public const string Instance = "X-Client-Instance";

        /// <summary>Header containing the client machine name.</summary>
        public const string Machine = "X-Client-Machine";

        /// <summary>Header containing the client application type.</summary>
        public const string Type = "X-Client-Type";

        /// <summary>Header containing the client application version.</summary>
        public const string Version = "X-Client-Version";
    }
}