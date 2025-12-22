namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for terminal theme colors.
/// </summary>
/// <param name="PromptColor">Color for the REPL prompt.</param>
/// <param name="TableHeaderColor">Color for table headers.</param>
/// <param name="JsonKeyColor">Color for JSON keys in output.</param>
public sealed record ThemeConfig(string PromptColor, string TableHeaderColor, string JsonKeyColor)
{
    /// <summary>
    /// Gets the default theme configuration.
    /// </summary>
    public static ThemeConfig Default { get; } = new("green", "blue", "cyan");
}
