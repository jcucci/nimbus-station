using NimbusStation.Core.Commands;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for setting the active resource context (cosmos, blob).
/// </summary>
public sealed class UseCommand : ICommand
{
    private readonly ISessionService _sessionService;
    private readonly IConfigurationService _configurationService;

    private static readonly HashSet<string> _subcommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "cosmos", "blob", "clear"
    };

    /// <inheritdoc/>
    public string Name => "use";

    /// <inheritdoc/>
    public string Description => "Set or clear the active resource context";

    /// <inheritdoc/>
    public string Usage => "use [cosmos|blob <alias>] | [clear [cosmos|blob]]";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands => _subcommands;

    /// <summary>
    /// Initializes a new instance of the <see cref="UseCommand"/> class.
    /// </summary>
    /// <param name="sessionService">The session service.</param>
    /// <param name="configurationService">The configuration service.</param>
    public UseCommand(ISessionService sessionService, IConfigurationService configurationService)
    {
        _sessionService = sessionService;
        _configurationService = configurationService;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default)
    {
        if (context.CurrentSession is null)
            return CommandResult.Error("No active session. Use 'session start <ticket>' first.");

        if (args.Length == 0)
            return HandleShowContext(context);

        var subcommand = args[0].ToLowerInvariant();
        var subArgs = args.Skip(1).ToArray();

        return subcommand switch
        {
            "cosmos" => await HandleSetCosmosAsync(subArgs, context, cancellationToken),
            "blob" => await HandleSetBlobAsync(subArgs, context, cancellationToken),
            "clear" => await HandleClearAsync(subArgs, context, cancellationToken),
            _ => CommandResult.Error($"Unknown provider '{args[0]}'. Available: cosmos, blob")
        };
    }

    private static CommandResult HandleShowContext(CommandContext context)
    {
        var session = context.CurrentSession!;
        var activeContext = session.ActiveContext;

        var cosmosAlias = activeContext?.ActiveCosmosAlias ?? "(none)";
        var blobAlias = activeContext?.ActiveBlobAlias ?? "(none)";

        context.Output.WriteLine("[bold]Active contexts:[/]");
        context.Output.WriteLine($"  [orange1]cosmos:[/] {cosmosAlias}");
        context.Output.WriteLine($"  [magenta]blob:[/]   {blobAlias}");

        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleSetCosmosAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: use cosmos <alias>");

        var aliasName = args[0];
        var aliasConfig = _configurationService.GetCosmosAlias(aliasName);

        if (aliasConfig is null)
            return CommandResult.Error($"Cosmos alias '{aliasName}' not found in config.");

        var session = context.CurrentSession!;
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = new SessionContext(aliasName, currentContext.ActiveBlobAlias);

        var updatedSession = await _sessionService.UpdateSessionContextAsync(
            session.TicketId,
            newContext,
            cancellationToken);

        _sessionService.CurrentSession = updatedSession;

        context.Output.WriteLine($"[green]Context set:[/] [orange1]cosmos/{aliasName}[/]");
        context.Output.WriteLine($"[dim]Endpoint: {aliasConfig.Endpoint}[/]");

        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleSetBlobAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: use blob <alias>");

        var aliasName = args[0];
        var aliasConfig = _configurationService.GetBlobAlias(aliasName);

        if (aliasConfig is null)
            return CommandResult.Error($"Blob alias '{aliasName}' not found in config.");

        var session = context.CurrentSession!;
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = new SessionContext(currentContext.ActiveCosmosAlias, aliasName);

        var updatedSession = await _sessionService.UpdateSessionContextAsync(
            session.TicketId,
            newContext,
            cancellationToken);

        _sessionService.CurrentSession = updatedSession;

        context.Output.WriteLine($"[green]Context set:[/] [magenta]blob/{aliasName}[/]");
        context.Output.WriteLine($"[dim]Account: {aliasConfig.Account}[/]");

        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleClearAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        var session = context.CurrentSession!;
        var currentContext = session.ActiveContext ?? SessionContext.Empty;

        SessionContext newContext;
        string message;

        if (args.Length == 0)
        {
            // Clear all contexts
            newContext = SessionContext.Empty;
            message = "Cleared all contexts.";
        }
        else
        {
            var provider = args[0].ToLowerInvariant();

            if (provider is not "cosmos" and not "blob")
                return CommandResult.Error($"Unknown provider '{args[0]}'. Available: cosmos, blob");

            newContext = provider switch
            {
                "cosmos" => new SessionContext(null, currentContext.ActiveBlobAlias),
                "blob" => new SessionContext(currentContext.ActiveCosmosAlias, null),
                _ => throw new InvalidOperationException() // Unreachable due to check above
            };

            message = $"Cleared {provider} context.";
        }

        var updatedSession = await _sessionService.UpdateSessionContextAsync(
            session.TicketId,
            newContext,
            cancellationToken);

        _sessionService.CurrentSession = updatedSession;

        context.Output.WriteLine($"[yellow]{message}[/]");

        return CommandResult.Ok();
    }
}
