using System.Reflection;
using System.Text.Json;
using Tharga.Communication.Contract;

namespace Tharga.Communication.MessageHandler;

public class MessageExecutor : IMessageExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHandlerTypeService _handlerTypeService;

    public MessageExecutor(IServiceProvider serviceProvider, IHandlerTypeService handlerTypeService /*, IActionEventService actionEventService, ILogger<MessageExecutor> logger*/)
    {
        _serviceProvider = serviceProvider;
        _handlerTypeService = handlerTypeService;
    }

    public async Task<IMessageWrapper> ExecuteAsync(string connectionId, IMessageWrapper wrapper)
    {
        var shortTypeName = string.Join(",", wrapper.Type.Split(",").Take(2));
        var type = Type.GetType(shortTypeName);
        type ??= Tharga.Runtime.TypeExtensions.GetType(wrapper.Type) ?? throw new InvalidOperationException($"Cannot find type from string '{wrapper.Type}'.");

        var payload = JsonSerializer.Deserialize(wrapper.Payload, type);

        if (!_handlerTypeService.TryGetHandler(type, out var handlerTypeInfo))
        {
            throw new InvalidOperationException($"Cannot find handler for type '{type.FullName}'.");
        }

        var handler = _serviceProvider.GetService(handlerTypeInfo.HandlerType)
                      ?? throw new InvalidOperationException($"Cannot get instance of handler of type '{handlerTypeInfo.HandlerType.FullName}'.");

        if (connectionId != null)
        {
            var baseType = handler.GetType().BaseType!;
            var prop = baseType.GetProperty("ConnectionId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var setter = prop!.GetSetMethod(true);
            setter!.Invoke(handler, [connectionId]);
        }

        var invokeResult = handlerTypeInfo.HandlerMethod.Invoke(handler, [payload]);

        if (invokeResult is Task task)
        {
            await task;

            var returnType = handlerTypeInfo.HandlerMethod.ReturnType;

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var responseType = returnType.GetGenericArguments()[0];
                var responseValue = returnType
                    .GetProperty("Result")!
                    .GetValue(task);

                return new RequestWrapper
                {
                    Payload = JsonSerializer.Serialize(responseValue),
                    Type = responseType.AssemblyQualifiedName!
                };
            }
        }

        return null;
    }
}