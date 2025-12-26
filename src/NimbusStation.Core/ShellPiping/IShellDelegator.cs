namespace NimbusStation.Core.ShellPiping;

/// <summary>
/// Executes multiple piped external commands by delegating to the system shell.
/// </summary>
/// <remarks>
/// <para>
/// Used when a pipeline contains two or more external commands (e.g., <c>internal | jq | head</c>).
/// The shell handles inter-process piping, buffering, and coordination.
/// </para>
/// <para>
/// On Unix systems, delegates to <c>/bin/sh -c</c> with a safely escaped pipeline command.
/// On Windows, delegates to <c>pwsh -Command</c> with equivalent escaping.
/// Special characters are escaped to prevent command injection.
/// </para>
/// </remarks>
public interface IShellDelegator
{
    /// <summary>
    /// Executes a chain of external commands via the system shell.
    /// </summary>
    /// <param name="externalCommands">The external commands to pipe together (in order).</param>
    /// <param name="stdinContent">Content to pipe to the first command's stdin.</param>
    /// <param name="cancellationToken">Cancellation token to cancel execution.</param>
    /// <returns>The result of the shell execution.</returns>
    Task<ProcessResult> ExecuteAsync(
        IReadOnlyList<string> externalCommands,
        string? stdinContent = null,
        CancellationToken cancellationToken = default);
}
