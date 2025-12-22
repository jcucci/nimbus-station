using System.Text;

namespace NimbusStation.Cli.Repl;

/// <summary>
/// Parses command-line input into command name and arguments.
/// Handles quoted strings and basic escape sequences.
/// </summary>
public static class InputParser
{
    /// <summary>
    /// Parses an input line into tokens (command name and arguments).
    /// </summary>
    /// <param name="input">The raw input string.</param>
    /// <returns>An array of tokens, or empty if input is empty/whitespace.</returns>
    public static string[] Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuote = false;
        var quoteChar = '\0';
        var escaped = false;

        foreach (var c in input)
        {
            if (escaped)
            {
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

        // Add final token if any
        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens.ToArray();
    }

    /// <summary>
    /// Extracts the command name from parsed tokens.
    /// </summary>
    /// <param name="tokens">The parsed tokens.</param>
    /// <returns>The command name, or null if no tokens.</returns>
    public static string? GetCommandName(string[] tokens)
    {
        return tokens.Length > 0 ? tokens[0] : null;
    }

    /// <summary>
    /// Extracts the arguments (everything after the command name) from parsed tokens.
    /// </summary>
    /// <param name="tokens">The parsed tokens.</param>
    /// <returns>The arguments array.</returns>
    public static string[] GetArguments(string[] tokens)
    {
        return tokens.Length > 1 ? tokens[1..] : [];
    }
}
