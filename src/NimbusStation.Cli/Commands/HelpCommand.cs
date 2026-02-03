using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for displaying help information about available commands.
/// </summary>
public sealed class HelpCommand : ICommand
{
    private readonly Func<CommandRegistry> _registryFactory;
    private readonly IConfigurationService _configurationService;
    private readonly Lazy<IReadOnlySet<string>> _subcommands;

    /// <inheritdoc/>
    public string Name => "help";

    /// <inheritdoc/>
    public string Description => "Display help information for commands";

    /// <inheritdoc/>
    public string Usage => "help [command]";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands => _subcommands.Value;

    /// <inheritdoc/>
    public IReadOnlySet<string> Aliases { get; } = new HashSet<string> { "?" };

    /// <inheritdoc/>
    public bool CanBePiped => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpCommand"/> class.
    /// </summary>
    /// <param name="registryFactory">Factory to get the command registry (deferred to avoid circular DI).</param>
    /// <param name="configurationService">The configuration service.</param>
    public HelpCommand(Func<CommandRegistry> registryFactory, IConfigurationService configurationService)
    {
        _registryFactory = registryFactory;
        _configurationService = configurationService;
        _subcommands = new Lazy<IReadOnlySet<string>>(
            () => _registryFactory().GetAllCommands().Select(c => c.Name).ToHashSet());
    }

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default)
    {
        var theme = _configurationService.GetTheme();

        if (args.Length > 0)
        {
            var commandName = args[0];
            var command = _registryFactory().GetCommand(commandName);

            if (command is null)
            {
                context.Output.WriteLine($"[{theme.ErrorColor}]Unknown command:[/] {Markup.Escape(commandName)}");
                return Task.FromResult(CommandResult.Ok());
            }

            context.Output.WriteLine($"[bold]{command.Name}[/] - {command.Description}");
            context.Output.WriteLine($"[{theme.DimColor}]Usage:[/] {command.Usage}");
            return Task.FromResult(CommandResult.Ok());
        }

        context.Output.WriteLine("[bold]Available Commands:[/]");
        context.Output.WriteLine("");

        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders();

        table.AddColumn("Command");
        table.AddColumn("Description");

        foreach (var command in _registryFactory().GetAllCommands().OrderBy(c => c.Name))
            table.AddRow($"[{theme.TableHeaderColor}]{command.Name}[/]", command.Description);

        table.AddRow($"[{theme.TableHeaderColor}]exit[/]", "Exit the REPL");

        context.Output.WriteRenderable(table);
        context.Output.WriteLine("");
        context.Output.WriteLine($"[{theme.DimColor}]Type 'help <command>' for more information about a specific command.[/]");

        return Task.FromResult(CommandResult.Ok());
    }
}
