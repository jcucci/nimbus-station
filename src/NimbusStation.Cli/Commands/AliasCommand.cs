using NimbusStation.Core.Aliases;
using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Aliases;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for managing command aliases (list, show, add, remove, test).
/// </summary>
public sealed class AliasCommand : ICommand
{
    private readonly IAliasService _aliasService;
    private readonly IAliasResolver _aliasResolver;

    /// <inheritdoc/>
    public string Name => "alias";

    /// <inheritdoc/>
    public string Description => "Manage command aliases";

    /// <inheritdoc/>
    public string Usage => "alias <list|show|add|remove|test> [args]";

    /// <summary>
    /// Initializes a new instance of the <see cref="AliasCommand"/> class.
    /// </summary>
    public AliasCommand(IAliasService aliasService, IAliasResolver aliasResolver)
    {
        _aliasService = aliasService;
        _aliasResolver = aliasResolver;
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
            "list" or "ls" => await HandleListAsync(cancellationToken),
            "show" => HandleShow(subArgs),
            "add" => await HandleAddAsync(subArgs, cancellationToken),
            "remove" or "rm" => await HandleRemoveAsync(subArgs, cancellationToken),
            "test" => await HandleTestAsync(subArgs, context, cancellationToken),
            _ => CommandResult.Error($"Unknown subcommand '{subcommand}'. Usage: {Usage}")
        };
    }

    private async Task<CommandResult> HandleListAsync(CancellationToken cancellationToken)
    {
        await _aliasService.LoadAliasesAsync(cancellationToken);
        var aliases = _aliasService.GetAllAliases();

        if (aliases.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No aliases defined. Use 'alias add <name> \"<expansion>\"' to create one.[/]");
            return CommandResult.Ok();
        }

        var table = new Table();
        table.AddColumn(new TableColumn("[bold]Alias[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Expansion[/]").LeftAligned());

        foreach (var (name, expansion) in aliases.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            table.AddRow(
                $"[cyan]{Markup.Escape(name)}[/]",
                $"[dim]{Markup.Escape(TruncateExpansion(expansion, maxLength: 60))}[/]");
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[dim]{aliases.Count} alias(es) defined[/]");

        return CommandResult.Ok(aliases);
    }

    private CommandResult HandleShow(string[] args)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: alias show <name>");

        var name = args[0];
        var aliases = _aliasService.GetAllAliases();

        if (!aliases.TryGetValue(name, out var expansion))
            return CommandResult.Error($"Alias '{name}' not found.");

        AnsiConsole.MarkupLine($"[cyan]{Markup.Escape(name)}[/] = [green]\"{Markup.Escape(expansion)}\"[/]");

        // Show parameter info if the alias has placeholders
        var placeholders = System.Text.RegularExpressions.Regex.Matches(expansion, @"\{(\d+)\}");
        if (placeholders.Count > 0)
        {
            var maxIndex = placeholders.Select(m => int.Parse(m.Groups[1].Value)).Max();
            var paramList = string.Join(", ", Enumerable.Range(0, maxIndex + 1).Select(i => $"{{[cyan]{i}[/]}}"));
            AnsiConsole.MarkupLine($"[dim]Parameters: {paramList}[/]");
        }

        // Show built-in variables used
        var builtInVars = new[] { "{ticket}", "{session-dir}", "{today}", "{now}", "{user}" };
        var usedVars = builtInVars.Where(v => expansion.Contains(v, StringComparison.OrdinalIgnoreCase)).ToList();
        if (usedVars.Count > 0)
            AnsiConsole.MarkupLine($"[dim]Variables: {string.Join(", ", usedVars)}[/]");

        return CommandResult.Ok(expansion);
    }

    private async Task<CommandResult> HandleAddAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length < 2)
            return CommandResult.Error("Usage: alias add <name> \"<expansion>\"");

        var name = args[0];
        var expansion = string.Join(" ", args.Skip(1));

        var validation = AliasNameValidator.Validate(name);
        if (!validation.IsValid)
            return CommandResult.Error(validation.ErrorMessage!);

        try
        {
            await _aliasService.AddAliasAsync(name, expansion, cancellationToken);
            AnsiConsole.MarkupLine($"[green]Added alias[/] [cyan]{Markup.Escape(name)}[/] = \"{Markup.Escape(expansion)}\"");
            return CommandResult.Ok();
        }
        catch (ArgumentException ex)
        {
            return CommandResult.Error(ex.Message);
        }
    }

    private async Task<CommandResult> HandleRemoveAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: alias remove <name>");

        var name = args[0];
        var removed = await _aliasService.RemoveAliasAsync(name, cancellationToken);

        if (!removed)
            return CommandResult.Error($"Alias '{name}' not found.");

        AnsiConsole.MarkupLine($"[red]Removed alias[/] [cyan]{Markup.Escape(name)}[/]");
        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleTestAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: alias test <name> [args...]");

        var aliasName = args[0];
        var aliasArgs = args.Skip(1).ToArray();

        var result = await _aliasResolver.TestExpandAsync(
            aliasName: aliasName,
            arguments: aliasArgs,
            currentSession: context.CurrentSession,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
            return CommandResult.Error(result.Error!);

        if (!result.WasExpanded)
        {
            AnsiConsole.MarkupLine($"[yellow]'{Markup.Escape(aliasName)}' is not a defined alias[/]");
            return CommandResult.Ok();
        }

        AnsiConsole.MarkupLine("[dim]Input:[/]");
        AnsiConsole.MarkupLine($"  [cyan]{Markup.Escape(aliasName)}[/] {Markup.Escape(string.Join(" ", aliasArgs))}");
        AnsiConsole.MarkupLine("[dim]Expands to:[/]");
        AnsiConsole.MarkupLine($"  [green]{Markup.Escape(result.ExpandedInput)}[/]");

        return CommandResult.Ok(result.ExpandedInput);
    }

    private static string TruncateExpansion(string expansion, int maxLength) =>
        expansion.Length <= maxLength ? expansion : expansion[..(maxLength - 3)] + "...";
}
