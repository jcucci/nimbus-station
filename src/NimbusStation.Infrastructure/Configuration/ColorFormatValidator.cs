namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Validates color format strings for theme configuration.
/// Supports named colors (e.g., "green"), hex codes (e.g., "#50FA7B"),
/// and RGB values (e.g., "rgb(80,250,123)").
/// </summary>
public static class ColorFormatValidator
{
    /// <summary>
    /// Validates whether a string represents a valid color format.
    /// </summary>
    /// <param name="color">The color string to validate.</param>
    /// <returns>True if the color string format is valid; otherwise, false.</returns>
    public static bool IsValid(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        if (color.StartsWith('#'))
            return IsValidHexColor(color);

        if (color.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) && color.EndsWith(')'))
            return IsValidRgbColor(color);

        return IsValidNamedColor(color);
    }

    private static bool IsValidHexColor(string color)
    {
        var hex = color[1..];
        return (hex.Length == 3 || hex.Length == 6) &&
               hex.All(c => char.IsAsciiHexDigit(c));
    }

    private static bool IsValidRgbColor(string color)
    {
        var inner = color[4..^1];
        var parts = inner.Split(',');
        return parts.Length == 3 && parts.All(p => byte.TryParse(p.Trim(), out _));
    }

    // Note: This method validates the syntactic format of a named color (alphanumeric only).
    // It does not verify that the name corresponds to an actual Spectre.Console Color.
    // Semantic validation occurs when the value is parsed by SpectreColorHelper.
    // The alphanumeric restriction also prevents markup injection (no brackets allowed).
    private static bool IsValidNamedColor(string color) =>
        color.All(c => char.IsLetter(c) || char.IsDigit(c));
}
