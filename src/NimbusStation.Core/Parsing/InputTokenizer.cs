using System.Text;

namespace NimbusStation.Core.Parsing;

/// <summary>
/// Tokenizes command-line input with support for quoted strings and escape sequences.
/// </summary>
public static class InputTokenizer
{
    /// <summary>
    /// Tokenizes an input line into individual tokens.
    /// </summary>
    /// <param name="input">The raw input string.</param>
    /// <param name="preserveQuotes">Whether to preserve quote characters in the output tokens.</param>
    /// <returns>An array of tokens, or empty if input is empty/whitespace.</returns>
    public static string[] Tokenize(string? input, bool preserveQuotes = false)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuote = false;
        var quoteChar = '\0';
        var escaped = false;

        foreach (var c in input)
        {
            if (escaped)
            {
                if (preserveQuotes)
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
                if (c == quoteChar)
                {
                    if (preserveQuotes)
                        current.Append(c);
                    inQuote = false;
                    quoteChar = '\0';
                }
                else
                {
                    current.Append(c);
                }
                continue;
            }

            if (c is '"' or '\'')
            {
                inQuote = true;
                quoteChar = c;
                if (preserveQuotes)
                    current.Append(c);
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return [.. tokens];
    }

    /// <summary>
    /// Extracts the command name (first token) from the input.
    /// </summary>
    /// <param name="tokens">The parsed tokens.</param>
    /// <returns>The command name, or null if no tokens.</returns>
    public static string? GetCommandName(string[] tokens) =>
        tokens.Length > 0 ? tokens[0] : null;

    /// <summary>
    /// Extracts the arguments (everything after the command name) from parsed tokens.
    /// </summary>
    /// <param name="tokens">The parsed tokens.</param>
    /// <returns>The arguments array.</returns>
    public static string[] GetArguments(string[] tokens) =>
        tokens.Length > 1 ? tokens[1..] : [];
}
