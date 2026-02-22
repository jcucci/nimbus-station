using System.Text.Json;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace NimbusStation.Cli.Output;

/// <summary>
/// Creates Spectre.Console renderables for JSON content with theme-aware syntax highlighting.
/// </summary>
public static class JsonRenderer
{
    /// <summary>
    /// Attempts to parse the given text as JSON and create a themed renderable.
    /// Returns null if the text is not valid JSON, allowing callers to fall back to raw output.
    /// </summary>
    /// <param name="text">The text to parse as JSON.</param>
    /// <param name="theme">The theme configuration for color mapping.</param>
    /// <returns>A renderable with syntax highlighting, or null if the text is not valid JSON.</returns>
    public static IRenderable? TryCreateRenderable(string text, ThemeConfig theme)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }

        var jsonText = new JsonText(text)
            .MemberStyle(new Style(SpectreColorHelper.ParseColorOrDefault(theme.JsonKeyColor, Color.Cyan1)))
            .StringStyle(new Style(SpectreColorHelper.ParseColorOrDefault(theme.JsonStringColor, Color.Green)))
            .NumberStyle(new Style(SpectreColorHelper.ParseColorOrDefault(theme.JsonNumberColor, Color.Magenta1)))
            .BooleanStyle(new Style(SpectreColorHelper.ParseColorOrDefault(theme.JsonBooleanColor, Color.Yellow)))
            .NullStyle(new Style(SpectreColorHelper.ParseColorOrDefault(theme.JsonNullColor, Color.Grey)));

        return jsonText;
    }
}
