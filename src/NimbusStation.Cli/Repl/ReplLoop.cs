using NimbusStation.Cli.Commands;
using NimbusStation.Core.Aliases;
using NimbusStation.Core.Commands;
using Spectre.Console;

namespace NimbusStation.Cli.Repl;

/// <summary>
/// The main Read-Eval-Print Loop for Nimbus Station.
/// </summary>
public sealed class ReplLoop
{
    private readonly CommandRegistry _commandRegistry;
    private readonly IAliasResolver _aliasResolver;
    private readonly CommandContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplLoop"/> class.
    /// </summary>
    /// <param name="commandRegistry">The registry that provides access to available commands.</param>
    /// <param name="aliasResolver">The resolver used to expand command aliases entered in the REPL.</param>
    public ReplLoop(CommandRegistry commandRegistry, IAliasResolver aliasResolver)
    {
        _commandRegistry = commandRegistry;
        _aliasResolver = aliasResolver;
        _context = new CommandContext();
    }

    /// <summary>
    /// Runs the REPL loop until the user exits.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel and stop the REPL loop.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var input = AnsiConsole.Prompt(
                new TextPrompt<string>(GetPrompt())
                    .AllowEmpty());

            if (string.IsNullOrWhiteSpace(input))
                continue;

            // Expand any command aliases before processing
            var aliasResult = await _aliasResolver.ExpandAsync(input, _context.CurrentSession, cancellationToken);

            if (!aliasResult.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(aliasResult.Error!)}");
                continue;
            }

            var effectiveInput = aliasResult.ExpandedInput;

            if (aliasResult.WasExpanded)
                AnsiConsole.MarkupLine($"[dim]>[/] {Markup.Escape(effectiveInput)}");

            var tokens = InputParser.Parse(effectiveInput);
            var commandName = InputParser.GetCommandName(tokens);

            if (commandName is null)
                continue;

            if (IsExitCommand(commandName))
            {
                AnsiConsole.MarkupLine("[dim]Goodbye![/]");
                break;
            }

            if (IsHelpCommand(commandName))
            {
                ShowHelp(tokens);
                continue;
            }

            var command = _commandRegistry.GetCommand(commandName);
            if (command is null)
            {
                AnsiConsole.MarkupLine($"[red]Unknown command:[/] {Markup.Escape(commandName)}");
                AnsiConsole.MarkupLine("[dim]Type 'help' for available commands.[/]");
                continue;
            }

            try
            {
                var args = InputParser.GetArguments(tokens);
                var result = await command.ExecuteAsync(args, _context, cancellationToken);

                if (!result.Success && result.Message is not null)
                    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(result.Message)}");
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]Command cancelled.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
#if DEBUG
                AnsiConsole.WriteException(ex);
#endif
            }
        }
    }

    private string GetPrompt() =>
        _context.CurrentSession is { } session
            ? $"[green]ns[/][[[cyan]{Markup.Escape(session.TicketId)}[/]]]\u203a "
            : "[green]ns[/]\u203a ";

    private static bool IsExitCommand(string command) =>
        command.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("q", StringComparison.OrdinalIgnoreCase);

    private static bool IsHelpCommand(string command) =>
        command.Equals("help", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("?", StringComparison.OrdinalIgnoreCase);

    private void ShowHelp(string[] tokens)
    {
        var args = InputParser.GetArguments(tokens);

        if (args.Length > 0)
        {
            var commandName = args[0];
            var command = _commandRegistry.GetCommand(commandName);

            if (command is null)
            {
                AnsiConsole.MarkupLine($"[red]Unknown command:[/] {Markup.Escape(commandName)}");
                return;
            }

            AnsiConsole.MarkupLine($"[bold]{command.Name}[/] - {command.Description}");
            AnsiConsole.MarkupLine($"[dim]Usage:[/] {command.Usage}");
            return;
        }

        AnsiConsole.MarkupLine("[bold]Available Commands:[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders();

        table.AddColumn("Command");
        table.AddColumn("Description");

        foreach (var command in _commandRegistry.GetAllCommands().OrderBy(c => c.Name))
            table.AddRow($"[cyan]{command.Name}[/]", command.Description);

        table.AddRow("[cyan]help[/]", "Show this help message");
        table.AddRow("[cyan]exit[/]", "Exit the REPL");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Type 'help <command>' for more information about a specific command.[/]");
    }
}
