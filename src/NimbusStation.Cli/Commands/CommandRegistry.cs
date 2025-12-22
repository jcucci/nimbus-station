using NimbusStation.Core.Commands;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Registry for discovering and dispatching commands.
/// </summary>
public sealed class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a command with the registry.
    /// </summary>
    /// <param name="command">The command to register.</param>
    public void Register(ICommand command)
    {
        _commands[command.Name] = command;
    }

    /// <summary>
    /// Gets a command by name.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <returns>The command, or null if not found.</returns>
    public ICommand? GetCommand(string name)
    {
        return _commands.GetValueOrDefault(name);
    }

    /// <summary>
    /// Gets all registered commands.
    /// </summary>
    /// <returns>An enumerable of all commands.</returns>
    public IEnumerable<ICommand> GetAllCommands()
    {
        return _commands.Values;
    }

    /// <summary>
    /// Checks if a command with the specified name exists.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <returns>True if the command exists, false otherwise.</returns>
    public bool HasCommand(string name)
    {
        return _commands.ContainsKey(name);
    }
}
