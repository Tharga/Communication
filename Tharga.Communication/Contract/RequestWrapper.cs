namespace Tharga.Communication.Contract;

/// <summary>
/// Default implementation of <see cref="IMessageWrapper"/> used to wrap serialized messages for transport.
/// </summary>
public record RequestWrapper : IMessageWrapper
{
    /// <inheritdoc />
    public required string Type { get; init; }

    /// <inheritdoc />
    public required string Payload { get; init; }

    /// <summary>
    /// Identifies a request-response message. The receiver copies it into its response so the sender can
    /// match the response to its request and ignore a response that arrives after its request timed out.
    /// <c>null</c> on fire-and-forget messages and on messages from peers older than version 1.2,
    /// which are matched the way they were before.
    /// </summary>
    public string RequestId { get; init; }

    internal IMessageWrapper Respond(IMessageWrapper response)
    {
        if (RequestId == null || response == null) return response;

        return response is RequestWrapper wrapper
            ? wrapper with { RequestId = RequestId }
            : new RequestWrapper { Type = response.Type, Payload = response.Payload, RequestId = RequestId };
    }

    internal static bool IsResponseTo(IMessageWrapper response, string requestId)
    {
        var responseRequestId = (response as RequestWrapper)?.RequestId;
        return responseRequestId == null || responseRequestId == requestId;
    }
}