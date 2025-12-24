using NimbusStation.Cli.Commands;
using NimbusStation.Cli.Output;
using NimbusStation.Core.Aliases;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Output;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Repl;

/// <summary>
/// The main Read-Eval-Print Loop for Nimbus Station.
/// </summary>
public sealed class ReplLoop
{
    private readonly CommandRegistry _commandRegistry;
    private readonly IAliasResolver _aliasResolver;
    private readonly ISessionService _sessionService;
    private readonly IConfigurationService _configurationService;
    private readonly IOutputWriter _outputWriter;

    private string? _lastSessionId;

    private const string HistoryFileName = ".repl_history";

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplLoop"/> class.
    /// </summary>
    /// <param name="commandRegistry">The registry that provides access to available commands.</param>
    /// <param name="aliasResolver">The resolver used to expand command aliases entered in the REPL.</param>
    /// <param name="sessionService">The session service for managing session state.</param>
    /// <param name="configurationService">The configuration service for loading resource aliases.</param>
    public ReplLoop(
        CommandRegistry commandRegistry,
        IAliasResolver aliasResolver,
        ISessionService sessionService,
        IConfigurationService configurationService)
        : this(commandRegistry, aliasResolver, sessionService, configurationService, new ConsoleOutputWriter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplLoop"/> class with a custom output writer.
    /// </summary>
    /// <param name="commandRegistry">The registry that provides access to available commands.</param>
    /// <param name="aliasResolver">The resolver used to expand command aliases entered in the REPL.</param>
    /// <param name="sessionService">The session service for managing session state.</param>
    /// <param name="configurationService">The configuration service for loading resource aliases.</param>
    /// <param name="outputWriter">The output writer for command output.</param>
    public ReplLoop(
        CommandRegistry commandRegistry,
        IAliasResolver aliasResolver,
        ISessionService sessionService,
        IConfigurationService configurationService,
        IOutputWriter outputWriter)
    {
        _commandRegistry = commandRegistry;
        _aliasResolver = aliasResolver;
        _sessionService = sessionService;
        _configurationService = configurationService;
        _outputWriter = outputWriter;
    }

    /// <summary>
    /// Runs the REPL loop until the user exits.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel and stop the REPL loop.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Eager-load configuration at startup so alias lookups are instant
        await _configurationService.LoadConfigurationAsync(cancellationToken);

        // Set up tab auto-completion
        ReadLine.AutoCompletionHandler = new CommandAutoCompleteHandler(_commandRegistry);

        // Load history for any active session at startup
        if (_sessionService.CurrentSession is { } initialSession)
        {
            LoadHistoryForSession(initialSession.TicketId);
            _lastSessionId = initialSession.TicketId;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            // Render the styled prompt with Spectre.Console, then use ReadLine for input with history
            AnsiConsole.Markup(GetPrompt());
            var input = ReadLine.Read();

            // Handle Ctrl+C (ReadLine returns null)
            if (input is null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Goodbye![/]");
                SaveHistory();
                break;
            }

            if (string.IsNullOrWhiteSpace(input))
                continue;

            // Add to history
            ReadLine.AddHistory(input);

            // Expand any command aliases before processing
            var aliasResult = await _aliasResolver.ExpandAsync(input, _sessionService.CurrentSession, cancellationToken);

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
                SaveHistory();
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
                var context = new CommandContext(_sessionService, _outputWriter);
                var result = await command.ExecuteAsync(args, context, cancellationToken);

                if (!result.Success && result.Message is not null)
                    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(result.Message)}");

                // Session may have changed - handle history persistence
                HandleSessionChange();
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

    private string GetPrompt()
    {
        if (_sessionService.CurrentSession is not { } session)
            return "[green]ns[/]\u203a ";

        var prompt = $"[green]ns[/][[[cyan]{Markup.Escape(session.TicketId)}[/]]]";

        // Append active context aliases
        var sessionContext = session.ActiveContext;
        if (sessionContext?.ActiveCosmosAlias is { } cosmosAlias)
            prompt += $"[[[orange1]{Markup.Escape(cosmosAlias)}[/]]]";

        if (sessionContext?.ActiveBlobAlias is { } blobAlias)
            prompt += $"[[[magenta]{Markup.Escape(blobAlias)}[/]]]";

        return prompt + "\u203a ";
    }

    private static bool IsExitCommand(string command) =>
        command.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("q", StringComparison.OrdinalIgnoreCase);

    private static bool IsHelpCommand(string command) =>
        command.Equals("help", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("?", StringComparison.OrdinalIgnoreCase);

    private void HandleSessionChange()
    {
        var currentSessionId = _sessionService.CurrentSession?.TicketId;

        // If session changed, save history to old session and load from new session
        if (currentSessionId != _lastSessionId)
        {
            if (_lastSessionId is not null)
                SaveHistoryForSession(_lastSessionId);

            if (currentSessionId is not null)
                LoadHistoryForSession(currentSessionId);

            _lastSessionId = currentSessionId;
        }
    }

    private void SaveHistory()
    {
        if (_sessionService.CurrentSession is { } session)
            SaveHistoryForSession(session.TicketId);
    }

    private void SaveHistoryForSession(string sessionId)
    {
        try
        {
            var sessionDir = _sessionService.GetSessionDirectory(sessionId);
            var historyPath = Path.Combine(sessionDir, HistoryFileName);
            var history = ReadLine.GetHistory();

            if (history.Count > 0)
                File.WriteAllLines(historyPath, history);
        }
        catch
        {
            // Silently ignore history save failures - not critical
        }
    }

    private void LoadHistoryForSession(string sessionId)
    {
        try
        {
            var sessionDir = _sessionService.GetSessionDirectory(sessionId);
            var historyPath = Path.Combine(sessionDir, HistoryFileName);

            if (File.Exists(historyPath))
            {
                // Clear existing history and load from file
                ReadLine.ClearHistory();
                var lines = File.ReadAllLines(historyPath);
                foreach (var line in lines)
                    ReadLine.AddHistory(line);
            }
        }
        catch
        {
            // Silently ignore history load failures - not critical
        }
    }

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
