using NimbusStation.Cli.Output;
using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for displaying detailed information about the active resource context.
/// </summary>
public sealed class InfoCommand : ICommand
{
    private readonly IConfigurationService _configurationService;

    /// <inheritdoc/>
    public string Name => "info";

    /// <inheritdoc/>
    public string Description => "Display active resource context details";

    /// <inheritdoc/>
    public string Usage => "info";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands { get; } = new HashSet<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="InfoCommand"/> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    public InfoCommand(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default)
    {
        var theme = _configurationService.GetTheme();

        if (context.CurrentSession is null)
            return Task.FromResult(CommandResult.Error("No active session. Use 'session start <ticket>' first."));

        var session = context.CurrentSession;
        var activeContext = session.ActiveContext;

        if (activeContext is null ||
            (activeContext.ActiveCosmosAlias is null && activeContext.ActiveBlobAlias is null))
        {
            context.Output.WriteLine($"[{theme.DimColor}]No active context. Use 'use cosmos <alias>' or 'use blob <alias>' to set one.[/]");
            return Task.FromResult(CommandResult.Ok());
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(SpectreColorHelper.ParseColorOrDefault(theme.TableBorderColor, Color.Grey));

        table.AddColumn(new TableColumn("[bold]Property[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

        // Add CosmosDB section if active
        if (activeContext.ActiveCosmosAlias is { } cosmosAlias)
        {
            var cosmosConfig = _configurationService.GetCosmosAlias(cosmosAlias);

            table.AddRow($"[{theme.PromptCosmosAliasColor} bold]CosmosDB[/]", "");
            table.AddRow("  Alias", $"[{theme.PromptSessionColor}]{cosmosAlias}[/]");

            if (cosmosConfig is not null)
            {
                table.AddRow("  Endpoint", cosmosConfig.Endpoint);
                table.AddRow("  Database", cosmosConfig.Database);
                table.AddRow("  Container", cosmosConfig.Container);
            }
            else
            {
                table.AddRow($"  [{theme.DimColor}]Config[/]", $"[{theme.ErrorColor}]Not found in config[/]");
            }
        }

        // Add separator if both are active
        if (activeContext.ActiveCosmosAlias is not null && activeContext.ActiveBlobAlias is not null)
        {
            table.AddEmptyRow();
        }

        // Add Blob section if active
        if (activeContext.ActiveBlobAlias is { } blobAlias)
        {
            var blobConfig = _configurationService.GetBlobAlias(blobAlias);

            table.AddRow($"[{theme.PromptBlobAliasColor} bold]Blob Storage[/]", "");
            table.AddRow("  Alias", $"[{theme.PromptSessionColor}]{blobAlias}[/]");

            if (blobConfig is not null)
            {
                table.AddRow("  Account", blobConfig.Account);
                table.AddRow("  Container", blobConfig.Container);
            }
            else
            {
                table.AddRow($"  [{theme.DimColor}]Config[/]", $"[{theme.ErrorColor}]Not found in config[/]");
            }
        }

        context.Output.WriteRenderable(table);

        return Task.FromResult(CommandResult.Ok());
    }
}
