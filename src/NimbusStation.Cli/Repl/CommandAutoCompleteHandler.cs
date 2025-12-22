using NimbusStation.Cli.Commands;

namespace NimbusStation.Cli.Repl;

/// <summary>
/// Provides tab auto-completion for REPL commands.
/// </summary>
public sealed class CommandAutoCompleteHandler : IAutoCompleteHandler
{
    private readonly CommandRegistry _commandRegistry;

    private static readonly HashSet<string> _builtInCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "help", "exit", "quit", "q", "?"
    };

    /// <inheritdoc />
    public char[] Separators { get; set; } = [' '];

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandAutoCompleteHandler"/> class.
    /// </summary>
    /// <param name="commandRegistry">The command registry containing available commands.</param>
    public CommandAutoCompleteHandler(CommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry;
    }

    /// <inheritdoc />
    public string[] GetSuggestions(string text, int index)
    {
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // If we're at the start of input or completing the first token (command name)
        if (tokens.Length == 0 || (tokens.Length == 1 && !text.EndsWith(' ')))
        {
            var prefix = tokens.Length > 0 ? tokens[0] : "";
            return GetAllCommandNames()
                .Where(cmd => cmd.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(cmd => cmd)
                .ToArray();
        }

        // For subcommand completion (e.g., "session st" -> "session start")
        var commandName = tokens[0];
        var command = _commandRegistry.GetCommand(commandName);

        if (command is null || command.Subcommands.Count == 0)
            return [];

        // Get the partial subcommand the user is typing
        var subcommandPrefix = text.EndsWith(' ') ? "" : tokens[^1];

        return command.Subcommands
            .Where(sub => sub.StartsWith(subcommandPrefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(sub => sub)
            .ToArray();
    }

    private IEnumerable<string> GetAllCommandNames()
    {
        var registeredCommands = _commandRegistry.GetAllCommands().Select(c => c.Name);
        return registeredCommands.Concat(_builtInCommands).Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
