namespace NimbusStation.Core.ShellPiping;

/// <summary>
/// Executes external processes with stdin streaming and output capture.
/// </summary>
public interface IExternalProcessExecutor
{
    /// <summary>
    /// Executes an external command, optionally piping content to stdin.
    /// </summary>
    /// <param name="command">The command/executable to run.</param>
    /// <param name="arguments">Command arguments (optional).</param>
    /// <param name="stdinContent">Content to write to the process's stdin (optional).</param>
    /// <param name="cancellationToken">Cancellation token to kill the process.</param>
    /// <returns>The process result with captured output.</returns>
    Task<ProcessResult> ExecuteAsync(
        string command,
        string? arguments = null,
        string? stdinContent = null,
        CancellationToken cancellationToken = default);
}
