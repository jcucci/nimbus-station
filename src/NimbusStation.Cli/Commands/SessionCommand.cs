using NimbusStation.Cli.Output;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for managing sessions (start, list, leave, resume, delete, status).
/// </summary>
public sealed class SessionCommand : ICommand
{
    private readonly ISessionService _sessionService;
    private readonly ISessionStateManager _sessionStateManager;
    private readonly IConfigurationService _configurationService;

    private static readonly HashSet<string> _subcommands =
    [
        "start", "list", "ls", "leave", "resume", "delete", "rm", "status"
    ];

    /// <inheritdoc/>
    public string Name => "session";

    /// <inheritdoc/>
    public string Description => "Manage investigation sessions";

    /// <inheritdoc/>
    public string Usage => "session <start|list|leave|resume|delete|status> [session-name]";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands => _subcommands;

    /// <inheritdoc/>
    public CommandHelpMetadata HelpMetadata { get; } = new()
    {
        Subcommands =
        [
            new("start <name>", "Start or resume a session"),
            new("list, ls", "List all sessions"),
            new("leave", "Leave the active session"),
            new("resume <name>", "Resume an existing session"),
            new("delete, rm <name>", "Delete a session and its data"),
            new("status", "Show active session details")
        ],
        Examples =
        [
            new("session start GH-123", "Start investigating issue #123"),
            new("session list", "View all sessions"),
            new("session status", "Show current session details")
        ],
        Notes = "Sessions persist state, history, and artifacts to ~/.nimbus/sessions/."
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionCommand"/> class.
    /// </summary>
    /// <param name="sessionService">The session service for persistence operations.</param>
    /// <param name="sessionStateManager">The session state manager for active session tracking.</param>
    /// <param name="configurationService">The configuration service for theme settings.</param>
    public SessionCommand(
        ISessionService sessionService,
        ISessionStateManager sessionStateManager,
        IConfigurationService configurationService)
    {
        _sessionService = sessionService;
        _sessionStateManager = sessionStateManager;
        _configurationService = configurationService;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0)
            return CommandResult.Error($"Usage: {Usage}");

        var subcommand = args[0].ToLowerInvariant();
        var subArgs = args.Skip(1).ToArray();

        return subcommand switch
        {
            "start" => await HandleStartAsync(subArgs, context, cancellationToken),
            "list" or "ls" => await HandleListAsync(context, cancellationToken),
            "leave" => HandleLeave(context),
            "resume" => await HandleResumeAsync(subArgs, context, cancellationToken),
            "delete" or "rm" => await HandleDeleteAsync(subArgs, context, cancellationToken),
            "status" => HandleStatus(context),
            _ => CommandResult.Error($"Unknown subcommand '{subcommand}'. Usage: {Usage}")
        };
    }

    private async Task<CommandResult> HandleStartAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: session start <session-name>");

        var sessionName = args[0];
        var theme = _configurationService.GetTheme();

