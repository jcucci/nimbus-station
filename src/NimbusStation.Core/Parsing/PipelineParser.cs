using System.Text;

namespace NimbusStation.Core.Parsing;

/// <summary>
/// Parses command input into pipeline segments, respecting quotes and escape sequences.
/// </summary>
/// <remarks>
/// <para>
/// Escape sequences are preserved literally in the output. A backslash escapes only
/// the immediately following character:
/// </para>
/// <list type="bullet">
///   <item><c>\|</c> - Escaped pipe, not a separator. Output: <c>\|</c></item>
///   <item><c>\\</c> - Escaped backslash. Output: <c>\\</c></item>
///   <item><c>\\|</c> - Escaped backslash followed by unescaped pipe (IS a separator)</item>
///   <item><c>\\\|</c> - Escaped backslash followed by escaped pipe (NOT a separator)</item>
/// </list>
/// <para>
/// Pipes inside quoted strings (single or double quotes) are never treated as separators.
/// </para>
/// </remarks>
public static class PipelineParser
{
    /// <summary>
    /// Parses an input string into pipeline segments separated by unquoted, unescaped pipe characters.
    /// </summary>
    /// <param name="input">The raw input string.</param>
    /// <returns>A parsed pipeline with segments or an error.</returns>
    public static ParsedPipeline Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ParsedPipeline.Failure("Input is empty");

        var segments = new List<PipelineSegment>();
        var current = new StringBuilder();
        var inQuote = false;
        var quoteChar = '\0';
        var escaped = false;

        foreach (var c in input)
        {
            if (escaped)
            {
                current.Append('\\');
                current.Append(c);
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (inQuote)
            {
                current.Append(c);
                if (c == quoteChar)
                {
                    inQuote = false;
                    quoteChar = '\0';
                }
                continue;
            }

            if (c is '"' or '\'')
            {
                inQuote = true;
                quoteChar = c;
                current.Append(c);
                continue;
            }

            if (c == '|')
            {
                // Found an unquoted, unescaped pipe - end current segment
                var segmentContent = current.ToString().Trim();
                if (string.IsNullOrEmpty(segmentContent))
                {
                    return segments.Count == 0
                        ? ParsedPipeline.Failure("No command before pipe character")
                        : ParsedPipeline.Failure($"Empty segment at position {segments.Count + 1}");
                }

                segments.Add(new PipelineSegment(
                    Content: segmentContent,
                    Index: segments.Count,
                    IsFirst: segments.Count == 0,
                    IsLast: false));

                current.Clear();
                continue;
            }

            current.Append(c);
        }

        // Handle trailing backslash
        if (escaped)
            current.Append('\\');

        // Add final segment
        var finalContent = current.ToString().Trim();
        if (string.IsNullOrEmpty(finalContent))
        {
            return segments.Count == 0
                ? ParsedPipeline.Failure("Input is empty")
                : ParsedPipeline.Failure("No command after final pipe character");
        }

        segments.Add(new PipelineSegment(
            Content: finalContent,
            Index: segments.Count,
            IsFirst: segments.Count == 0,
            IsLast: true));

        return ParsedPipeline.Success(segments);
    }

    /// <summary>
    /// Checks if the input contains any unquoted, unescaped pipe characters.
    /// Quick check without full parsing.
    /// </summary>
    /// <param name="input">The raw input string.</param>
    /// <returns>True if the input contains a pipe that would split the command.</returns>
    public static bool ContainsPipe(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var inQuote = false;
        var quoteChar = '\0';
        var escaped = false;

        foreach (var c in input)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (inQuote)
            {
                if (c == quoteChar)
                {
                    inQuote = false;
                    quoteChar = '\0';
                }
                continue;
            }

            if (c is '"' or '\'')
            {
                inQuote = true;
                quoteChar = c;
                continue;
            }

            if (c == '|')
                return true;
        }

        return false;
    }
}
