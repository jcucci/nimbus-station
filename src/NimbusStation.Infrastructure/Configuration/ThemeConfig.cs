namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for terminal theme colors.
/// All colors support Spectre.Console color names (e.g., "green", "cyan"),
/// hex codes (e.g., "#50FA7B"), or RGB values (e.g., "rgb(80,250,123)").
/// </summary>
/// <param name="PromptColor">Color for the main prompt text (e.g., "ns").</param>
/// <param name="PromptSessionColor">Color for the session/ticket ID in the prompt.</param>
/// <param name="PromptContextColor">Color for context brackets in the prompt.</param>
/// <param name="PromptCosmosAliasColor">Color for the active Cosmos alias in the prompt.</param>
/// <param name="PromptBlobAliasColor">Color for the active Blob alias in the prompt.</param>
/// <param name="TableHeaderColor">Color for table headers.</param>
/// <param name="TableBorderColor">Color for table borders.</param>
/// <param name="ErrorColor">Color for error messages.</param>
/// <param name="WarningColor">Color for warning messages.</param>
/// <param name="SuccessColor">Color for success messages.</param>
/// <param name="DimColor">Color for muted/hint text.</param>
/// <param name="JsonKeyColor">Color for JSON object keys.</param>
/// <param name="JsonStringColor">Color for JSON string values.</param>
/// <param name="JsonNumberColor">Color for JSON number values.</param>
/// <param name="JsonBooleanColor">Color for JSON boolean values.</param>
/// <param name="JsonNullColor">Color for JSON null values.</param>
/// <param name="BannerColor">Color for the startup banner.</param>
public sealed record ThemeConfig(
    string PromptColor,
    string PromptSessionColor,
    string PromptContextColor,
    string PromptCosmosAliasColor,
    string PromptBlobAliasColor,
    string TableHeaderColor,
    string TableBorderColor,
    string ErrorColor,
    string WarningColor,
    string SuccessColor,
    string DimColor,
    string JsonKeyColor,
    string JsonStringColor,
    string JsonNumberColor,
    string JsonBooleanColor,
    string JsonNullColor,
    string BannerColor)
{
    /// <summary>
    /// Gets the default theme configuration.
    /// </summary>
    public static ThemeConfig Default { get; } = new(
        PromptColor: "green",
        PromptSessionColor: "cyan",
        PromptContextColor: "yellow",
        PromptCosmosAliasColor: "orange1",
        PromptBlobAliasColor: "magenta",
        TableHeaderColor: "blue",
        TableBorderColor: "grey",
        ErrorColor: "red",
        WarningColor: "yellow",
        SuccessColor: "green",
        DimColor: "grey",
        JsonKeyColor: "cyan",
        JsonStringColor: "green",
        JsonNumberColor: "magenta",
        JsonBooleanColor: "yellow",
        JsonNullColor: "grey",
        BannerColor: "cyan1");
}
