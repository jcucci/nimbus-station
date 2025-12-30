using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Output;

/// <summary>
/// Prints the startup banner with theme-aware colors.
/// </summary>
public static class BannerPrinter
{
    /// <summary>
    /// Prints the Nimbus Station banner using the specified theme.
    /// </summary>
    /// <param name="theme">The theme configuration for colors.</param>
    /// <param name="version">The application version to display.</param>
    public static void Print(ThemeConfig theme, string version)
    {
        var bannerColor = SpectreColorHelper.ParseColorOrDefault(theme.BannerColor, Color.Cyan1);

        AnsiConsole.Write(
            new FigletText("Nimbus Station")
                .LeftJustified()
                .Color(bannerColor));

        AnsiConsole.MarkupLine($"[{theme.DimColor}]Cloud-agnostic investigation workbench v{version}[/]");
        AnsiConsole.WriteLine();
    }
}
