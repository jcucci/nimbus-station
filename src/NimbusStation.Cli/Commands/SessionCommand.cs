using NimbusStation.Core.Commands;
using NimbusStation.Core.Session;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for managing sessions (start, list, leave, resume, delete, status).
/// </summary>
public sealed class SessionCommand : ICommand
{
    private readonly ISessionService _sessionService;

    /// <inheritdoc/>
    public string Name => "session";

    /// <inheritdoc/>
    public string Description => "Manage investigation sessions";

    /// <inheritdoc/>
    public string Usage => "session <start|list|leave|resume|delete|status> [session-name]";

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionCommand"/> class.
    /// </summary>
    /// <param name="sessionService">The session service.</param>
    public SessionCommand(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0)
        {
            return CommandResult.Error($"Usage: {Usage}");
        }

        var subcommand = args[0].ToLowerInvariant();
        var subArgs = args.Skip(1).ToArray();

        return subcommand switch
        {
            "start" => await HandleStartAsync(subArgs, context, cancellationToken),
            "list" or "ls" => await HandleListAsync(cancellationToken),
            "leave" => HandleLeave(context),
            "resume" => await HandleResumeAsync(subArgs, context, cancellationToken),
            "delete" or "rm" => await HandleDeleteAsync(subArgs, cancellationToken),
            "status" => HandleStatus(context),
            _ => CommandResult.Error($"Unknown subcommand '{subcommand}'. Usage: {Usage}")
        };
    }

    private async Task<CommandResult> HandleStartAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            return CommandResult.Error("Usage: session start <session-name>");
        }

        var sessionName = args[0];

        try
        {
            // Check if session exists before starting to determine the action message
            var existed = _sessionService.SessionExists(sessionName);
            var session = await _sessionService.StartSessionAsync(sessionName, cancellationToken);
            context.CurrentSession = session;

            var action = existed ? "Resumed" : "Started";

            AnsiConsole.MarkupLine($"[green]{action} session[/] [cyan]{session.TicketId}[/]");
            AnsiConsole.MarkupLine($"[dim]Session directory: {_sessionService.GetSessionDirectory(sessionName)}[/]");

            return CommandResult.Ok(session);
        }
        catch (InvalidSessionNameException ex)
        {
            return CommandResult.Error(ex.Message);
        }
    }

    private async Task<CommandResult> HandleListAsync(CancellationToken cancellationToken)
    {
        var sessions = await _sessionService.ListSessionsAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No sessions found. Use 'session start <name>' to create one.[/]");
            return CommandResult.Ok();
        }

        var table = new Table();
        table.AddColumn(new TableColumn("[bold]Session[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Created[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Last Accessed[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Context[/]").LeftAligned());

        foreach (var session in sessions)
        {
            var contextInfo = GetContextSummary(session.ActiveContext);
            table.AddRow(
                $"[cyan]{session.TicketId}[/]",
                FormatTimestamp(session.CreatedAt),
                FormatTimestamp(session.LastAccessedAt),
                contextInfo);
        }

        AnsiConsole.Write(table);
        return CommandResult.Ok(sessions);
    }

    private static CommandResult HandleLeave(CommandContext context)
    {
        if (context.CurrentSession is null)
        {
            return CommandResult.Error("No active session to leave.");
        }

        var sessionName = context.CurrentSession.TicketId;
        context.CurrentSession = null;

        AnsiConsole.MarkupLine($"[yellow]Left session[/] [cyan]{sessionName}[/]");
        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleResumeAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            return CommandResult.Error("Usage: session resume <session-name>");
        }

        var sessionName = args[0];

        try
        {
            var session = await _sessionService.ResumeSessionAsync(sessionName, cancellationToken);
            context.CurrentSession = session;

            AnsiConsole.MarkupLine($"[green]Resumed session[/] [cyan]{session.TicketId}[/]");
            return CommandResult.Ok(session);
        }
        catch (SessionNotFoundException ex)
        {
            return CommandResult.Error(ex.Message);
        }
    }

    private async Task<CommandResult> HandleDeleteAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            return CommandResult.Error("Usage: session delete <session-name>");
        }

        var sessionName = args[0];

        if (!_sessionService.SessionExists(sessionName))
        {
            return CommandResult.Error($"Session '{sessionName}' not found.");
        }

        // Confirmation prompt
        var confirmed = AnsiConsole.Confirm(
            $"[yellow]Delete session '[cyan]{sessionName}[/]' and all its data?[/]",
            defaultValue: false);

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[dim]Deletion cancelled.[/]");
            return CommandResult.Ok();
        }

        try
        {
            await _sessionService.DeleteSessionAsync(sessionName, cancellationToken);
            AnsiConsole.MarkupLine($"[red]Deleted session[/] [cyan]{sessionName}[/]");
            return CommandResult.Ok();
        }
        catch (SessionNotFoundException ex)
        {
            return CommandResult.Error(ex.Message);
        }
    }

    private static CommandResult HandleStatus(CommandContext context)
    {
        if (context.CurrentSession is null)
        {
            AnsiConsole.MarkupLine("[dim]No active session. Use 'session start <name>' to begin.[/]");
            return CommandResult.Ok();
        }

        var session = context.CurrentSession;

        var panel = new Panel(new Rows(
            new Markup($"[bold]Session:[/] [cyan]{session.TicketId}[/]"),
            new Markup($"[bold]Created:[/] {FormatTimestamp(session.CreatedAt)}"),
            new Markup($"[bold]Last Accessed:[/] {FormatTimestamp(session.LastAccessedAt)}"),
            new Markup($"[bold]Cosmos Alias:[/] {session.ActiveContext?.ActiveCosmosAlias ?? "[dim]none[/]"}"),
            new Markup($"[bold]Blob Alias:[/] {session.ActiveContext?.ActiveBlobAlias ?? "[dim]none[/]"}")
        ))
        {
            Header = new PanelHeader("[bold green]Session Status[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
        return CommandResult.Ok(session);
    }

    private static string GetContextSummary(SessionContext? context)
    {
        if (context is null)
        {
            return "[dim]none[/]";
        }

        var parts = new List<string>();
        if (context.ActiveCosmosAlias is not null)
        {
            parts.Add($"cosmos:{context.ActiveCosmosAlias}");
        }
        if (context.ActiveBlobAlias is not null)
        {
            parts.Add($"blob:{context.ActiveBlobAlias}");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "[dim]none[/]";
    }

    private static string FormatTimestamp(DateTimeOffset timestamp)
    {
        var local = timestamp.ToLocalTime();
        var now = DateTimeOffset.Now;
        var diff = now - local;

        if (diff.TotalMinutes < 1)
        {
            return "just now";
        }
        if (diff.TotalHours < 1)
        {
            return $"{(int)diff.TotalMinutes}m ago";
        }
        if (diff.TotalDays < 1)
        {
            return $"{(int)diff.TotalHours}h ago";
        }
        if (diff.TotalDays < 7)
        {
            return $"{(int)diff.TotalDays}d ago";
        }

        return local.ToString("yyyy-MM-dd HH:mm");
    }
}
