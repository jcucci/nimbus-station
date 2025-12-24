using NimbusStation.Core.Parsing;

namespace NimbusStation.Tests.Core.Parsing;

public class PipelineParserContainsPipeTests
{
    [Fact]
    public void NoPipe_ReturnsFalse() =>
        Assert.False(PipelineParser.ContainsPipe("cosmos query \"SELECT *\""));

    [Fact]
    public void UnquotedPipe_ReturnsTrue() =>
        Assert.True(PipelineParser.ContainsPipe("cosmos query | jq ."));

    [Fact]
    public void PipeInDoubleQuotes_ReturnsFalse() =>
        Assert.False(PipelineParser.ContainsPipe("cosmos query \"a | b\""));

    [Fact]
    public void PipeInSingleQuotes_ReturnsFalse() =>
        Assert.False(PipelineParser.ContainsPipe("jq '.[] | .id'"));

    [Fact]
    public void EscapedPipe_ReturnsFalse() =>
        Assert.False(PipelineParser.ContainsPipe(@"echo hello\|world"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NullOrEmptyInput_ReturnsFalse(string? input) =>
        Assert.False(PipelineParser.ContainsPipe(input));

    [Fact]
    public void MultiplePipes_ReturnsTrue() =>
        Assert.True(PipelineParser.ContainsPipe("a | b | c"));
}
