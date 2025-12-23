using System.Text;

namespace NimbusStation.Spike.Piping;

/// <summary>
/// Parses command input into pipeline segments, respecting quotes and escape sequences.
/// </summary>
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
                    IsLast: false)); // Will fix up later

                current.Clear();
                continue;
            }

            current.Append(c);
        }

        // Handle trailing backslash
        if (escaped)
            current.Append('\\');

        // Handle unclosed quote - include it as-is (user might be mid-typing)
        // For a spike, we'll just warn but continue

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

        // Fix up IsLast for all segments (only the last one should be true)
        var result = new List<PipelineSegment>(segments.Count);
        for (var i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            result.Add(seg with { IsLast = i == segments.Count - 1 });
        }

        return ParsedPipeline.Success(result);
    }

    /// <summary>
    /// Checks if the input contains any unquoted, unescaped pipe characters.
    /// Quick check without full parsing.
    /// </summary>
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
