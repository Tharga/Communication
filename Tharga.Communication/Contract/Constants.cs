namespace Tharga.Communication.Contract;

public static class Constants
{
    public const string DefaultPattern = "hub";
    public const string PostMessage = "PostMessage";
    public const string SendMessage = "SendMessage";
    public const string ResponseMessage = "ResponseMessage";
    public const string DefaultModel = "tinyllama";

    public static class Header
    {
        public const string Instance = "X-Client-Instance";
        public const string Machine = "X-Client-Machine";
        public const string Type = "X-Client-Type";
        public const string Version = "X-Client-Version";
    }
}