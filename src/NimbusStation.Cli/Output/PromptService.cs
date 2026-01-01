using NimbusStation.Core.Options;
using Spectre.Console;

namespace NimbusStation.Cli.Output;

/// <summary>
/// Provides confirmation prompts that respect global options (--yes flag).
/// </summary>
public sealed class PromptService
{
    private readonly GlobalOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptService"/> class.
    /// </summary>
    /// <param name="options">The global CLI options.</param>
    public PromptService(GlobalOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Prompts the user for confirmation, respecting the --yes flag.
    /// </summary>
    /// <param name="message">The confirmation message to display.</param>
    /// <param name="defaultValue">The default value if the user just presses Enter.</param>
    /// <returns>True if confirmed, false otherwise.</returns>
    public bool Confirm(string message, bool defaultValue = false)
    {
        if (_options.YesToAll)
            return true;

        return AnsiConsole.Confirm(message, defaultValue);
    }

    /// <summary>
    /// Prompts the user for confirmation before overwriting a file.
    /// </summary>
    /// <param name="filePath">The path to the file that would be overwritten.</param>
    /// <returns>True if the user confirms or --yes flag is set, false otherwise.</returns>
    public bool ConfirmOverwrite(string filePath)
    {
        if (_options.YesToAll)
            return true;

        var fileName = Path.GetFileName(filePath);
        return AnsiConsole.Confirm($"File '{fileName}' already exists. Overwrite?", defaultValue: false);
    }

    /// <summary>
    /// Prompts the user for confirmation before a destructive operation.
    /// </summary>
    /// <param name="message">The warning message describing the destructive operation.</param>
    /// <param name="warningColor">The Spectre color name for the warning.</param>
    /// <returns>True if the user confirms or --yes flag is set, false otherwise.</returns>
    public bool ConfirmDestructive(string message, string warningColor = "yellow")
    {
        if (_options.YesToAll)
            return true;

        return AnsiConsole.Confirm($"[{warningColor}]{Markup.Escape(message)}[/]", defaultValue: false);
    }
}
