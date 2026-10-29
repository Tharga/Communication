using System.Reflection;
using System.Text.Json;

namespace Tharga.Communication.Contract;

/// <summary>
/// The response a receiver sends when it cannot handle a request-response message.
/// It keeps the shape earlier versions use (the request type with an <c>{"Error": "..."}</c> payload)
/// and also sets <see cref="RequestWrapper.Error"/>.
/// </summary>
internal static class ErrorResponse
{
    private const string ErrorProperty = "Error";

    public static RequestWrapper Create(RequestWrapper request, Exception exception)
    {
        var message = (exception is TargetInvocationException { InnerException: { } inner } ? inner : exception).Message;
        return request with
        {
            Payload = JsonSerializer.Serialize(new Dictionary<string, string> { [ErrorProperty] = message }),
            Error = message
        };
    }

    public static bool TryGetError(IMessageWrapper response, RequestWrapper request, Type responseType, out string error)
    {
        error = (response as RequestWrapper)?.Error;
        if (error != null) return true;

        var isEarlierVersionErrorShape = response != null
                                         && response.Type == request.Type
                                         && request.Type != responseType.AssemblyQualifiedName;
        return isEarlierVersionErrorShape && TryReadErrorPayload(response.Payload, out error);
    }

    private static bool TryReadErrorPayload(string payload, out string error)
    {
        error = null;
        if (string.IsNullOrEmpty(payload)) return false;

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            if (document.RootElement.EnumerateObject().ToArray() is not [{ Name: ErrorProperty, Value.ValueKind: JsonValueKind.String } only]) return false;

            error = only.Value.GetString();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
