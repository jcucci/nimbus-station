using NimbusStation.Core.Errors;
using Spectre.Console;

namespace NimbusStation.Cli.Output;

/// <summary>
/// Formats <see cref="UserFacingError"/> instances for display in the console.
/// </summary>
public static class ErrorFormatter
{
    private const string Indent = "       ";
    private const string Bullet = "\u2022"; // •

    /// <summary>
    /// Formats an error for display, respecting quiet mode.
    /// </summary>
    /// <param name="error">The error to format.</param>
    /// <param name="errorColor">The Spectre color name for the error prefix.</param>
    /// <param name="quiet">If true, only outputs the message without details or suggestions.</param>
    /// <returns>The formatted markup string.</returns>
    public static string Format(UserFacingError error, string errorColor, bool quiet = false)
    {
        var escapedMessage = Markup.Escape(error.Message);
        var lines = new List<string>
        {
            $"[{errorColor}]Error:[/] {escapedMessage}"
        };

        if (quiet)
            return string.Join(Environment.NewLine, lines);

        if (!string.IsNullOrWhiteSpace(error.Details))
        {
            lines.Add($"{Indent}{Markup.Escape(error.Details)}");
        }

        if (error.Suggestions is { Count: > 0 })
        {
            lines.Add(string.Empty);
            lines.Add($"{Indent}Suggestions:");
            foreach (var suggestion in error.Suggestions)
            {
                lines.Add($"{Indent}{Bullet} {Markup.Escape(suggestion)}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Formats a simple error message (not a UserFacingError) for display.
    /// </summary>
    public static string FormatSimple(string message, string errorColor) =>
        $"[{errorColor}]Error:[/] {Markup.Escape(message)}";

    /// <summary>
    /// Formats an "unknown command" error with optional suggestions.
    /// </summary>
    /// <param name="commandName">The unknown command that was entered.</param>
    /// <param name="suggestions">Optional list of similar command names.</param>
    /// <param name="errorColor">The Spectre color name for the error prefix.</param>
    /// <param name="quiet">If true, only outputs the message without suggestions.</param>
    public static string FormatUnknownCommand(
        string commandName,
        IReadOnlyList<string>? suggestions,
        string errorColor,
        bool quiet = false)
    {
        var escapedCommand = Markup.Escape(commandName);
        var lines = new List<string>
        {
            $"[{errorColor}]Error:[/] Unknown command '{escapedCommand}'"
        };

        if (quiet)
            return string.Join(Environment.NewLine, lines);

        if (suggestions is { Count: > 0 })
        {
            lines.Add(string.Empty);
            lines.Add($"{Indent}Did you mean?");
            foreach (var suggestion in suggestions)
            {
                lines.Add($"{Indent}{Bullet} {Markup.Escape(suggestion)}");
            }
        }

        lines.Add(string.Empty);
        lines.Add($"{Indent}Run 'help' to see available commands.");

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Writes a formatted error to the console.
    /// </summary>
    public static void WriteError(UserFacingError error, string errorColor, bool quiet = false) =>
        AnsiConsole.MarkupLine(Format(error, errorColor, quiet));

    /// <summary>
    /// Writes a simple error message to the console.
    /// </summary>
    public static void WriteSimpleError(string message, string errorColor) =>
        AnsiConsole.MarkupLine(FormatSimple(message, errorColor));

    /// <summary>
    /// Writes an unknown command error to the console.
    /// </summary>
    public static void WriteUnknownCommand(
        string commandName,
        IReadOnlyList<string>? suggestions,
        string errorColor,
        bool quiet = false) =>
        AnsiConsole.MarkupLine(FormatUnknownCommand(commandName, suggestions, errorColor, quiet));
}
