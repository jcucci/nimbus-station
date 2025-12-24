using NimbusStation.Core.Parsing;

namespace NimbusStation.Tests.Core.Parsing;

public class PipelineParserParseTests
{
    [Fact]
    public void SingleCommand_NoPipe_ReturnsSingleSegment()
    {
        var result = PipelineParser.Parse("cosmos query \"SELECT * FROM c\"");

        Assert.True(result.IsValid);
        Assert.Single(result.Segments);
        Assert.Equal("cosmos query \"SELECT * FROM c\"", result.Segments[0].Content);
        Assert.True(result.Segments[0].IsFirst);
        Assert.True(result.Segments[0].IsLast);
        Assert.Equal(0, result.Segments[0].Index);
    }

    [Fact]
    public void TwoCommands_SinglePipe_ReturnsTwoSegments()
    {
        var result = PipelineParser.Parse("cosmos query \"SELECT *\" | jq .");

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Segments.Count);

        Assert.Equal("cosmos query \"SELECT *\"", result.Segments[0].Content);
        Assert.True(result.Segments[0].IsFirst);
        Assert.False(result.Segments[0].IsLast);
        Assert.Equal(0, result.Segments[0].Index);

        Assert.Equal("jq .", result.Segments[1].Content);
        Assert.False(result.Segments[1].IsFirst);
        Assert.True(result.Segments[1].IsLast);
        Assert.Equal(1, result.Segments[1].Index);
    }

    [Fact]
    public void ThreeCommands_TwoPipes_ReturnsThreeSegments()
    {
        var result = PipelineParser.Parse("blob cat data.json | jq '.records' | wc -l");

        Assert.True(result.IsValid);
        Assert.Equal(3, result.Segments.Count);

        Assert.Equal("blob cat data.json", result.Segments[0].Content);
        Assert.True(result.Segments[0].IsFirst);
        Assert.False(result.Segments[0].IsLast);

        Assert.Equal("jq '.records'", result.Segments[1].Content);
        Assert.False(result.Segments[1].IsFirst);
        Assert.False(result.Segments[1].IsLast);

        Assert.Equal("wc -l", result.Segments[2].Content);
        Assert.False(result.Segments[2].IsFirst);
        Assert.True(result.Segments[2].IsLast);
    }

    [Fact]
    public void PipeInsideDoubleQuotes_PreservedAsContent()
    {
        var result = PipelineParser.Parse("cosmos query \"SELECT * | WHERE id = 1\"");

        Assert.True(result.IsValid);
        Assert.Single(result.Segments);
        Assert.Equal("cosmos query \"SELECT * | WHERE id = 1\"", result.Segments[0].Content);
    }

    [Fact]
    public void PipeInsideSingleQuotes_PreservedAsContent()
    {
        var result = PipelineParser.Parse("jq '.[] | .id'");

        Assert.True(result.IsValid);
        Assert.Single(result.Segments);
        Assert.Equal("jq '.[] | .id'", result.Segments[0].Content);
    }

    [Fact]
    public void EscapedPipe_PreservedAsContent()
    {
        var result = PipelineParser.Parse(@"echo hello\|world");

        Assert.True(result.IsValid);
        Assert.Single(result.Segments);
        Assert.Equal(@"echo hello\|world", result.Segments[0].Content);
    }

    [Fact]
    public void MixedQuotedAndUnquotedPipes_ParsedCorrectly()
    {
        var result = PipelineParser.Parse("cosmos query \"SELECT * FROM c\" | jq '.[] | .id' | head -5");

        Assert.True(result.IsValid);
        Assert.Equal(3, result.Segments.Count);
        Assert.Equal("cosmos query \"SELECT * FROM c\"", result.Segments[0].Content);
        Assert.Equal("jq '.[] | .id'", result.Segments[1].Content);
        Assert.Equal("head -5", result.Segments[2].Content);
    }

    [Fact]
    public void LeadingPipe_ReturnsError()
    {
        var result = PipelineParser.Parse("| jq .");

        Assert.False(result.IsValid);
        Assert.Equal("No command before pipe character", result.Error);
    }

    [Fact]
    public void TrailingPipe_ReturnsError()
    {
        var result = PipelineParser.Parse("cosmos query |");

        Assert.False(result.IsValid);
        Assert.Equal("No command after final pipe character", result.Error);
    }

    [Fact]
    public void EmptyMiddleSegment_ReturnsError()
    {
        var result = PipelineParser.Parse("cosmos query | | jq .");

        Assert.False(result.IsValid);
        Assert.Equal("Empty segment at position 2", result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrWhitespaceInput_ReturnsError(string? input)
    {
        var result = PipelineParser.Parse(input);

        Assert.False(result.IsValid);
        Assert.Equal("Input is empty", result.Error);
    }

    [Fact]
    public void TrailingBackslash_PreservedInOutput()
    {
        var result = PipelineParser.Parse(@"echo test\");

        Assert.True(result.IsValid);
        Assert.Single(result.Segments);
        Assert.Equal(@"echo test\", result.Segments[0].Content);
    }

    [Fact]
    public void WhitespaceAroundPipe_TrimmedFromSegments()
    {
        var result = PipelineParser.Parse("  cosmos query  |  jq .  ");

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Segments.Count);
        Assert.Equal("cosmos query", result.Segments[0].Content);
        Assert.Equal("jq .", result.Segments[1].Content);
    }

    [Fact]
    public void EscapedBackslashFollowedByPipe_SplitsOnPipe()
    {
        // \\| = escaped backslash (\) followed by unescaped pipe
        // The pipe IS a separator because the backslash escapes the next backslash, not the pipe
        var result = PipelineParser.Parse(@"echo a\\| cat");

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Segments.Count);
        Assert.Equal(@"echo a\\", result.Segments[0].Content);
        Assert.Equal("cat", result.Segments[1].Content);
    }

    [Fact]
    public void EscapedBackslashFollowedByEscapedPipe_DoesNotSplit()
    {
        // \\\| = escaped backslash (\) followed by escaped pipe (|)
        // Neither is a separator - results in literal \|
        var result = PipelineParser.Parse(@"echo a\\\|b");

        Assert.True(result.IsValid);
        Assert.Single(result.Segments);
        Assert.Equal(@"echo a\\\|b", result.Segments[0].Content);
    }

    [Fact]
    public void MultipleEscapedBackslashesFollowedByPipe_SplitsOnPipe()
    {
        // \\\\| = two escaped backslashes (\\) followed by unescaped pipe
        var result = PipelineParser.Parse(@"echo a\\\\| cat");

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Segments.Count);
        Assert.Equal(@"echo a\\\\", result.Segments[0].Content);
        Assert.Equal("cat", result.Segments[1].Content);
    }
}
