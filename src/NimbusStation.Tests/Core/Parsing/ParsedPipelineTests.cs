using NimbusStation.Core.Parsing;

namespace NimbusStation.Tests.Core.Parsing;

public class ParsedPipelineTests
{
    [Fact]
    public void HasExternalCommands_SingleSegment_ReturnsFalse()
    {
        var result = PipelineParser.Parse("cosmos query");

        Assert.False(result.HasExternalCommands);
    }

    [Fact]
    public void HasExternalCommands_MultipleSegments_ReturnsTrue()
    {
        var result = PipelineParser.Parse("cosmos query | jq .");

        Assert.True(result.HasExternalCommands);
    }

    [Fact]
    public void InternalCommand_ReturnsFirstSegment()
    {
        var result = PipelineParser.Parse("cosmos query | jq . | head -5");

        Assert.NotNull(result.InternalCommand);
        Assert.Equal("cosmos query", result.InternalCommand.Content);
    }

    [Fact]
    public void ExternalCommands_ReturnsAllButFirst()
    {
        var result = PipelineParser.Parse("cosmos query | jq . | head -5");

        var external = result.ExternalCommands.ToList();
        Assert.Equal(2, external.Count);
        Assert.Equal("jq .", external[0].Content);
        Assert.Equal("head -5", external[1].Content);
    }

    [Fact]
    public void InternalCommand_InvalidPipeline_ReturnsNull()
    {
        var result = PipelineParser.Parse("");

        Assert.Null(result.InternalCommand);
    }
}
