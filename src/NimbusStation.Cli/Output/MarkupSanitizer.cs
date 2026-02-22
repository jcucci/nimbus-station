namespace NimbusStation.Cli.Output;

/// <summary>
/// Sanitizes text for safe use with Spectre.Console prompts.
/// Spectre.Console's search highlighting (AnsiMarkup.Highlight) calculates character
/// positions from stripped text then splices markup tags into the original. When the
/// original contains escaped brackets ([[/]]), the position math is wrong, producing
/// malformed markup. Replacing brackets with parentheses avoids this entirely.
/// </summary>
public static class MarkupSanitizer
{
    /// <summary>
    /// Replaces square brackets with parentheses so text is safe for
    /// SelectionPrompt/MultiSelectionPrompt choices with search enabled.
    /// </summary>
    public static string SanitizeBrackets(string text) =>
        text.Replace("[", "(").Replace("]", ")");
}
