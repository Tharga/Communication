using Tharga.Communication.Contract;

namespace Tharga.Communication.MessageHandler;

public interface IMessageExecutor
{
    Task<IMessageWrapper> ExecuteAsync(string connectionId, IMessageWrapper wrapper);
}