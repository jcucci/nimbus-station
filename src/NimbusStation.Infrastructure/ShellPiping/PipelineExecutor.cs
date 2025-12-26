using NimbusStation.Core.Commands;
using NimbusStation.Core.Output;
using NimbusStation.Core.Parsing;
using NimbusStation.Core.ShellPiping;
using NimbusStation.Infrastructure.Output;

namespace NimbusStation.Infrastructure.ShellPiping;

/// <summary>
/// Executes pipelines by running internal commands and piping output to external processes.
/// Currently supports single-pipe scenarios (internal | external).
/// </summary>
public sealed class PipelineExecutor : IPipelineExecutor
{
    private readonly IExternalProcessExecutor _externalProcessExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineExecutor"/> class.
    /// </summary>
    /// <param name="externalProcessExecutor">The executor for external processes.</param>
    public PipelineExecutor(IExternalProcessExecutor externalProcessExecutor) =>
        _externalProcessExecutor = externalProcessExecutor ?? throw new ArgumentNullException(nameof(externalProcessExecutor));

    /// <inheritdoc/>
    public async Task<PipelineExecutionResult> ExecuteAsync(
        ParsedPipeline pipeline,
        Func<string, IOutputWriter, CancellationToken, Task<CommandResult>> internalCommandExecutor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(internalCommandExecutor);

        // Validate pipeline
        if (!pipeline.IsValid)
            return PipelineExecutionResult.Failed(pipeline.Error ?? "Invalid pipeline");

        if (!pipeline.HasExternalCommands)
            return PipelineExecutionResult.Failed("Pipeline has no external commands");

        // MVP: Only support single external pipe
        var externalCommands = pipeline.ExternalCommands.ToList();
        if (externalCommands.Count > 1)
            return PipelineExecutionResult.Failed("Multiple external pipes not yet supported. Use a single pipe (e.g., 'command | jq .')");

        var internalSegment = pipeline.InternalCommand!;
        var externalSegment = externalCommands[0];

        // Execute internal command with capture
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

        // Get captured output to pipe to external process
        var capturedOutput = captureWriter.GetOutput();

        // Parse external command
        var (command, arguments) = CommandParser.Parse(externalSegment.Content);
        if (string.IsNullOrEmpty(command))
            return PipelineExecutionResult.Failed("External command is empty");

        // Execute external process
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

        // Handle startup errors (command not found, etc.)
        if (externalResult.Error is not null)
        {
            return PipelineExecutionResult.Failed(
                $"Failed to execute '{command}': {externalResult.Error}. Is '{command}' installed and in your PATH?");
        }

        // Handle killed process
        if (externalResult.WasKilled)
        {
            return PipelineExecutionResult.Cancelled(
                partialOutput: externalResult.StandardOutput,
                partialErrorOutput: externalResult.StandardError);
        }

        // Return result (success or non-zero exit)
        return PipelineExecutionResult.Succeeded(
            output: externalResult.StandardOutput,
            errorOutput: externalResult.StandardError,
            exitCode: externalResult.ExitCode);
    }
}
