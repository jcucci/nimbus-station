using NimbusStation.Cli.Output;
using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for viewing and managing UI themes (list, preview, current).
/// </summary>
public sealed class ThemeCommand : ICommand
{
    private readonly IConfigurationService _configurationService;

    private static readonly HashSet<string> _subcommands = ["list", "ls", "preview", "current", "presets"];

    /// <inheritdoc/>
    public string Name => "theme";

    /// <inheritdoc/>
    public string Description => "View and manage UI themes";

    /// <inheritdoc/>
    public string Usage => "theme <list|preview|current> [args]";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands => _subcommands;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeCommand"/> class.
    /// </summary>
    public ThemeCommand(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0)
            return Task.FromResult(HandleCurrent(context));

        var subcommand = args[0].ToLowerInvariant();
        var subArgs = args.Skip(1).ToArray();

        var result = subcommand switch
        {
            "list" or "ls" or "presets" => HandleList(context),
            "preview" => HandlePreview(subArgs, context),
            "current" => HandleCurrent(context),
            _ => CommandResult.Error($"Unknown subcommand '{subcommand}'. Usage: {Usage}")
        };

        return Task.FromResult(result);
    }

    private CommandResult HandleList(CommandContext context)
    {
        var theme = _configurationService.GetTheme();
        var presetNames = ThemePresets.GetPresetNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(SpectreColorHelper.ParseColorOrDefault(theme.TableBorderColor, Color.Grey));

        table.AddColumn(new TableColumn($"[{theme.TableHeaderColor}]Theme[/]").LeftAligned());
        table.AddColumn(new TableColumn($"[{theme.TableHeaderColor}]Type[/]").LeftAligned());

        foreach (var name in presetNames)
        {
            var themeType = GetThemeType(name);
            table.AddRow(
                $"[{theme.PromptSessionColor}]{Markup.Escape(name)}[/]",
                $"[{theme.DimColor}]{themeType}[/]");
        }

        context.Output.WriteRenderable(table);
        context.Output.WriteLine($"[{theme.DimColor}]{presetNames.Count} theme(s) available[/]");
        context.Output.WriteLine($"[{theme.DimColor}]Use 'theme preview <name>' to preview a theme[/]");

        return CommandResult.Ok(presetNames);
    }

    private CommandResult HandlePreview(string[] args, CommandContext context)
    {
        if (args.Length == 0)
            return CommandResult.Error("Usage: theme preview <name>");

        var name = args[0];
        var previewTheme = ThemePresets.GetPreset(name);

        if (previewTheme is null)
        {
            var currentTheme = _configurationService.GetTheme();
            context.Output.WriteLine($"[{currentTheme.ErrorColor}]Theme '{Markup.Escape(name)}' not found.[/]");
            context.Output.WriteLine($"[{currentTheme.DimColor}]Use 'theme list' to see available themes.[/]");
            return CommandResult.Error($"Theme '{name}' not found.");
        }

        PrintThemePreview(previewTheme, name, context);

        return CommandResult.Ok(previewTheme);
    }

    private CommandResult HandleCurrent(CommandContext context)
    {
        var theme = _configurationService.GetTheme();

        context.Output.WriteLine($"[{theme.PromptSessionColor} bold]Current Theme Configuration[/]");
        context.Output.WriteLine();

        PrintThemeColors(theme, context);

        context.Output.WriteLine();
        context.Output.WriteLine($"[{theme.DimColor}]Configure in ~/.config/nimbus/config.toml under [theme][/]");

        return CommandResult.Ok(theme);
    }

    private void PrintThemePreview(ThemeConfig previewTheme, string themeName, CommandContext context)
    {
        var currentTheme = _configurationService.GetTheme();

        context.Output.WriteLine($"[{currentTheme.PromptSessionColor} bold]Preview: {Markup.Escape(themeName)}[/]");
        context.Output.WriteLine();

        // Show a sample prompt using the preview theme colors
        context.Output.WriteLine($"[{currentTheme.DimColor}]Sample prompt:[/]");
        var samplePrompt = BuildSamplePrompt(previewTheme);
        context.Output.WriteLine($"  {samplePrompt}");
        context.Output.WriteLine();

        // Show sample messages
        context.Output.WriteLine($"[{currentTheme.DimColor}]Sample messages:[/]");
        context.Output.WriteLine($"  [{previewTheme.SuccessColor}]Success: Operation completed successfully[/]");
        context.Output.WriteLine($"  [{previewTheme.WarningColor}]Warning: Configuration may need attention[/]");
        context.Output.WriteLine($"  [{previewTheme.ErrorColor}]Error: Something went wrong[/]");
        context.Output.WriteLine($"  [{previewTheme.DimColor}]Hint: This is dimmed helper text[/]");
        context.Output.WriteLine();

        // Show sample JSON highlighting
        context.Output.WriteLine($"[{currentTheme.DimColor}]Sample JSON:[/]");
        context.Output.WriteLine($"  {{");
        context.Output.WriteLine($"    [{previewTheme.JsonKeyColor}]\"name\"[/]: [{previewTheme.JsonStringColor}]\"example\"[/],");
        context.Output.WriteLine($"    [{previewTheme.JsonKeyColor}]\"count\"[/]: [{previewTheme.JsonNumberColor}]42[/],");
        context.Output.WriteLine($"    [{previewTheme.JsonKeyColor}]\"active\"[/]: [{previewTheme.JsonBooleanColor}]true[/],");
        context.Output.WriteLine($"    [{previewTheme.JsonKeyColor}]\"data\"[/]: [{previewTheme.JsonNullColor}]null[/]");
        context.Output.WriteLine($"  }}");
        context.Output.WriteLine();

        // Show color details
        PrintThemeColors(previewTheme, context);

        context.Output.WriteLine();
        context.Output.WriteLine($"[{currentTheme.DimColor}]To use this theme, add to ~/.config/nimbus/config.toml:[/]");
        context.Output.WriteLine($"[{currentTheme.DimColor}]  [theme][/]");
        context.Output.WriteLine($"[{currentTheme.DimColor}]  preset = \"{Markup.Escape(themeName)}\"[/]");
    }

    private void PrintThemeColors(ThemeConfig theme, CommandContext context)
    {
        var currentTheme = _configurationService.GetTheme();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(SpectreColorHelper.ParseColorOrDefault(currentTheme.TableBorderColor, Color.Grey));

        table.AddColumn(new TableColumn($"[{currentTheme.TableHeaderColor}]Property[/]").LeftAligned());
        table.AddColumn(new TableColumn($"[{currentTheme.TableHeaderColor}]Value[/]").LeftAligned());
        table.AddColumn(new TableColumn($"[{currentTheme.TableHeaderColor}]Preview[/]").LeftAligned());

        AddColorRow(table, "prompt_color", theme.PromptColor);
        AddColorRow(table, "prompt_session_color", theme.PromptSessionColor);
        AddColorRow(table, "prompt_context_color", theme.PromptContextColor);
        AddColorRow(table, "prompt_cosmos_alias_color", theme.PromptCosmosAliasColor);
        AddColorRow(table, "prompt_blob_alias_color", theme.PromptBlobAliasColor);
        AddColorRow(table, "table_header_color", theme.TableHeaderColor);
        AddColorRow(table, "table_border_color", theme.TableBorderColor);
        AddColorRow(table, "error_color", theme.ErrorColor);
        AddColorRow(table, "warning_color", theme.WarningColor);
        AddColorRow(table, "success_color", theme.SuccessColor);
        AddColorRow(table, "dim_color", theme.DimColor);
        AddColorRow(table, "json_key_color", theme.JsonKeyColor);
        AddColorRow(table, "json_string_color", theme.JsonStringColor);
        AddColorRow(table, "json_number_color", theme.JsonNumberColor);
        AddColorRow(table, "json_boolean_color", theme.JsonBooleanColor);
        AddColorRow(table, "json_null_color", theme.JsonNullColor);
        AddColorRow(table, "banner_color", theme.BannerColor);

        context.Output.WriteRenderable(table);
    }

    private static void AddColorRow(Table table, string propertyName, string colorValue)
    {
        var escapedValue = Markup.Escape(colorValue);
        table.AddRow(
            propertyName,
            escapedValue,
            $"[{colorValue}]{escapedValue}[/]");
    }

    private static string BuildSamplePrompt(ThemeConfig theme)
    {
        var prompt = $"[{theme.PromptColor}]ns[/]";
        prompt += $" [{theme.PromptSessionColor}]TICKET-123[/]";
        prompt += $" [{theme.PromptContextColor}][[/]";
        prompt += $"[{theme.PromptCosmosAliasColor}]prod-cosmos[/]";
        prompt += $"[{theme.PromptContextColor}]/[/]";
        prompt += $"[{theme.PromptBlobAliasColor}]prod-blob[/]";
        prompt += $"[{theme.PromptContextColor}]][/]";
        prompt += $"[{theme.PromptColor}]>[/] ";
        return prompt;
    }

    private static string GetThemeType(string name) =>
        name switch
        {
            "default" => "built-in",
            _ when name.Contains("light", StringComparison.OrdinalIgnoreCase) => "light",
            _ when name.Contains("latte", StringComparison.OrdinalIgnoreCase) => "light",
            _ when name.Contains("day", StringComparison.OrdinalIgnoreCase) => "light",
            _ => "dark"
        };
}
