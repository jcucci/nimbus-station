using System.Globalization;
using System.Reflection;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Output;

/// <summary>
/// Helper for parsing theme color strings into Spectre.Console Color objects.
/// Supports named colors (e.g., "green"), hex codes (e.g., "#50FA7B"),
/// and RGB values (e.g., "rgb(80,250,123)").
/// </summary>
public static class SpectreColorHelper
{
    /// <summary>
    /// Validates whether a string represents a valid color format.
    /// Delegates to <see cref="ColorFormatValidator.IsValid"/> for consistent validation.
    /// </summary>
    /// <param name="color">The color string to validate.</param>
    /// <returns>True if the color string is valid; otherwise, false.</returns>
    public static bool IsValidColor(string color) => ColorFormatValidator.IsValid(color);

    /// <summary>
    /// Parses a color string into a Spectre.Console Color.
    /// </summary>
    /// <param name="colorString">The color string to parse.</param>
    /// <returns>The parsed Color, or null if parsing failed.</returns>
    public static Color? TryParseColor(string colorString)
    {
        if (string.IsNullOrWhiteSpace(colorString))
            return null;

        try
        {
            if (colorString.StartsWith('#'))
                return ParseHexColor(colorString);

            if (colorString.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) && colorString.EndsWith(')'))
                return ParseRgbColor(colorString);

            return ParseNamedColor(colorString);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a color string into a Spectre.Console Color, returning a fallback if parsing fails.
    /// </summary>
    /// <param name="colorString">The color string to parse.</param>
    /// <param name="fallback">The fallback color to use if parsing fails.</param>
    /// <returns>The parsed Color, or the fallback if parsing failed.</returns>
    public static Color ParseColorOrDefault(string colorString, Color fallback) =>
        TryParseColor(colorString) ?? fallback;

    private static Color? ParseHexColor(string colorString)
    {
        var hex = colorString[1..];

        // Expand #RGB to #RRGGBB
        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);

        if (hex.Length != 6)
            return null;

        if (byte.TryParse(hex[..2], NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(hex[2..4], NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(hex[4..6], NumberStyles.HexNumber, null, out var b))
        {
            return new Color(r, g, b);
        }

        return null;
    }

    private static Color? ParseRgbColor(string colorString)
    {
        var inner = colorString[4..^1];
        var parts = inner.Split(',');

        if (parts.Length != 3)
            return null;

        if (byte.TryParse(parts[0].Trim(), out var r) &&
            byte.TryParse(parts[1].Trim(), out var g) &&
            byte.TryParse(parts[2].Trim(), out var b))
        {
            return new Color(r, g, b);
        }

        return null;
    }

    private static Color? ParseNamedColor(string colorString)
    {
        var colorProperty = typeof(Color).GetProperty(
            colorString,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

        return colorProperty?.GetValue(null) as Color?;
    }
}
