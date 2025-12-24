using NimbusStation.Core.ShellPiping;

namespace NimbusStation.Tests.Core.ShellPiping;

public class ProcessResultTests
{
    [Fact]
    public void Success_CreatesResultWithZeroExitCode()
    {
        var result = ProcessResult.Success("output", "error");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("output", result.StandardOutput);
        Assert.Equal("error", result.StandardError);
        Assert.False(result.WasKilled);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Success_WithDefaultStderr_HasEmptyStderr()
    {
        var result = ProcessResult.Success("output");

        Assert.Equal("", result.StandardError);
    }

    [Fact]
    public void Failed_CreatesResultWithNonZeroExitCode()
    {
        var result = ProcessResult.Failed(exitCode: 1, stdout: "out", stderr: "err");

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("out", result.StandardOutput);
        Assert.Equal("err", result.StandardError);
        Assert.False(result.WasKilled);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Killed_CreatesResultWithWasKilledTrue()
    {
        var result = ProcessResult.Killed("partial output", "partial error");

        Assert.Equal(-1, result.ExitCode);
        Assert.Equal("partial output", result.StandardOutput);
        Assert.Equal("partial error", result.StandardError);
        Assert.True(result.WasKilled);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Killed_WithDefaults_HasEmptyOutput()
    {
        var result = ProcessResult.Killed();

        Assert.Equal("", result.StandardOutput);
        Assert.Equal("", result.StandardError);
    }

    [Fact]
    public void StartupError_CreatesResultWithErrorMessage()
    {
        var result = ProcessResult.StartupError("Command not found");

        Assert.Equal(-1, result.ExitCode);
        Assert.Equal("", result.StandardOutput);
        Assert.Equal("", result.StandardError);
        Assert.False(result.WasKilled);
        Assert.Equal("Command not found", result.Error);
    }

    [Fact]
    public void IsSuccess_TrueWhenExitCodeZeroAndNotKilledAndNoError()
    {
        var result = ProcessResult.Success("output");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_FalseWhenNonZeroExitCode()
    {
        var result = ProcessResult.Failed(exitCode: 1, stdout: "", stderr: "");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_FalseWhenKilled()
    {
        var result = ProcessResult.Killed();

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_FalseWhenStartupError()
    {
        var result = ProcessResult.StartupError("error");

        Assert.False(result.IsSuccess);
    }
}
