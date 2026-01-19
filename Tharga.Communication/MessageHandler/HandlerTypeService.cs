using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Runtime;

namespace Tharga.Communication.MessageHandler;

public class HandlerTypeService : IHandlerTypeService
{
    private readonly Dictionary<Type, HandlerTypeInfo> _handlerTypes;

    public HandlerTypeService(Dictionary<Type, HandlerTypeInfo> handlerTypes)
    {
        _handlerTypes = handlerTypes;
    }

    public bool TryGetHandler(Type type, out HandlerTypeInfo handlerTypeInfo)
    {
        return _handlerTypes.TryGetValue(type, out handlerTypeInfo);
    }

    public static Dictionary<Type, HandlerTypeInfo> GetHandlerTypes(IServiceCollection serviceCollection, IEnumerable<Assembly> assemblies = null)
    {
        var assemblyArray = assemblies?.ToArray();

        var postHandlerTypes = AssemblyService
            .GetTypes(x => x.IsOfType(typeof(PostMessageHandlerBase<>)), assemblyArray)
            .Where(x => x.BaseType?.IsGenericType ?? false)
            .Where(x => x != typeof(PostMessageHandlerBase<>))
            .Where(x => !x.IsAbstract)
            .Select(x =>
            {
                var payloadType = x.BaseType?.GenericTypeArguments.Single();
                if (payloadType == null) return null;
                var handlerType = x.AsType();
                var handlerMethod = handlerType.GetMethod(nameof(PostMessageHandlerBase<object>.Handle));

                serviceCollection.AddTransient(handlerType);

                return new HandlerTypeInfo
                {
                    PayloadType = payloadType,
                    HandlerType = handlerType,
                    HandlerMethod = handlerMethod
                };
            })
            .Where(x => x != null);

        var sendHandlerTypes = AssemblyService
            .GetTypes(x => x.IsOfType(typeof(SendMessageHandlerBase<,>)), assemblyArray)
            .Where(x => x.BaseType?.IsGenericType ?? false)
            .Where(x => x != typeof(SendMessageHandlerBase<,>))
            .Where(x => !x.IsAbstract)
            .Select(x =>
            {
                var payloadType = x.BaseType?.GenericTypeArguments.First();
                if (payloadType == null) return null;
                var handlerType = x.AsType();
                var handlerMethod = handlerType.GetMethod(nameof(SendMessageHandlerBase<object, object>.Handle));

                serviceCollection.AddTransient(handlerType);

                return new HandlerTypeInfo
                {
                    PayloadType = payloadType,
                    HandlerType = handlerType,
                    HandlerMethod = handlerMethod
                };
            })
            .Where(x => x != null);

        return postHandlerTypes.Union(sendHandlerTypes).ToDictionary(x => x.PayloadType, x => x); ;
    }
}