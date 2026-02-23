using NimbusStation.Core.Aliases;
using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Aliases;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for managing command aliases (list, show, add, remove, test).
/// </summary>
public sealed class AliasCommand : ICommand
{
    private readonly IAliasService _aliasService;
    private readonly IAliasResolver _aliasResolver;
    private readonly IConfigurationService _configurationService;

    private static readonly HashSet<string> _subcommands =
    [
        "list", "ls", "show", "add", "remove", "rm", "test"
    ];

    /// <inheritdoc/>
    public string Name => "alias";

    /// <inheritdoc/>
    public string Description => "Manage command aliases";

    /// <inheritdoc/>
    public string Usage => "alias <list|show|add|remove|test> [args]";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands => _subcommands;

    /// <inheritdoc/>
    public CommandHelpMetadata HelpMetadata { get; } = new()
    {
        Subcommands =
        [
            new("list, ls", "List all defined aliases"),
            new("show <name>", "Show alias expansion details"),
            new("add <name> \"<expansion>\"", "Define a new alias"),
            new("remove, rm <name>", "Remove an alias"),
            new("test <name> [args...]", "Preview alias expansion without executing")
        ],
        Examples =
        [
            new("alias add q \"cosmos query\"", "Create a shortcut for cosmos query"),
            new("alias test q \"SELECT * FROM c\"", "Preview what 'q' expands to"),
            new("alias list", "Show all aliases")
        ],
        Notes = "Aliases support positional parameters ({0}, {1}) and built-in variables ({ticket}, {today})."
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AliasCommand"/> class.
    /// </summary>
    public AliasCommand(
        IAliasService aliasService,
        IAliasResolver aliasResolver,
        IConfigurationService configurationService)
    {
        _aliasService = aliasService;
        _aliasResolver = aliasResolver;
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
            "list" or "ls" => await HandleListAsync(context, cancellationToken),
            "show" => await HandleShowAsync(subArgs, context, cancellationToken),
            "add" => await HandleAddAsync(subArgs, context, cancellationToken),
            "remove" or "rm" => await HandleRemoveAsync(subArgs, context, cancellationToken),
            "test" => await HandleTestAsync(subArgs, context, cancellationToken),
            _ => CommandResult.Error($"Unknown subcommand '{subcommand}'. Usage: {Usage}")
        };
    }

    private async Task<CommandResult> HandleListAsync(CommandContext context, CancellationToken cancellationToken)
    {
        await _aliasService.LoadAliasesAsync(cancellationToken);
        var aliases = _aliasService.GetAllAliases();
        var theme = _configurationService.GetTheme();

        if (aliases.Count == 0)
        {
            context.Output.WriteLine($"[{theme.DimColor}]No aliases defined. Use 'alias add <name> \"<expansion>\"' to create one.[/]");
            return CommandResult.Ok();
        }

        var table = new Table();
        table.AddColumn(new TableColumn("[bold]Alias[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Expansion[/]").LeftAligned());

        foreach (var (name, expansion) in aliases.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            table.AddRow(
                $"[{theme.PromptSessionColor}]{Markup.Escape(name)}[/]",
                $"[{theme.DimColor}]{Markup.Escape(TruncateExpansion(expansion, maxLength: 60))}[/]");
        }

        context.Output.WriteRenderable(table);
        context.Output.WriteLine($"[{theme.DimColor}]{aliases.Count} alias(es) defined[/]");

        return CommandResult.Ok(aliases);
    }

    private async Task<CommandResult> HandleShowAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: alias show <name>");

        var name = args[0];
        var theme = _configurationService.GetTheme();
        await _aliasService.LoadAliasesAsync(cancellationToken);
        var aliases = _aliasService.GetAllAliases();

        if (!aliases.TryGetValue(name, out var expansion))
            return CommandResult.Error($"Alias '{name}' not found.");

