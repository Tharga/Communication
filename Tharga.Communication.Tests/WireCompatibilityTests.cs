using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.SignalR;
using Tharga.Communication.Contract;
using Xunit;

namespace Tharga.Communication.Tests;

/// <summary>
/// Peers on different versions must still understand each other's message wrappers.
/// </summary>
public class WireCompatibilityTests
{
    /// <summary>The message wrapper as released up to version 1.1.</summary>
    public record PreviousWireWrapper
    {
        public required string Type { get; init; }
        public required string Payload { get; init; }
    }

    private static readonly JsonSerializerOptions HubProtocolOptions = new JsonHubProtocolOptions().PayloadSerializerOptions;
    private static readonly JsonSerializerOptions HubMethodOptions = new() { PropertyNameCaseInsensitive = true };

    public static TheoryData<string> ReaderOptions => [nameof(HubProtocolOptions), nameof(HubMethodOptions)];

    [Theory]
    [MemberData(nameof(ReaderOptions))]
    public void CurrentWrapper_CanBeReadByAPreviousVersion(string readerOptions)
    {
        var json = JsonSerializer.Serialize(new RequestWrapper { Type = "type", Payload = "payload", RequestId = "request", Error = "error" }, HubProtocolOptions);

        var read = JsonSerializer.Deserialize<PreviousWireWrapper>(json, Options(readerOptions));

        read.Should().Be(new PreviousWireWrapper { Type = "type", Payload = "payload" });
    }

    [Theory]
    [MemberData(nameof(ReaderOptions))]
    public void PreviousWrapper_CanBeReadByTheCurrentVersion(string readerOptions)
    {
        var json = JsonSerializer.Serialize(new PreviousWireWrapper { Type = "type", Payload = "payload" }, HubProtocolOptions);

        var read = JsonSerializer.Deserialize<RequestWrapper>(json, Options(readerOptions));

        read.Should().Be(new RequestWrapper { Type = "type", Payload = "payload" });
        read.RequestId.Should().BeNull();
        read.Error.Should().BeNull();
    }

    private static JsonSerializerOptions Options(string name) => name == nameof(HubProtocolOptions) ? HubProtocolOptions : HubMethodOptions;
}
