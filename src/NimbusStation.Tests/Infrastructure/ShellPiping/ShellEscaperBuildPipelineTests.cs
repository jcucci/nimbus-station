using NimbusStation.Infrastructure.ShellPiping;

namespace NimbusStation.Tests.Infrastructure.ShellPiping;

/// <summary>
/// Tests for ShellEscaper.BuildPipelineCommand and platform-aware escaping.
/// </summary>
public class ShellEscaperBuildPipelineTests
{
    [Fact]
    public void Escape_UsesCorrectEscapingForCurrentPlatform()
    {
        var result = ShellEscaper.Escape("echo 'hello'");

        if (PlatformHelper.IsWindows)
            Assert.Equal("'echo ''hello'''", result);
        else
            Assert.Equal(@"'echo '\''hello'\'''", result);
    }

    [Fact]
    public void BuildPipelineCommand_SingleCommand_ReturnsUnchanged() =>
        Assert.Equal("jq .", ShellEscaper.BuildPipelineCommand(["jq ."]));

    [Fact]
    public void BuildPipelineCommand_TwoCommands_JoinsWithPipe() =>
        Assert.Equal("jq . | head -5", ShellEscaper.BuildPipelineCommand(["jq .", "head -5"]));

    [Fact]
    public void BuildPipelineCommand_ThreeCommands_JoinsWithPipes() =>
        Assert.Equal("jq . | grep foo | head -5", ShellEscaper.BuildPipelineCommand(["jq .", "grep foo", "head -5"]));

    [Fact]
    public void BuildPipelineCommand_EmptyList_ReturnsEmptyString() =>
        Assert.Equal("", ShellEscaper.BuildPipelineCommand([]));

    [Fact]
    public void BuildPipelineCommand_NullList_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => ShellEscaper.BuildPipelineCommand(null!));
}