        try
        {
            var existed = _sessionService.SessionExists(sessionName);
            var session = await _sessionService.StartSessionAsync(sessionName, cancellationToken);
            _sessionStateManager.ActivateSession(session);

            var action = existed ? "Resumed" : "Started";

            context.Output.WriteLine($"[{theme.SuccessColor}]{action} session[/] [{theme.PromptSessionColor}]{session.TicketId}[/]");
            context.Output.WriteLine($"[{theme.DimColor}]Session directory: {_sessionService.GetSessionDirectory(sessionName)}[/]");

            return CommandResult.Ok();
        }
        catch (InvalidSessionNameException ex)
        {
            return CommandResult.Error(ex.Message);
        }
    }

    private async Task<CommandResult> HandleListAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var sessions = await _sessionService.ListSessionsAsync(cancellationToken);
        var theme = _configurationService.GetTheme();

        if (sessions.Count == 0)
        {
            context.Output.WriteLine($"[{theme.DimColor}]No sessions found. Use 'session start <name>' to create one.[/]");
            return CommandResult.Ok();
        }

        var table = new Table();
        table.AddColumn(new TableColumn("[bold]Session[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Created[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Last Accessed[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Context[/]").LeftAligned());

        foreach (var session in sessions)
        {
            var contextInfo = GetContextSummary(session.ActiveContext, theme);
            table.AddRow(
                $"[{theme.PromptSessionColor}]{session.TicketId}[/]",
                FormatTimestamp(session.CreatedAt),
                FormatTimestamp(session.LastAccessedAt),
                contextInfo);
        }

        context.Output.WriteRenderable(table);
        return CommandResult.Ok(sessions);
    }

    private CommandResult HandleLeave(CommandContext context)
    {
        if (!context.HasActiveSession)
            return CommandResult.Error("No active session to leave.");

        var theme = _configurationService.GetTheme();
        var sessionName = context.CurrentSession!.TicketId;
        _sessionStateManager.DeactivateSession();

        context.Output.WriteLine($"[{theme.WarningColor}]Left session[/] [{theme.PromptSessionColor}]{sessionName}[/]");
        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleResumeAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: session resume <session-name>");

        var sessionName = args[0];
        var theme = _configurationService.GetTheme();

        try
        {
            var session = await _sessionService.ResumeSessionAsync(sessionName, cancellationToken);
            _sessionStateManager.ActivateSession(session);

            context.Output.WriteLine($"[{theme.SuccessColor}]Resumed session[/] [{theme.PromptSessionColor}]{session.TicketId}[/]");
            return CommandResult.Ok();
        }
        catch (SessionNotFoundException ex)
        {
            return CommandResult.Error(ex.Message);
        }
    }

    private async Task<CommandResult> HandleDeleteAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: session delete <session-name>");

        var sessionName = args[0];
        var theme = _configurationService.GetTheme();

        if (!_sessionService.SessionExists(sessionName))
            return CommandResult.Error($"Session '{sessionName}' not found.");

        // Use PromptService for confirmation (respects --yes flag)
        var prompt = new PromptService(context.Options);
        var confirmed = prompt.ConfirmDestructive(
            $"Delete session '{sessionName}' and all its data?",
            theme.WarningColor);

        if (!confirmed)
        {
            context.Output.WriteLine($"[{theme.DimColor}]Deletion cancelled.[/]");
            return CommandResult.Ok();
        }

        try
        {
            await _sessionService.DeleteSessionAsync(sessionName, cancellationToken);
            context.Output.WriteLine($"[{theme.ErrorColor}]Deleted session[/] [{theme.PromptSessionColor}]{sessionName}[/]");
            return CommandResult.Ok();
        }
        catch (SessionNotFoundException ex)
        {
            return CommandResult.Error(ex.Message);
        }
    }

    private CommandResult HandleStatus(CommandContext context)
    {
        var theme = _configurationService.GetTheme();

        if (!context.HasActiveSession)
        {
            context.Output.WriteLine($"[{theme.DimColor}]No active session. Use 'session start <name>' to begin.[/]");
            return CommandResult.Ok();
        }

        var session = context.CurrentSession!;
        var cosmosAlias = session.ActiveContext?.ActiveCosmosAlias ?? $"[{theme.DimColor}]none[/]";
        var blobAlias = session.ActiveContext?.ActiveBlobAlias ?? $"[{theme.DimColor}]none[/]";

        var panel = new Panel(new Rows(
            new Markup($"[bold]Session:[/] [{theme.PromptSessionColor}]{session.TicketId}[/]"),
            new Markup($"[bold]Created:[/] {FormatTimestamp(session.CreatedAt)}"),
            new Markup($"[bold]Last Accessed:[/] {FormatTimestamp(session.LastAccessedAt)}"),
            new Markup($"[bold]Cosmos Alias:[/] {cosmosAlias}"),
            new Markup($"[bold]Blob Alias:[/] {blobAlias}")
        ))
        {
            Header = new PanelHeader($"[bold {theme.SuccessColor}]Session Status[/]"),
            Border = BoxBorder.Rounded
        };

        context.Output.WriteRenderable(panel);
        return CommandResult.Ok();
    }

    private static string GetContextSummary(SessionContext? activeContext, ThemeConfig theme)
    {
        if (activeContext is null)
            return $"[{theme.DimColor}]none[/]";

        var parts = new List<string>();
        if (activeContext.ActiveCosmosAlias is not null)
            parts.Add($"cosmos:{activeContext.ActiveCosmosAlias}");

        if (activeContext.ActiveBlobAlias is not null)
            parts.Add($"blob:{activeContext.ActiveBlobAlias}");

        return parts.Count > 0 ? string.Join(", ", parts) : $"[{theme.DimColor}]none[/]";
    }

    private static string FormatTimestamp(DateTimeOffset timestamp)
    {
        var local = timestamp.ToLocalTime();
        var now = DateTimeOffset.Now;
        var diff = now - local;

        if (diff.TotalMinutes < 1)
            return "just now";

        if (diff.TotalHours < 1)
            return $"{(int)diff.TotalMinutes}m ago";

        if (diff.TotalDays < 1)
            return $"{(int)diff.TotalHours}h ago";

        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";

        return local.ToString("yyyy-MM-dd HH:mm");
    }
}
