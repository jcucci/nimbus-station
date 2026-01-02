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
    private readonly ISessionStateManager _sessionStateManager;
    private readonly IConfigurationService _configurationService;

    private static readonly HashSet<string> _subcommands = ["cosmos", "blob", "storage", "clear"];

    /// <inheritdoc/>
    public string Name => "use";

    /// <inheritdoc/>
    public string Description => "Set or clear the active resource context";

    /// <inheritdoc/>
    public string Usage => "use [cosmos|blob|storage <alias>] | [clear [cosmos|blob|storage]]";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands => _subcommands;

    /// <summary>
    /// Initializes a new instance of the <see cref="UseCommand"/> class.
    /// </summary>
    /// <param name="sessionService">The session service for persistence operations.</param>
    /// <param name="sessionStateManager">The session state manager for active session tracking.</param>
    /// <param name="configurationService">The configuration service.</param>
    public UseCommand(
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
        if (!context.HasActiveSession)
            return CommandResult.Error("No active session. Use 'session start <ticket>' first.");

        if (args.Length == 0)
            return HandleShowContext(context);

        var subcommand = args[0].ToLowerInvariant();
        var subArgs = args.Skip(1).ToArray();

        return subcommand switch
        {
            "cosmos" => await HandleSetCosmosAsync(subArgs, context, cancellationToken),
            "blob" => await HandleSetBlobAsync(subArgs, context, cancellationToken),
            "storage" => await HandleSetStorageAsync(subArgs, context, cancellationToken),
            "clear" => await HandleClearAsync(subArgs, context, cancellationToken),
            _ => CommandResult.Error($"Unknown provider '{args[0]}'. Available: cosmos, blob, storage")
        };
    }

    private CommandResult HandleShowContext(CommandContext context)
    {
        var session = context.CurrentSession!;
        var activeContext = session.ActiveContext;
        var theme = _configurationService.GetTheme();

        var cosmosAlias = activeContext?.ActiveCosmosAlias ?? "(none)";
        var blobAlias = activeContext?.ActiveBlobAlias ?? "(none)";
        var storageAlias = activeContext?.ActiveStorageAlias ?? "(none)";

        context.Output.WriteLine("[bold]Active contexts:[/]");
        context.Output.WriteLine($"  [{theme.PromptCosmosAliasColor}]cosmos:[/]   {cosmosAlias}");
        context.Output.WriteLine($"  [{theme.PromptBlobAliasColor}]blob:[/]     {blobAlias}");
        context.Output.WriteLine($"  [{theme.TableHeaderColor}]storage:[/]  {storageAlias}");

        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleSetCosmosAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: use cosmos <alias>");

        var aliasName = args[0];
        var theme = _configurationService.GetTheme();
        var aliasConfig = _configurationService.GetCosmosAlias(aliasName);

        if (aliasConfig is null)
            return CommandResult.Error($"Cosmos alias '{aliasName}' not found in config.");

        // Update in-memory state
        _sessionStateManager.SetCosmosAlias(aliasName);

        // Persist to disk
        await _sessionService.UpdateSessionContextAsync(
            context.CurrentSession!.TicketId,
            context.CurrentSession.ActiveContext!,
            cancellationToken);

        context.Output.WriteLine($"[{theme.SuccessColor}]Context set:[/] [{theme.PromptCosmosAliasColor}]cosmos/{aliasName}[/]");
        context.Output.WriteLine($"[{theme.DimColor}]Endpoint: {aliasConfig.Endpoint}[/]");

        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleSetBlobAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: use blob <alias>");

        var aliasName = args[0];
        var theme = _configurationService.GetTheme();
        var aliasConfig = _configurationService.GetBlobAlias(aliasName);

        if (aliasConfig is null)
            return CommandResult.Error($"Blob alias '{aliasName}' not found in config.");

        // Update in-memory state
        _sessionStateManager.SetBlobAlias(aliasName);

        // Persist to disk
        await _sessionService.UpdateSessionContextAsync(
            context.CurrentSession!.TicketId,
            context.CurrentSession.ActiveContext!,
            cancellationToken);

        context.Output.WriteLine($"[{theme.SuccessColor}]Context set:[/] [{theme.PromptBlobAliasColor}]blob/{aliasName}[/]");
        context.Output.WriteLine($"[{theme.DimColor}]Account: {aliasConfig.Account}[/]");

        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleSetStorageAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: use storage <alias>");

        var aliasName = args[0];
        var theme = _configurationService.GetTheme();
        var aliasConfig = _configurationService.GetStorageAlias(aliasName);

        if (aliasConfig is null)
            return CommandResult.Error($"Storage alias '{aliasName}' not found in config.");

        // Update in-memory state
        _sessionStateManager.SetStorageAlias(aliasName);

        // Persist to disk
        await _sessionService.UpdateSessionContextAsync(
            context.CurrentSession!.TicketId,
            context.CurrentSession.ActiveContext!,
            cancellationToken);

        context.Output.WriteLine($"[{theme.SuccessColor}]Context set:[/] [{theme.TableHeaderColor}]storage/{aliasName}[/]");
        context.Output.WriteLine($"[{theme.DimColor}]Account: {aliasConfig.Account}[/]");

        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleClearAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        string message;

        if (args.Length == 0)
        {
            // Clear all contexts
            _sessionStateManager.ClearAllAliases();
            message = "Cleared all contexts.";
        }
        else
        {
            var provider = args[0].ToLowerInvariant();

            switch (provider)
            {
                case "cosmos":
                    _sessionStateManager.ClearCosmosAlias();
                    message = "Cleared cosmos context.";
                    break;
                case "blob":
                    _sessionStateManager.ClearBlobAlias();
                    message = "Cleared blob context.";
                    break;
                case "storage":
                    _sessionStateManager.ClearStorageAlias();
                    message = "Cleared storage context.";
                    break;
                default:
                    return CommandResult.Error($"Unknown provider '{args[0]}'. Available: cosmos, blob, storage");
            }
        }

        // Persist to disk
        await _sessionService.UpdateSessionContextAsync(
            context.CurrentSession!.TicketId,
            context.CurrentSession.ActiveContext ?? SessionContext.Empty,
            cancellationToken);

        var theme = _configurationService.GetTheme();
        context.Output.WriteLine($"[{theme.WarningColor}]{message}[/]");

        return CommandResult.Ok();
    }
}
