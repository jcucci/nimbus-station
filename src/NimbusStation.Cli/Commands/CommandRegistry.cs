using NimbusStation.Core.Commands;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Registry for discovering and dispatching commands.
/// </summary>
public sealed class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a command with the registry.
    /// </summary>
    /// <param name="command">The command to register.</param>
    public void Register(ICommand command) => _commands[command.Name] = command;

    /// <summary>
    /// Registers an alias for a command.
    /// </summary>
    /// <param name="alias">The alias name.</param>
    /// <param name="commandName">The target command name.</param>
    public void RegisterAlias(string alias, string commandName) => _aliases[alias] = commandName;

    /// <summary>
    /// Gets a command by name or alias.
    /// </summary>
    /// <param name="name">The command name or alias.</param>
    /// <returns>The command, or null if not found.</returns>
    public ICommand? GetCommand(string name)
    {
        if (_commands.TryGetValue(name, out var command))
            return command;

        if (_aliases.TryGetValue(name, out var targetName))
            return _commands.GetValueOrDefault(targetName);

        return null;
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
    /// Checks if a command with the specified name or alias exists.
    /// </summary>
    /// <param name="name">The command name or alias.</param>
    /// <returns>True if the command exists, false otherwise.</returns>
    public bool HasCommand(string name) =>
        _commands.ContainsKey(name) || _aliases.ContainsKey(name);

    /// <summary>
    /// Gets all command names including aliases.
    /// </summary>
    /// <returns>An enumerable of all command names and aliases.</returns>
    public IEnumerable<string> GetAllCommandNames() =>
        _commands.Keys.Concat(_aliases.Keys);
}
