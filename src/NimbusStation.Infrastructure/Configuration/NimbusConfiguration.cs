using NimbusStation.Infrastructure.Configuration.Generators;

namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Represents the application configuration loaded from TOML.
/// </summary>
public sealed class NimbusConfiguration
{
    /// <summary>
    /// Gets or sets the default cloud provider identifier.
    /// </summary>
    public string? DefaultProvider { get; set; }

    /// <summary>
    /// Gets or sets the theme configuration.
    /// </summary>
    public ThemeConfig Theme { get; set; } = ThemeConfig.Default;

    /// <summary>
    /// Gets or sets the CosmosDB connection aliases.
    /// </summary>
    public Dictionary<string, CosmosAliasConfig> CosmosAliases { get; set; } = [];

    /// <summary>
    /// Gets or sets the Blob storage connection aliases.
    /// </summary>
    public Dictionary<string, BlobAliasConfig> BlobAliases { get; set; } = [];

    /// <summary>
    /// Gets or sets the Storage account aliases (for account-level operations like container listing).
    /// </summary>
    public Dictionary<string, StorageAliasConfig> StorageAliases { get; set; } = [];

    /// <summary>
    /// Gets or sets the include configuration for loading additional config files.
    /// </summary>
    public IncludeConfig? Include { get; set; }

    /// <summary>
    /// Gets or sets the generators configuration for template-based alias generation.
    /// </summary>
    public GeneratorsConfig? Generators { get; set; }
}
