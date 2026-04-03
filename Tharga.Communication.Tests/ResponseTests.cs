using FluentAssertions;
using Tharga.Communication.Server.Communication;
using Xunit;

namespace Tharga.Communication.Tests;

public class ResponseTests
{
    [Fact]
    public void Constructor_SetsValueAndSuccess()
    {
        var response = new Response<string>("hello");

        response.Value.Should().Be("hello");
        response.IsSuccess.Should().BeTrue();
        response.Code.Should().BeNull();
        response.Message.Should().BeNull();
        response.StatusCode.Should().BeNull();
    }

    [Fact]
    public void Ok_SetsValueAndStatusCode()
    {
        var response = Response<int>.Ok(42, 200);

        response.Value.Should().Be(42);
        response.IsSuccess.Should().BeTrue();
        response.StatusCode.Should().Be(200);
    }

    [Fact]
    public void Ok_WithoutStatusCode_DefaultsToNull()
    {
        var response = Response<string>.Ok("data");

        response.IsSuccess.Should().BeTrue();
        response.StatusCode.Should().BeNull();
    }

    [Fact]
    public void Fail_SetsErrorProperties()
    {
        var response = Response<string>.Fail("ERR_TIMEOUT", "Request timed out", 408);

        response.IsSuccess.Should().BeFalse();
        response.Value.Should().BeNull();
        response.Code.Should().Be("ERR_TIMEOUT");
        response.Message.Should().Be("Request timed out");
        response.StatusCode.Should().Be(408);
    }

    [Fact]
    public void Fail_WithoutStatusCode_DefaultsToNull()
    {
        var response = Response<int>.Fail("ERR", "failed");

        response.IsSuccess.Should().BeFalse();
        response.StatusCode.Should().BeNull();
    }
}
