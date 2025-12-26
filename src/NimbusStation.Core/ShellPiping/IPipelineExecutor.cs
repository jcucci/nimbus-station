using NimbusStation.Core.Commands;
using NimbusStation.Core.Output;
using NimbusStation.Core.Parsing;

namespace NimbusStation.Core.ShellPiping;

/// <summary>
/// Orchestrates pipeline execution by running internal commands and piping output to external processes.
/// </summary>
public interface IPipelineExecutor
{
    /// <summary>
    /// Executes a parsed pipeline, running the internal command and piping its output to external processes.
    /// </summary>
    /// <param name="pipeline">The parsed pipeline containing internal and external command segments.</param>
    /// <param name="internalCommandExecutor">
    /// A delegate that executes the internal command. Receives the command string, an output writer for
    /// capturing output, and a cancellation token. Returns the command result.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the entire pipeline.</param>
    /// <returns>The result of the pipeline execution.</returns>
    Task<PipelineExecutionResult> ExecuteAsync(
        ParsedPipeline pipeline,
        Func<string, IOutputWriter, CancellationToken, Task<CommandResult>> internalCommandExecutor,
        CancellationToken cancellationToken = default);
}
