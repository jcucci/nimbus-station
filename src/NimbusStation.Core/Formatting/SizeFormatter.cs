namespace NimbusStation.Core.Formatting;

/// <summary>
/// Provides utilities for formatting file sizes in human-readable form.
/// </summary>
public static class SizeFormatter
{
    private static readonly string[] SizeSuffixes = ["B", "KB", "MB", "GB", "TB"];

    /// <summary>
    /// Formats a byte count as a human-readable size string.
    /// </summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <returns>A formatted string (e.g., "1.5 MB", "256 B").</returns>
    public static string Format(long bytes)
    {
        if (bytes < 0)
            return "0 B";

        var suffixIndex = 0;
        var size = (double)bytes;

        while (size >= 1024 && suffixIndex < SizeSuffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return suffixIndex == 0
            ? $"{size:F0} {SizeSuffixes[suffixIndex]}"
            : $"{size:F1} {SizeSuffixes[suffixIndex]}";
    }
}
