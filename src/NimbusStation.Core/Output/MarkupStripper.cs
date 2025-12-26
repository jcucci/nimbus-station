using System.Text;

namespace NimbusStation.Core.Output;

/// <summary>
/// Utility class for stripping Spectre.Console markup tags from text.
/// </summary>
public static class MarkupStripper
{
    /// <summary>
    /// Strips Spectre.Console markup tags from text, returning plain text.
    /// Handles escaped brackets [[ and ]] correctly.
    /// </summary>
    /// <param name="text">The text potentially containing markup.</param>
    /// <returns>The text with all markup tags removed.</returns>
    public static string Strip(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder(text.Length);
        var inTag = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (c == '[')
            {
                // Check for escaped bracket [[
                if (i + 1 < text.Length && text[i + 1] == '[')
                {
                    result.Append('[');
                    i++; // Skip the second bracket
                    continue;
                }
                inTag = true;
                continue;
            }

            if (c == ']')
            {
                // Check for escaped bracket ]]
                if (i + 1 < text.Length && text[i + 1] == ']')
                {
                    result.Append(']');
                    i++; // Skip the second bracket
                    continue;
                }
                inTag = false;
                continue;
            }

            if (!inTag)
                result.Append(c);
        }

        return result.ToString();
    }
}
