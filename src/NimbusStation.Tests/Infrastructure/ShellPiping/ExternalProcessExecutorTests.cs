using NimbusStation.Infrastructure.ShellPiping;

namespace NimbusStation.Tests.Infrastructure.ShellPiping;

/// <summary>
/// Integration tests for ExternalProcessExecutor using real system processes.
/// These tests assume a Unix-like environment (macOS/Linux) with standard commands available.
/// </summary>
public class ExternalProcessExecutorTests
{
    private readonly ExternalProcessExecutor _executor = new();

    [Fact]
    public async Task ExecuteAsync_EchoCommand_ReturnsOutput()
    {
        var result = await _executor.ExecuteAsync("echo", "hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello\n", result.StandardOutput);
        Assert.Equal("", result.StandardError);
    }

    [Fact]
    public async Task ExecuteAsync_WithStdinContent_PipesToProcess()
    {
        var result = await _executor.ExecuteAsync(
            command: "cat",
            stdinContent: "piped content");

        Assert.True(result.IsSuccess);
        Assert.Equal("piped content", result.StandardOutput);
    }

    [Fact]
    public async Task ExecuteAsync_WithArguments_PassesArguments()
    {
        var result = await _executor.ExecuteAsync(
            command: "head",
            arguments: "-1",
            stdinContent: "line1\nline2\nline3\n");

        Assert.True(result.IsSuccess);
        Assert.Equal("line1\n", result.StandardOutput);
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentCommand_ReturnsStartupError()
    {
        var result = await _executor.ExecuteAsync("nonexistent_command_12345");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Contains("nonexistent_command_12345", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_NonZeroExitCode_ReturnsFailedResult()
    {
        // grep returns exit code 1 when no match is found
        var result = await _executor.ExecuteAsync(
            command: "grep",
            arguments: "nomatch",
            stdinContent: "this will not match");

        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ExitCode);
        Assert.False(result.WasKilled);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessClosesStdinEarly_HandlesGracefully()
    {
        // head -1 closes stdin after reading the first line.
        // This should not cause an error when we try to write more data.
        var largeContent = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"Line {i}"));

        var result = await _executor.ExecuteAsync(
            command: "head",
            arguments: "-1",
            stdinContent: largeContent);

        Assert.True(result.IsSuccess);
        Assert.Equal("Line 1\n", result.StandardOutput);
    }

    [Fact]
    public async Task ExecuteAsync_CommandWritesToStderr_CapturesStderr()
    {
        // ls on a non-existent file writes to stderr
        var result = await _executor.ExecuteAsync(
            command: "ls",
            arguments: "/nonexistent_path_12345");

        Assert.False(result.IsSuccess);
        Assert.NotEqual(0, result.ExitCode);
        Assert.NotEmpty(result.StandardError);
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_KillsProcessAndReturnsKilledResult()
    {
        using var cts = new CancellationTokenSource();

        // Start a long-running process
        var task = _executor.ExecuteAsync(
            command: "sleep",
            arguments: "10",
            cancellationToken: cts.Token);

        // Give the process time to start
        await Task.Delay(100);

        // Cancel it
        cts.Cancel();

        var result = await task;

        Assert.True(result.WasKilled);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_WcCountsLines_ReturnsCorrectCount()
    {
        var result = await _executor.ExecuteAsync(
            command: "wc",
            arguments: "-l",
            stdinContent: "line1\nline2\nline3\n");

        Assert.True(result.IsSuccess);
        // wc -l output format varies by platform, but should contain "3"
        Assert.Contains("3", result.StandardOutput);
    }

    [Fact]
    public async Task ExecuteAsync_GrepFindsMatch_ReturnsMatchingLines()
    {
        var result = await _executor.ExecuteAsync(
            command: "grep",
            arguments: "hello",
            stdinContent: "hello world\ngoodbye world\nhello again\n");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello world\nhello again\n", result.StandardOutput);
    }

    [Fact]
    public async Task ExecuteAsync_NoStdinContent_ClosesStdinImmediately()
    {
        // cat with no stdin should exit immediately (not hang waiting for input)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var result = await _executor.ExecuteAsync(
            command: "cat",
            cancellationToken: cts.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal("", result.StandardOutput);
    }
}
