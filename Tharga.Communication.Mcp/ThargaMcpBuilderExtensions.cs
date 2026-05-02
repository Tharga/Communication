using Tharga.Mcp;

namespace Tharga.Communication.Mcp;

/// <summary>
/// Extension methods for <see cref="IThargaMcpBuilder"/> that register Tharga.Communication MCP providers.
/// </summary>
public static class ThargaMcpBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="CommunicationResourceProvider"/>, exposing connected clients, active subscriptions,
    /// and registered handlers on the System scope.
    /// </summary>
    public static IThargaMcpBuilder AddCommunication(this IThargaMcpBuilder builder)
    {
        builder.AddResourceProvider<CommunicationResourceProvider>();
        return builder;
    }
}
