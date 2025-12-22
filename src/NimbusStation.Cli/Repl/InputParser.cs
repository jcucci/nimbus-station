using NimbusStation.Core.Parsing;

namespace NimbusStation.Cli.Repl;

/// <summary>
/// Parses command-line input into command name and arguments.
/// Handles quoted strings and basic escape sequences.
/// </summary>
public static class InputParser
{
    /// <summary>
    /// Parses an input line into tokens (command name and arguments).
    /// Quotes are stripped from the output tokens.
    /// </summary>
    /// <param name="input">The raw input string.</param>
    /// <returns>An array of tokens, or empty if input is empty/whitespace.</returns>
    public static string[] Parse(string? input) =>
        InputTokenizer.Tokenize(input, preserveQuotes: false);

    /// <summary>
    /// Extracts the command name from parsed tokens.
    /// </summary>
    /// <param name="tokens">The parsed tokens.</param>
    /// <returns>The command name, or null if no tokens.</returns>
    public static string? GetCommandName(string[] tokens) =>
        InputTokenizer.GetCommandName(tokens);

    /// <summary>
    /// Extracts the arguments (everything after the command name) from parsed tokens.
    /// </summary>
    /// <param name="tokens">The parsed tokens.</param>
    /// <returns>The arguments array.</returns>
    public static string[] GetArguments(string[] tokens) =>
        InputTokenizer.GetArguments(tokens);
}
