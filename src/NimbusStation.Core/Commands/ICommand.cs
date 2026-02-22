namespace NimbusStation.Core.Commands;

/// <summary>
/// Represents a command that can be executed in the REPL.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets the primary name of the command (e.g., "session").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a brief description of what the command does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the usage pattern for the command (e.g., "session <start|list|leave|resume|delete> [args]").
    /// </summary>
    string Usage { get; }

    /// <summary>
    /// Gets the available subcommands for this command, used for tab auto-completion.
    /// Returns an empty set if the command has no subcommands.
    /// </summary>
    IReadOnlySet<string> Subcommands => new HashSet<string>();

    /// <summary>
    /// Gets the command aliases (alternative names for the command).
    /// Returns an empty set if the command has no aliases.
    /// </summary>
    IReadOnlySet<string> Aliases => new HashSet<string>();

    /// <summary>
    /// Gets whether this command's output can be piped to external processes.
    /// REPL-control commands (exit, help) should return false.
    /// </summary>
    bool CanBePiped => true;

    /// <summary>
    /// Gets structured help metadata for rich inline help rendering.
    /// Returns null when the command has no structured help, falling back to basic name/description/usage.
    /// </summary>
    CommandHelpMetadata? HelpMetadata => null;

    /// <summary>
    /// Executes the command with the given arguments.
    /// </summary>
    /// <param name="args">The arguments passed to the command (excluding the command name itself).</param>
    /// <param name="context">The current command context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the command execution.</returns>
    Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default);
}
