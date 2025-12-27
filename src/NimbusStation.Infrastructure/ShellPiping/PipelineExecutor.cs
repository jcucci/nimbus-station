using NimbusStation.Core.Commands;
using NimbusStation.Core.Output;
using NimbusStation.Core.Parsing;
using NimbusStation.Core.ShellPiping;
using NimbusStation.Infrastructure.Output;

namespace NimbusStation.Infrastructure.ShellPiping;

/// <summary>
/// Executes pipelines by running internal commands and piping output to external processes.
/// Supports single-pipe (direct process) and multi-pipe (shell delegation) scenarios.
/// </summary>
public sealed class PipelineExecutor : IPipelineExecutor
{
    private readonly IExternalProcessExecutor _externalProcessExecutor;
    private readonly IShellDelegator _shellDelegator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineExecutor"/> class.
    /// </summary>
    /// <param name="externalProcessExecutor">The executor for single external processes.</param>
    /// <param name="shellDelegator">The delegator for multi-pipe shell execution.</param>
    public PipelineExecutor(
        IExternalProcessExecutor externalProcessExecutor,
        IShellDelegator shellDelegator)
    {
        _externalProcessExecutor = externalProcessExecutor ?? throw new ArgumentNullException(nameof(externalProcessExecutor));
        _shellDelegator = shellDelegator ?? throw new ArgumentNullException(nameof(shellDelegator));
    }

    /// <inheritdoc/>
    public async Task<PipelineExecutionResult> ExecuteAsync(
        ParsedPipeline pipeline,
        Func<string, IOutputWriter, CancellationToken, Task<CommandResult>> internalCommandExecutor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(internalCommandExecutor);

        if (!pipeline.IsValid)
            return PipelineExecutionResult.Failed(pipeline.Error ?? "Invalid pipeline");

        if (!pipeline.HasExternalCommands)
            return PipelineExecutionResult.Failed("Pipeline has no external commands");

        var internalSegment = pipeline.InternalCommand!;
        var externalCommands = pipeline.ExternalCommands.ToList();

        var captureWriter = new CaptureOutputWriter();
        CommandResult internalResult;

        try
        {
            internalResult = await internalCommandExecutor(internalSegment.Content, captureWriter, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return PipelineExecutionResult.Cancelled();
        }

        if (!internalResult.Success)
        {
            return PipelineExecutionResult.InternalCommandFailed(
                internalResult.Message ?? "Internal command failed");
        }

        var capturedOutput = captureWriter.GetOutput();

        if (externalCommands.Count == 1)
            return await ExecuteSinglePipeAsync(externalCommands[0], capturedOutput, cancellationToken);

        return await ExecuteMultiPipeAsync(externalCommands, capturedOutput, cancellationToken);
    }

    private async Task<PipelineExecutionResult> ExecuteSinglePipeAsync(
        PipelineSegment externalSegment,
        string capturedOutput,
        CancellationToken cancellationToken)
    {
        var (command, arguments) = CommandParser.Parse(externalSegment.Content);
        if (string.IsNullOrEmpty(command))
            return PipelineExecutionResult.Failed("External command is empty");

        ProcessResult externalResult;
        try
        {
            externalResult = await _externalProcessExecutor.ExecuteAsync(
                command: command,
                arguments: arguments,
                stdinContent: capturedOutput,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return PipelineExecutionResult.Cancelled(partialOutput: capturedOutput);
        }

        return ToExecutionResult(externalResult, command, isMultiPipe: false);
    }

    private async Task<PipelineExecutionResult> ExecuteMultiPipeAsync(
        List<PipelineSegment> externalCommands,
        string capturedOutput,
        CancellationToken cancellationToken)
    {
        var commandStrings = externalCommands.Select(s => s.Content).ToList();

        ProcessResult result;
        try
        {
            result = await _shellDelegator.ExecuteAsync(
                externalCommands: commandStrings,
                stdinContent: capturedOutput,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return PipelineExecutionResult.Cancelled(partialOutput: capturedOutput);
        }

        var pipelineDescription = string.Join(" | ", commandStrings);
        return ToExecutionResult(result, pipelineDescription, isMultiPipe: true);
    }

    private static PipelineExecutionResult ToExecutionResult(ProcessResult result, string commandDescription, bool isMultiPipe = false)
    {
        if (result.Error is not null)
        {
            var hint = isMultiPipe
                ? "Check that all commands in the pipeline are installed and in your PATH."
                : $"Is '{commandDescription}' installed and in your PATH?";
            return PipelineExecutionResult.Failed(
                $"Failed to execute '{commandDescription}': {result.Error}. {hint}");
        }

        if (result.WasKilled)
        {
            return PipelineExecutionResult.Cancelled(
                partialOutput: result.StandardOutput,
                partialErrorOutput: result.StandardError);
        }

        return PipelineExecutionResult.Succeeded(
            output: result.StandardOutput,
            errorOutput: result.StandardError,
            exitCode: result.ExitCode);
    }
}
