using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for displaying help information about available commands.
/// </summary>
public sealed class HelpCommand : ICommand
{
    private readonly CommandRegistry _registry;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpCommand"/> class.
    /// </summary>
    /// <param name="registry">The command registry.</param>
    /// <param name="configurationService">The configuration service.</param>
    public HelpCommand(CommandRegistry registry, IConfigurationService configurationService)
    {
        _registry = registry;
        _configurationService = configurationService;
        _subcommands = new Lazy<IReadOnlySet<string>>(
            () => _registry.GetAllCommands().Select(c => c.Name).ToHashSet());
    }

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default)
    {
        var theme = _configurationService.GetTheme();

        if (args.Length > 0)
        {
            var commandName = args[0];
            var command = _registry.GetCommand(commandName);

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

        foreach (var command in _registry.GetAllCommands().OrderBy(c => c.Name))
            table.AddRow($"[{theme.TableHeaderColor}]{command.Name}[/]", command.Description);

        table.AddRow($"[{theme.TableHeaderColor}]exit[/]", "Exit the REPL");

        context.Output.WriteRenderable(table);
        context.Output.WriteLine("");
        context.Output.WriteLine($"[{theme.DimColor}]Type 'help <command>' for more information about a specific command.[/]");

        return Task.FromResult(CommandResult.Ok());
    }
}
