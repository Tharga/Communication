namespace Tharga.Communication.Server.Communication;

public record Response<T>
{
    public Response(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    private Response()
    {
        Value = default;
        IsSuccess = true;
    }

    public T Value { get; }
    public bool IsSuccess { get; private set; }
    public string Code { get; private set; }
    public string Message { get; private set; }
    public int? StatusCode { get; private set; }

    public static Response<T> Ok(T value, int? statusCode = null)
    {
        return new Response<T>(value)
        {
            StatusCode = statusCode
        };
    }

    public static Response<T> Fail(string code, string message, int? statusCode = null)
    {
        return new Response<T>()
        {
            IsSuccess = false,
            Code = code,
            Message = message,
            StatusCode = statusCode
        };
    }
}