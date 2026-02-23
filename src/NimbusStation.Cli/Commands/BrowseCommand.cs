using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Browser;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for browsing resource aliases with hierarchical drill-down.
/// Displays alias details without activating them.
/// </summary>
public sealed class BrowseCommand : ICommand
{
    private readonly IConfigurationService _configurationService;

    private static readonly HashSet<string> _subcommands = ["cosmos", "blob", "storage"];

    /// <inheritdoc/>
    public string Name => "browse";

    /// <inheritdoc/>
    public string Description => "Browse resource aliases with hierarchical drill-down";

    /// <inheritdoc/>
    public string Usage => "browse [cosmos|blob|storage]";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands => _subcommands;

    /// <inheritdoc/>
    public CommandHelpMetadata HelpMetadata { get; } = new()
    {
        Subcommands =
        [
            new("cosmos", "Browse Cosmos DB aliases"),
            new("blob", "Browse Blob Storage aliases"),
            new("storage", "Browse Storage Account aliases")
        ],
        Examples =
        [
            new("browse cosmos", "Browse and inspect cosmos aliases"),
            new("browse blob", "Browse and inspect blob aliases")
        ],
        Notes = "Read-only browsing — displays alias details without activating them."
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowseCommand"/> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    public BrowseCommand(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0)
            return Task.FromResult(CommandResult.Error($"Usage: {Usage}"));

        var subcommand = args[0].ToLowerInvariant();

        return subcommand switch
        {
            "cosmos" => Task.FromResult(HandleBrowse("cosmos", context)),
            "blob" => Task.FromResult(HandleBrowse("blob", context)),
            "storage" => Task.FromResult(HandleBrowse("storage", context)),
            _ => Task.FromResult(CommandResult.Error($"Unknown type '{args[0]}'. Available: cosmos, blob, storage"))
        };
    }

    private CommandResult HandleBrowse(string providerType, CommandContext context)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
            return CommandResult.Error("Browse requires an interactive terminal.");

        var generators = _configurationService.GetGeneratorsConfig();
        var theme = _configurationService.GetTheme();

        List<string> allAliasNames = providerType switch
        {
            "cosmos" => _configurationService.GetAllCosmosAliases().Keys.ToList(),
            "blob" => _configurationService.GetAllBlobAliases().Keys.ToList(),
            "storage" => _configurationService.GetAllStorageAliases().Keys.ToList(),
            _ => throw new ArgumentException($"Unknown provider type: {providerType}")
        };

        AliasHierarchyNode? hierarchy = providerType switch
        {
            "cosmos" => AliasHierarchyBuilder.BuildCosmosHierarchy(generators, allAliasNames),
            "blob" => AliasHierarchyBuilder.BuildBlobHierarchy(generators, allAliasNames),
            "storage" => AliasHierarchyBuilder.BuildStorageHierarchy(generators, allAliasNames),
            _ => throw new ArgumentException($"Unknown provider type: {providerType}")
        };

        if (allAliasNames.Count == 0)
            return CommandResult.Error($"No {providerType} aliases configured.");

        string? selectedAlias = AliasBrowserHandler.Run(providerType, hierarchy, allAliasNames, theme);
        if (selectedAlias is null)
            return CommandResult.Ok();

        DisplayAliasDetails(providerType, selectedAlias, context, theme);
        return CommandResult.Ok();
    }

    private void DisplayAliasDetails(string providerType, string aliasName, CommandContext context, ThemeConfig theme)
    {
        switch (providerType)
        {
            case "cosmos":
                var cosmos = _configurationService.GetCosmosAlias(aliasName);
                if (cosmos is null) return;
                context.Output.WriteLine($"[{theme.SuccessColor}]Alias:[/] [{theme.PromptCosmosAliasColor}]cosmos/{aliasName}[/]");
                context.Output.WriteLine($"[{theme.DimColor}]Endpoint:  {cosmos.Endpoint}[/]");
                context.Output.WriteLine($"[{theme.DimColor}]Database:  {cosmos.Database}[/]");
                context.Output.WriteLine($"[{theme.DimColor}]Container: {cosmos.Container}[/]");
                break;

            case "blob":
                var blob = _configurationService.GetBlobAlias(aliasName);
                if (blob is null) return;
                context.Output.WriteLine($"[{theme.SuccessColor}]Alias:[/] [{theme.PromptBlobAliasColor}]blob/{aliasName}[/]");
                context.Output.WriteLine($"[{theme.DimColor}]Account:   {blob.Account}[/]");
                context.Output.WriteLine($"[{theme.DimColor}]Container: {blob.Container}[/]");
                break;

            case "storage":
                var storage = _configurationService.GetStorageAlias(aliasName);
                if (storage is null) return;
                context.Output.WriteLine($"[{theme.SuccessColor}]Alias:[/] [{theme.TableHeaderColor}]storage/{aliasName}[/]");
                context.Output.WriteLine($"[{theme.DimColor}]Account: {storage.Account}[/]");
                break;
        }
    }
}
