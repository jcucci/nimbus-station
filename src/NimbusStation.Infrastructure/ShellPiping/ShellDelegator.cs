using NimbusStation.Core.ShellPiping;

namespace NimbusStation.Infrastructure.ShellPiping;

/// <summary>
/// Executes multiple piped external commands by delegating to the system shell.
/// </summary>
/// <remarks>
/// <para>
/// This class handles multi-pipe scenarios (e.g., <c>jq . | grep foo | head -5</c>) by
/// constructing a pipeline command and executing it via the system shell.
/// </para>
/// <para>
/// On Unix systems, invokes <c>/bin/sh -c "cmd1 | cmd2 | cmd3"</c> with special characters escaped.
/// On Windows, invokes <c>pwsh -Command "cmd1 | cmd2 | cmd3"</c> with double quotes escaped.
/// </para>
/// </remarks>
public sealed class ShellDelegator : IShellDelegator
{
    private readonly IExternalProcessExecutor _processExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellDelegator"/> class.
    /// </summary>
    /// <param name="processExecutor">The executor for running the shell process.</param>
    public ShellDelegator(IExternalProcessExecutor processExecutor) =>
        _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));

    /// <inheritdoc/>
    public async Task<ProcessResult> ExecuteAsync(
        IReadOnlyList<string> externalCommands,
        string? stdinContent = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(externalCommands);

        if (externalCommands.Count == 0)
            return ProcessResult.StartupError("No commands provided for shell delegation");

        if (externalCommands.Count == 1)
            return ProcessResult.StartupError("Shell delegation requires at least 2 commands. Use ExternalProcessExecutor for single commands.");

        var pipelineCommand = ShellEscaper.BuildPipelineCommand(externalCommands);
        var (shell, shellArg) = PlatformHelper.GetDefaultShell();
        var escapedCommand = ShellEscaper.EscapeForShellArgument(pipelineCommand);

        return await _processExecutor.ExecuteAsync(
            command: shell,
            arguments: $"{shellArg} {escapedCommand}",
            stdinContent: stdinContent,
            cancellationToken: cancellationToken);
    }
}
