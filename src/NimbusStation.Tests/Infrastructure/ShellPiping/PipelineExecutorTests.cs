using NimbusStation.Core.Commands;
using NimbusStation.Core.Output;
using NimbusStation.Core.Parsing;
using NimbusStation.Core.ShellPiping;
using NimbusStation.Infrastructure.ShellPiping;

namespace NimbusStation.Tests.Infrastructure.ShellPiping;

/// <summary>
/// Tests for PipelineExecutor covering single-pipe and multi-pipe scenarios.
/// Uses real ExternalProcessExecutor and ShellDelegator for integration testing.
/// </summary>
public class PipelineExecutorTests
{
    private readonly PipelineExecutor _executor;

    public PipelineExecutorTests()
    {
        var processExecutor = new ExternalProcessExecutor();
        var shellDelegator = new ShellDelegator(processExecutor);
        _executor = new PipelineExecutor(processExecutor, shellDelegator);
    }

    [Fact]
    public async Task ExecuteAsync_SinglePipe_InternalSucceeds_ExternalReceivesOutput()
    {
        var pipeline = PipelineParser.Parse("internal | cat");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                writer.WriteLine("hello from internal");
                return Task.FromResult(CommandResult.Ok());
            });

        Assert.True(result.Success);
        Assert.Equal("hello from internal\n", result.Output);
        Assert.Equal(0, result.ExternalExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_SinglePipe_InternalFails_ReturnsInternalError()
    {
        var pipeline = PipelineParser.Parse("internal | cat");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) => Task.FromResult(CommandResult.Error("Something went wrong")));

        Assert.False(result.Success);
        Assert.Equal("Something went wrong", result.Error);
        Assert.Null(result.ExternalExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_SinglePipe_ExternalNotFound_ReturnsActionableError()
    {
        var pipeline = PipelineParser.Parse("internal | nonexistent_command_12345");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                writer.WriteLine("output");
                return Task.FromResult(CommandResult.Ok());
            });

        Assert.False(result.Success);
        Assert.Contains("nonexistent_command_12345", result.Error);
        Assert.Contains("installed", result.Error);
        Assert.Contains("PATH", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_SinglePipe_ExternalNonZeroExit_ReturnsWithExitCode()
    {
        // grep returns exit code 1 when no match is found
        var pipeline = PipelineParser.Parse("internal | grep nomatch");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                writer.WriteLine("this will not match");
                return Task.FromResult(CommandResult.Ok());
            });

        // Pipeline execution succeeds (no errors), but exit code is non-zero
        Assert.True(result.Success);
        Assert.Equal(1, result.ExternalExitCode);
        Assert.True(result.HasNonZeroExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_SinglePipe_ExternalStderr_CapturedInResult()
    {
        // ls on non-existent path writes to stderr
        var pipeline = PipelineParser.Parse("internal | ls /nonexistent_path_12345");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) => Task.FromResult(CommandResult.Ok()));

        Assert.True(result.Success); // Pipeline completes
        Assert.True(result.HasErrorOutput);
        Assert.NotEmpty(result.ErrorOutput!);
        Assert.True(result.HasNonZeroExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_SinglePipe_Cancellation_ReturnsCancelledResult()
    {
        using var cts = new CancellationTokenSource();
        var pipeline = PipelineParser.Parse("internal | sleep 10");

        // Cancel shortly after starting
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                writer.WriteLine("output");
                return Task.FromResult(CommandResult.Ok());
            },
            cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_MultiplePipes_DelegatesToShell()
    {
        var pipeline = PipelineParser.Parse("internal | cat | head -2");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                writer.WriteLine("line1");
                writer.WriteLine("line2");
                writer.WriteLine("line3");
                return Task.FromResult(CommandResult.Ok());
            });

        Assert.True(result.Success);
        Assert.Equal("line1\nline2\n", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_MultiplePipes_ThreeCommands_PipesCorrectly()
    {
        var pipeline = PipelineParser.Parse("internal | cat | grep hello | head -1");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                writer.WriteLine("hello world");
                writer.WriteLine("goodbye");
                writer.WriteLine("hello again");
                return Task.FromResult(CommandResult.Ok());
            });

        Assert.True(result.Success);
        Assert.Equal("hello world\n", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_MultiplePipes_JqAndGrep_TransformsJson()
    {
        var pipeline = PipelineParser.Parse("internal | jq -r .name | grep test");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                writer.WriteLine("{\"name\": \"test\"}");
                writer.WriteLine("{\"name\": \"other\"}");
                return Task.FromResult(CommandResult.Ok());
            });

        Assert.True(result.Success);
        Assert.Equal("test\n", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyInternalOutput_ExternalReceivesEmptyStdin()
    {
        var pipeline = PipelineParser.Parse("internal | cat");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) => Task.FromResult(CommandResult.Ok()));

        Assert.True(result.Success);
        Assert.Equal("", result.Output);
        Assert.Equal(0, result.ExternalExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidPipeline_ReturnsError()
    {
        var pipeline = ParsedPipeline.Failure("Test error");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) => Task.FromResult(CommandResult.Ok()));

        Assert.False(result.Success);
        Assert.Equal("Test error", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_NoPipe_ReturnsError()
    {
        // Single segment with no pipe operator
        var pipeline = PipelineParser.Parse("internal");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) => Task.FromResult(CommandResult.Ok()));

        Assert.False(result.Success);
        Assert.Contains("no external commands", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_JqProcessing_TransformsJson()
    {
        // Note: When invoking jq directly (not via shell), don't use shell quoting.
        // The single quotes around .name are shell syntax, not jq syntax.
        var pipeline = PipelineParser.Parse("internal | jq -r .name");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                writer.WriteLine("{\"name\": \"test\", \"value\": 42}");
                return Task.FromResult(CommandResult.Ok());
            });

        Assert.True(result.Success);
        Assert.Equal("test\n", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_GrepFiltering_FiltersLines()
    {
        var pipeline = PipelineParser.Parse("internal | grep hello");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                writer.WriteLine("hello world");
                writer.WriteLine("goodbye world");
                writer.WriteLine("hello again");
                return Task.FromResult(CommandResult.Ok());
            });

        Assert.True(result.Success);
        Assert.Equal("hello world\nhello again\n", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_HeadCommand_TruncatesOutput()
    {
        var pipeline = PipelineParser.Parse("internal | head -2");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                for (int i = 1; i <= 100; i++)
                    writer.WriteLine($"Line {i}");
                return Task.FromResult(CommandResult.Ok());
            });

        Assert.True(result.Success);
        Assert.Equal("Line 1\nLine 2\n", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WcCommand_CountsLines()
    {
        var pipeline = PipelineParser.Parse("internal | wc -l");

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                writer.WriteLine("line 1");
                writer.WriteLine("line 2");
                writer.WriteLine("line 3");
                return Task.FromResult(CommandResult.Ok());
            });

        Assert.True(result.Success);
        Assert.Contains("3", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_InternalCommandReceivesCorrectCommandString()
    {
        var pipeline = PipelineParser.Parse("mycommand arg1 arg2 | cat");
        string? capturedCommand = null;

        await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                capturedCommand = cmd;
                return Task.FromResult(CommandResult.Ok());
            });

        Assert.Equal("mycommand arg1 arg2", capturedCommand);
    }

    [Fact]
    public async Task ExecuteAsync_NullPipeline_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecuteAsync(
                null!,
                (cmd, writer, ct) => Task.FromResult(CommandResult.Ok())));
    }

    [Fact]
    public async Task ExecuteAsync_NullExecutor_ThrowsArgumentNullException()
    {
        var pipeline = PipelineParser.Parse("internal | cat");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecuteAsync(pipeline, null!));
    }

    [Fact]
    public async Task ExecuteAsync_InternalCancellation_ReturnsCancelledResult()
    {
        using var cts = new CancellationTokenSource();
        var pipeline = PipelineParser.Parse("internal | cat");

        var result = await _executor.ExecuteAsync(
            pipeline,
            async (cmd, writer, ct) =>
            {
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
                await Task.Delay(1000, ct); // Should not reach here
                return CommandResult.Ok();
            },
            cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyExternalCommand_ReturnsError()
    {
        // Create a pipeline where external command parses to empty
        // This is an edge case that shouldn't normally happen with PipelineParser
        var segments = new List<PipelineSegment>
        {
            new("internal", 0, IsFirst: true, IsLast: false),
            new("   ", 1, IsFirst: false, IsLast: true) // Whitespace only
        };
        var pipeline = ParsedPipeline.Success(segments);

        var result = await _executor.ExecuteAsync(
            pipeline,
            (cmd, writer, ct) =>
            {
                writer.WriteLine("output");
                return Task.FromResult(CommandResult.Ok());
            });

        Assert.False(result.Success);
        Assert.Contains("empty", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
