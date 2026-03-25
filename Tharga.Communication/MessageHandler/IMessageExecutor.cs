using Tharga.Communication.Contract;

namespace Tharga.Communication.MessageHandler;

/// <summary>
/// Routes incoming messages to the appropriate registered handler for execution.
/// </summary>
public interface IMessageExecutor
{
    /// <summary>
    /// Executes the handler for the given message and returns the response wrapper.
    /// </summary>
    /// <param name="connectionId">The SignalR connection ID of the sender.</param>
    /// <param name="wrapper">The incoming message wrapper containing type and payload.</param>
    /// <returns>A response wrapper, or <c>null</c> for fire-and-forget messages.</returns>
    Task<IMessageWrapper> ExecuteAsync(string connectionId, IMessageWrapper wrapper);
}