        context.Output.WriteLine($"[{theme.PromptSessionColor}]{Markup.Escape(name)}[/] = [{theme.SuccessColor}]\"{Markup.Escape(expansion)}\"[/]");

        // Show parameter info if the alias has placeholders
        var placeholders = System.Text.RegularExpressions.Regex.Matches(expansion, @"\{(\d+)\}");
        if (placeholders.Count > 0)
        {
            var maxIndex = placeholders.Select(m => int.Parse(m.Groups[1].Value)).Max();
            var paramList = string.Join(", ", Enumerable.Range(0, maxIndex + 1).Select(i => $"{{[{theme.PromptSessionColor}]{i}[/]}}"));
            context.Output.WriteLine($"[{theme.DimColor}]Parameters: {paramList}[/]");
        }

        // Show built-in variables used
        var builtInVars = new[] { "{ticket}", "{session-dir}", "{today}", "{now}", "{user}" };
        var usedVars = builtInVars.Where(v => expansion.Contains(v, StringComparison.OrdinalIgnoreCase)).ToList();
        if (usedVars.Count > 0)
            context.Output.WriteLine($"[{theme.DimColor}]Variables: {string.Join(", ", usedVars)}[/]");

        return CommandResult.Ok(expansion);
    }

    private async Task<CommandResult> HandleAddAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length < 2)
            return CommandResult.Error("Usage: alias add <name> \"<expansion>\"");

        var name = args[0];
        var expansion = string.Join(" ", args.Skip(1));
        var theme = _configurationService.GetTheme();

        var validation = AliasNameValidator.Validate(name);
        if (!validation.IsValid)
            return CommandResult.Error(validation.ErrorMessage!);

        try
        {
            await _aliasService.AddAliasAsync(name, expansion, cancellationToken);
            context.Output.WriteLine($"[{theme.SuccessColor}]Added alias[/] [{theme.PromptSessionColor}]{Markup.Escape(name)}[/] = \"{Markup.Escape(expansion)}\"");
            return CommandResult.Ok();
        }
        catch (ArgumentException ex)
        {
            return CommandResult.Error(ex.Message);
        }
    }

    private async Task<CommandResult> HandleRemoveAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: alias remove <name>");

        var name = args[0];
        var theme = _configurationService.GetTheme();
        var removed = await _aliasService.RemoveAliasAsync(name, cancellationToken);

        if (!removed)
            return CommandResult.Error($"Alias '{name}' not found.");

        context.Output.WriteLine($"[{theme.ErrorColor}]Removed alias[/] [{theme.PromptSessionColor}]{Markup.Escape(name)}[/]");
        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleTestAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: alias test <name> [args...]");

        var aliasName = args[0];
        var aliasArgs = args.Skip(1).ToArray();
        var theme = _configurationService.GetTheme();

        var result = await _aliasResolver.TestExpandAsync(
            aliasName: aliasName,
            arguments: aliasArgs,
            currentSession: context.CurrentSession,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
            return CommandResult.Error(result.Error!);

        if (!result.WasExpanded)
        {
            context.Output.WriteLine($"[{theme.WarningColor}]'{Markup.Escape(aliasName)}' is not a defined alias[/]");
            return CommandResult.Ok();
        }

        context.Output.WriteLine($"[{theme.DimColor}]Input:[/]");
        context.Output.WriteLine($"  [{theme.PromptSessionColor}]{Markup.Escape(aliasName)}[/] {Markup.Escape(string.Join(" ", aliasArgs))}");
        context.Output.WriteLine($"[{theme.DimColor}]Expands to:[/]");
        context.Output.WriteLine($"  [{theme.SuccessColor}]{Markup.Escape(result.ExpandedInput)}[/]");

        return CommandResult.Ok(result.ExpandedInput);
    }

    private static string TruncateExpansion(string expansion, int maxLength) =>
        expansion.Length <= maxLength ? expansion : expansion[..(maxLength - 3)] + "...";
}
