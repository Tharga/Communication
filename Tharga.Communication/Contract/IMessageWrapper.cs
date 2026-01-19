namespace Tharga.Communication.Contract;

public interface IMessageWrapper
{
    string Type { get; }
    string Payload { get; }
}