using NimbusStation.Cli.Commands;
using NimbusStation.Cli.Output;
using NimbusStation.Core.Aliases;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Errors;
using NimbusStation.Core.Options;
using NimbusStation.Core.Output;
using NimbusStation.Core.Parsing;
using NimbusStation.Core.Session;
using NimbusStation.Core.ShellPiping;
using NimbusStation.Core.Suggestions;
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
    private readonly ISessionStateManager _sessionStateManager;
    private readonly IConfigurationService _configurationService;
    private readonly IOutputWriter _outputWriter;
    private readonly IPipelineExecutor _pipelineExecutor;
    private readonly GlobalOptions _globalOptions;

    private string? _lastSessionId;
    private ErrorCategory _lastErrorCategory = ErrorCategory.None;

    private const string HistoryFileName = ".repl_history";

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplLoop"/> class.
    /// </summary>
    /// <param name="commandRegistry">The registry that provides access to available commands.</param>
    /// <param name="aliasResolver">The resolver used to expand command aliases entered in the REPL.</param>
    /// <param name="sessionService">The session service for persistence operations.</param>
    /// <param name="sessionStateManager">The session state manager for active session tracking.</param>
    /// <param name="configurationService">The configuration service for loading resource aliases.</param>
    /// <param name="pipelineExecutor">The executor for piped commands.</param>
    /// <param name="globalOptions">The global CLI options.</param>
    public ReplLoop(
        CommandRegistry commandRegistry,
        IAliasResolver aliasResolver,
        ISessionService sessionService,
        ISessionStateManager sessionStateManager,
        IConfigurationService configurationService,
        IPipelineExecutor pipelineExecutor,
        GlobalOptions globalOptions)
        : this(commandRegistry, aliasResolver, sessionService, sessionStateManager, configurationService, pipelineExecutor, globalOptions, new ConsoleOutputWriter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplLoop"/> class with a custom output writer.
    /// </summary>
    /// <param name="commandRegistry">The registry that provides access to available commands.</param>
    /// <param name="aliasResolver">The resolver used to expand command aliases entered in the REPL.</param>
    /// <param name="sessionService">The session service for persistence operations.</param>
    /// <param name="sessionStateManager">The session state manager for active session tracking.</param>
    /// <param name="configurationService">The configuration service for loading resource aliases.</param>
    /// <param name="pipelineExecutor">The executor for piped commands.</param>
    /// <param name="globalOptions">The global CLI options.</param>
    /// <param name="outputWriter">The output writer for command output.</param>
    public ReplLoop(
        CommandRegistry commandRegistry,
        IAliasResolver aliasResolver,
        ISessionService sessionService,
        ISessionStateManager sessionStateManager,
        IConfigurationService configurationService,
        IPipelineExecutor pipelineExecutor,
        GlobalOptions globalOptions,
        IOutputWriter outputWriter)
    {
        _commandRegistry = commandRegistry;
        _aliasResolver = aliasResolver;
        _sessionService = sessionService;
        _sessionStateManager = sessionStateManager;
        _configurationService = configurationService;
        _pipelineExecutor = pipelineExecutor;
        _globalOptions = globalOptions;
        _outputWriter = outputWriter;
    }

    /// <summary>
    /// Runs the REPL loop until the user exits.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel and stop the REPL loop.</param>
    /// <returns>The exit code based on the last error category.</returns>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        // Eager-load configuration at startup so alias lookups are instant
        await _configurationService.LoadConfigurationAsync(cancellationToken);

        // Set up tab auto-completion
        ReadLine.AutoCompletionHandler = new CommandAutoCompleteHandler(_commandRegistry);

        // Load history for any active session at startup
        if (_sessionStateManager.CurrentSession is { } initialSession)
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
                var theme = _configurationService.GetTheme();
                AnsiConsole.WriteLine();
                if (!_globalOptions.Quiet)
                    AnsiConsole.MarkupLine($"[{theme.DimColor}]Goodbye![/]");
                SaveHistory();
                _lastErrorCategory = ErrorCategory.Cancelled;
                break;
            }

            if (string.IsNullOrWhiteSpace(input))
                continue;

            // Add to history
            ReadLine.AddHistory(input);

            // Expand any command aliases before processing
            var aliasResult = await _aliasResolver.ExpandAsync(input, _sessionStateManager.CurrentSession, cancellationToken);

            if (!aliasResult.IsSuccess)
            {
                var theme = _configurationService.GetTheme();
                var error = UserFacingError.Configuration(aliasResult.Error!);
                ErrorFormatter.WriteError(error, theme.ErrorColor, _globalOptions.Quiet);
                _lastErrorCategory = error.Category;
                continue;
            }

            var effectiveInput = aliasResult.ExpandedInput;

            if (aliasResult.WasExpanded && !_globalOptions.Quiet)
            {
                var theme = _configurationService.GetTheme();
                AnsiConsole.MarkupLine($"[{theme.DimColor}]>[/] {Markup.Escape(effectiveInput)}");
            }

            // Check for piped commands before normal command processing
            if (PipelineParser.ContainsPipe(effectiveInput))
            {
                await ExecutePipelineAsync(effectiveInput, cancellationToken);
                continue;
            }

            var tokens = InputParser.Parse(effectiveInput);
            var commandName = InputParser.GetCommandName(tokens);

            if (commandName is null)
                continue;

            var command = _commandRegistry.GetCommand(commandName);
            if (command is null)
            {
                var theme = _configurationService.GetTheme();
                var allCommandNames = _commandRegistry.GetAllCommands().Select(c => c.Name);
                var suggestions = CommandSuggester.GetSuggestions(commandName, allCommandNames);
                ErrorFormatter.WriteUnknownCommand(commandName, suggestions, theme.ErrorColor, _globalOptions.Quiet);
                _lastErrorCategory = ErrorCategory.General;
                continue;
            }

            try
            {
                var args = InputParser.GetArguments(tokens);
                var context = new CommandContext(_sessionStateManager, _outputWriter, _globalOptions);
                var result = await command.ExecuteAsync(args, context, cancellationToken);

                // Check if command signals REPL to exit
                if (result.IsExitSignal)
                {
                    var theme = _configurationService.GetTheme();
                    if (!_globalOptions.Quiet && result.Message is not null)
                        AnsiConsole.MarkupLine($"[{theme.DimColor}]{result.Message}[/]");
                    SaveHistory();
                    return ExitCodes.Success;
                }

                if (!result.Success && result.Message is not null)
                {
                    var theme = _configurationService.GetTheme();
                    ErrorFormatter.WriteSimpleError(result.Message, theme.ErrorColor);
                    _lastErrorCategory = ErrorCategory.General;
                }
                else if (result.Success)
                {
                    _lastErrorCategory = ErrorCategory.None;
                }

                // Session may have changed - handle history persistence
                HandleSessionChange();
            }
            catch (OperationCanceledException)
            {
                var theme = _configurationService.GetTheme();
                AnsiConsole.MarkupLine($"[{theme.WarningColor}]Command cancelled.[/]");
                _lastErrorCategory = ErrorCategory.Cancelled;
            }
            catch (Exception ex)
            {
                var theme = _configurationService.GetTheme();
                ErrorFormatter.WriteSimpleError(ex.Message, theme.ErrorColor);
                _lastErrorCategory = ErrorCategory.General;
#if DEBUG
                AnsiConsole.WriteException(ex);
#endif
            }
        }

        return ExitCodes.FromCategory(_lastErrorCategory);
    }

    private async Task ExecutePipelineAsync(string input, CancellationToken cancellationToken)
    {
        var theme = _configurationService.GetTheme();
        var pipeline = PipelineParser.Parse(input);

        if (!pipeline.IsValid)
        {
            ErrorFormatter.WriteSimpleError(pipeline.Error ?? "Invalid pipeline", theme.ErrorColor);
            _lastErrorCategory = ErrorCategory.General;
            return;
        }

        try
        {
            var result = await _pipelineExecutor.ExecuteAsync(
                pipeline,
                ExecuteInternalCommandAsync,
                cancellationToken);

            if (!result.Success)
            {
                ErrorFormatter.WriteSimpleError(result.Error ?? "Pipeline execution failed", theme.ErrorColor);
                _lastErrorCategory = ErrorCategory.General;
                return;
            }

            _lastErrorCategory = ErrorCategory.None;

            // Display stdout
            if (!string.IsNullOrEmpty(result.Output))
                AnsiConsole.Write(result.Output);

            // Display stderr in error color
            if (result.HasErrorOutput)
                AnsiConsole.MarkupLine($"[{theme.ErrorColor}]{Markup.Escape(result.ErrorOutput!)}[/]");

            // Show warning for non-zero exit code
            if (result.HasNonZeroExitCode && !_globalOptions.Quiet)
                AnsiConsole.MarkupLine($"[{theme.WarningColor}]Process exited with code {result.ExternalExitCode}[/]");

            // Session may have changed during internal command - handle history persistence
            HandleSessionChange();
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine($"[{theme.WarningColor}]Pipeline cancelled.[/]");
            _lastErrorCategory = ErrorCategory.Cancelled;
        }
        catch (Exception ex)
        {
            ErrorFormatter.WriteSimpleError(ex.Message, theme.ErrorColor);
            _lastErrorCategory = ErrorCategory.General;
#if DEBUG
            AnsiConsole.WriteException(ex);
#endif
        }
    }

    private async Task<CommandResult> ExecuteInternalCommandAsync(
        string commandString,
        IOutputWriter outputWriter,
        CancellationToken cancellationToken)
    {
        var tokens = InputParser.Parse(commandString);
        var commandName = InputParser.GetCommandName(tokens);

        if (commandName is null)
            return CommandResult.Error("Empty command");

        // Help and exit commands cannot be piped - they are REPL-control commands.
        // Note: These commands are registered in CommandRegistry, but we check explicitly
        // here to enforce this pipe restriction before command lookup occurs.
        if (IsExitCommand(commandName) || IsHelpCommand(commandName))
            return CommandResult.Error($"Cannot pipe '{commandName}' command");

        static bool IsExitCommand(string command) =>
            command.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            command.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
            command.Equals("q", StringComparison.OrdinalIgnoreCase);

        static bool IsHelpCommand(string command) =>
            command.Equals("help", StringComparison.OrdinalIgnoreCase) ||
            command.Equals("?", StringComparison.OrdinalIgnoreCase);

        var command = _commandRegistry.GetCommand(commandName);
        if (command is null)
            return CommandResult.Error($"Unknown command: {commandName}");

        var args = InputParser.GetArguments(tokens);
        var context = new CommandContext(_sessionStateManager, outputWriter, _globalOptions);

        return await command.ExecuteAsync(args, context, cancellationToken);
    }

    private string GetPrompt()
    {
        var theme = _configurationService.GetTheme();

        if (_sessionStateManager.CurrentSession is not { } session)
            return $"[{theme.PromptColor}]ns[/]\u203a ";

        var prompt = $"[{theme.PromptColor}]ns[/]" +
                     $"[{theme.PromptContextColor}][[[/]" +
                     $"[{theme.PromptSessionColor}]{Markup.Escape(session.TicketId)}[/]" +
                     $"[{theme.PromptContextColor}]]][/]";

        // Append active context aliases
        var sessionContext = session.ActiveContext;
        if (sessionContext?.ActiveCosmosAlias is { } cosmosAlias)
        {
            prompt += $"[{theme.PromptContextColor}][[[/]" +
                      $"[{theme.PromptCosmosAliasColor}]{Markup.Escape(cosmosAlias)}[/]" +
                      $"[{theme.PromptContextColor}]]][/]";
        }

        if (sessionContext?.ActiveBlobAlias is { } blobAlias)
        {
            prompt += $"[{theme.PromptContextColor}][[[/]" +
                      $"[{theme.PromptBlobAliasColor}]{Markup.Escape(blobAlias)}[/]" +
                      $"[{theme.PromptContextColor}]]][/]";
        }

        return prompt + "\u203a ";
    }

    private void HandleSessionChange()
    {
        var currentSessionId = _sessionStateManager.CurrentSession?.TicketId;

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
        if (_sessionStateManager.CurrentSession is { } session)
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
}
