using NimbusStation.Core.ShellPiping;
using NimbusStation.Infrastructure.ShellPiping;

namespace NimbusStation.Tests.Infrastructure.ShellPiping;

/// <summary>
/// Integration tests for ShellDelegator using real shell execution.
/// </summary>
public class ShellDelegatorTests
{
    private readonly ShellDelegator _delegator;

    public ShellDelegatorTests()
    {
        _delegator = new ShellDelegator(new ExternalProcessExecutor());
    }

    [Fact]
    public async Task ExecuteAsync_TwoCommands_PipesCorrectly()
    {
        var result = await _delegator.ExecuteAsync(
            externalCommands: ["cat", "head -2"],
            stdinContent: "line1\nline2\nline3\n");

        Assert.True(result.IsSuccess);
        Assert.Equal("line1\nline2\n", result.StandardOutput);
    }

    [Fact]
    public async Task ExecuteAsync_ThreeCommands_PipesCorrectly()
    {
        var result = await _delegator.ExecuteAsync(
            externalCommands: ["cat", "grep line", "head -1"],
            stdinContent: "line1\nline2\nline3\n");

        Assert.True(result.IsSuccess);
        Assert.Equal("line1\n", result.StandardOutput);
    }

    [Fact]
    public async Task ExecuteAsync_JqAndGrep_TransformsJson()
    {
        var json = "{\"name\": \"test\", \"value\": 42}\n{\"name\": \"foo\", \"value\": 1}\n";

        var result = await _delegator.ExecuteAsync(
            externalCommands: ["jq -r .name", "grep test"],
            stdinContent: json);

        Assert.True(result.IsSuccess);
        Assert.Equal("test\n", result.StandardOutput);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyCommands_ReturnsError()
    {
        var result = await _delegator.ExecuteAsync(
            externalCommands: [],
            stdinContent: "test");

        Assert.False(result.IsSuccess);
        Assert.Contains("No commands provided", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_SingleCommand_ReturnsError()
    {
        var result = await _delegator.ExecuteAsync(
            externalCommands: ["cat"],
            stdinContent: "test");

        Assert.False(result.IsSuccess);
        Assert.Contains("at least 2 commands", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_NullCommands_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _delegator.ExecuteAsync(externalCommands: null!, stdinContent: "test"));
    }

    [Fact]
    public async Task ExecuteAsync_CommandNotFound_WritesToStderr()
    {
        var result = await _delegator.ExecuteAsync(
            externalCommands: ["nonexistent_cmd_12345", "cat"],
            stdinContent: "test");

        Assert.Contains("not found", result.StandardError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_KillsProcess()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var result = await _delegator.ExecuteAsync(
            externalCommands: ["sleep 10", "cat"],
            stdinContent: "test",
            cancellationToken: cts.Token);

        Assert.True(result.WasKilled);
    }

    [Fact]
    public async Task ExecuteAsync_NoStdin_WorksCorrectly()
    {
        var result = await _delegator.ExecuteAsync(
            externalCommands: ["echo hello", "cat"]);

        Assert.True(result.IsSuccess);
        Assert.Equal("hello\n", result.StandardOutput);
    }

    [Fact]
    public async Task ExecuteAsync_CommandWithQuotes_HandlesEscaping()
    {
        var result = await _delegator.ExecuteAsync(
            externalCommands: ["echo 'hello world'", "cat"],
            stdinContent: null);

        Assert.True(result.IsSuccess);
        Assert.Equal("hello world\n", result.StandardOutput);
    }
}
