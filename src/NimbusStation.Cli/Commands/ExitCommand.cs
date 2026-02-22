using NimbusStation.Core.Commands;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for exiting the REPL.
/// </summary>
public sealed class ExitCommand : ICommand
{
    /// <inheritdoc/>
    public string Name => "exit";

    /// <inheritdoc/>
    public string Description => "Exit the REPL";

    /// <inheritdoc/>
    public string Usage => "exit";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands { get; } = new HashSet<string>();

    /// <inheritdoc/>
    public IReadOnlySet<string> Aliases { get; } = new HashSet<string> { "quit", "q" };

    /// <inheritdoc/>
    public bool CanBePiped => false;

    /// <inheritdoc/>
    public CommandHelpMetadata HelpMetadata { get; } = new()
    {
        Examples =
        [
            new("exit", "Exit the REPL")
        ],
        Notes = "Aliases: quit, q"
    };

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(CommandResult.Exit());
}
