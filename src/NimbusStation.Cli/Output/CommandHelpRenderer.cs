using NimbusStation.Core.Commands;
using NimbusStation.Core.Output;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Output;

/// <summary>
/// Renders TLDR-style inline help for commands using structured <see cref="CommandHelpMetadata"/>.
/// </summary>
public static class CommandHelpRenderer
{
    /// <summary>
    /// Renders help for a command to the given output writer.
    /// When the command has <see cref="ICommand.HelpMetadata"/>, renders rich structured help.
    /// Otherwise falls back to basic name, description, and usage.
    /// </summary>
    public static void Render(ICommand command, IOutputWriter output, ThemeConfig theme)
    {
        output.WriteLine($"[bold]{Markup.Escape(command.Name)}[/] - {Markup.Escape(command.Description)}");
        output.WriteLine($"[{theme.DimColor}]Usage:[/] {Markup.Escape(command.Usage)}");

        var metadata = command.HelpMetadata;
        if (metadata is null)
            return;

        if (metadata.Subcommands.Count > 0)
        {
            output.WriteLine();
            output.WriteLine("[bold]Subcommands:[/]");
            RenderEntries(metadata.Subcommands, output, theme);
        }

        if (metadata.Flags.Count > 0)
        {
            output.WriteLine();
            output.WriteLine("[bold]Flags:[/]");
            RenderEntries(metadata.Flags, output, theme);
        }

        if (metadata.Examples.Count > 0)
        {
            output.WriteLine();
            output.WriteLine("[bold]Examples:[/]");
            RenderEntries(metadata.Examples, output, theme);
        }

        if (metadata.Notes is not null)
        {
            output.WriteLine();
            output.WriteLine($"[{theme.DimColor}]{Markup.Escape(metadata.Notes)}[/]");
        }
    }

    private static void RenderEntries(IReadOnlyList<HelpEntry> entries, IOutputWriter output, ThemeConfig theme)
    {
        int maxLabelWidth = entries.Max(e => e.Label.Length);
        int padding = maxLabelWidth + 4;

        foreach (var entry in entries)
        {
            string paddedLabel = entry.Label.PadRight(padding);
            output.WriteLine($"  [{theme.TableHeaderColor}]{Markup.Escape(paddedLabel)}[/]{Markup.Escape(entry.Description)}");
        }
    }
}
