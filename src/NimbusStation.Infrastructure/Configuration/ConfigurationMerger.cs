namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Merges multiple NimbusConfiguration instances with proper override precedence.
/// Later configurations override earlier ones.
/// </summary>
public static class ConfigurationMerger
{
    /// <summary>
    /// Merges a source configuration into a target configuration.
    /// Values from source override values in target.
    /// </summary>
    /// <param name="target">The target configuration to merge into.</param>
    /// <param name="source">The source configuration to merge from.</param>
    public static void Merge(NimbusConfiguration target, NimbusConfiguration source)
    {
        if (source.DefaultProvider is not null)
            target.DefaultProvider = source.DefaultProvider;

        if (source.Theme != ThemeConfig.Default)
            target.Theme = MergeTheme(target.Theme, source.Theme);

        MergeDictionary(target.CosmosAliases, source.CosmosAliases);
        MergeDictionary(target.BlobAliases, source.BlobAliases);
        MergeDictionary(target.StorageAliases, source.StorageAliases);
    }

    private static ThemeConfig MergeTheme(ThemeConfig target, ThemeConfig source)
    {
        // Source values override target unless they match the default (indicating not explicitly set)
        var defaults = ThemeConfig.Default;

        return new ThemeConfig(
            PromptColor: source.PromptColor != defaults.PromptColor ? source.PromptColor : target.PromptColor,
            PromptSessionColor: source.PromptSessionColor != defaults.PromptSessionColor ? source.PromptSessionColor : target.PromptSessionColor,
            PromptContextColor: source.PromptContextColor != defaults.PromptContextColor ? source.PromptContextColor : target.PromptContextColor,
            PromptCosmosAliasColor: source.PromptCosmosAliasColor != defaults.PromptCosmosAliasColor ? source.PromptCosmosAliasColor : target.PromptCosmosAliasColor,
            PromptBlobAliasColor: source.PromptBlobAliasColor != defaults.PromptBlobAliasColor ? source.PromptBlobAliasColor : target.PromptBlobAliasColor,
            TableHeaderColor: source.TableHeaderColor != defaults.TableHeaderColor ? source.TableHeaderColor : target.TableHeaderColor,
            TableBorderColor: source.TableBorderColor != defaults.TableBorderColor ? source.TableBorderColor : target.TableBorderColor,
            ErrorColor: source.ErrorColor != defaults.ErrorColor ? source.ErrorColor : target.ErrorColor,
            WarningColor: source.WarningColor != defaults.WarningColor ? source.WarningColor : target.WarningColor,
            SuccessColor: source.SuccessColor != defaults.SuccessColor ? source.SuccessColor : target.SuccessColor,
            DimColor: source.DimColor != defaults.DimColor ? source.DimColor : target.DimColor,
            JsonKeyColor: source.JsonKeyColor != defaults.JsonKeyColor ? source.JsonKeyColor : target.JsonKeyColor,
            JsonStringColor: source.JsonStringColor != defaults.JsonStringColor ? source.JsonStringColor : target.JsonStringColor,
            JsonNumberColor: source.JsonNumberColor != defaults.JsonNumberColor ? source.JsonNumberColor : target.JsonNumberColor,
            JsonBooleanColor: source.JsonBooleanColor != defaults.JsonBooleanColor ? source.JsonBooleanColor : target.JsonBooleanColor,
            JsonNullColor: source.JsonNullColor != defaults.JsonNullColor ? source.JsonNullColor : target.JsonNullColor,
            BannerColor: source.BannerColor != defaults.BannerColor ? source.BannerColor : target.BannerColor);
    }

    private static void MergeDictionary<TKey, TValue>(
        Dictionary<TKey, TValue> target,
        Dictionary<TKey, TValue> source) where TKey : notnull
    {
        foreach (var kvp in source)
        {
            target[kvp.Key] = kvp.Value;
        }
    }
}
