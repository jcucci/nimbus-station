namespace NimbusStation.Infrastructure.Configuration.Generators;

/// <summary>
/// Configuration for generating Storage account aliases from templates.
/// </summary>
public sealed class StorageGeneratorConfig
{
    /// <summary>
    /// Gets or sets whether this generator is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the template for generating the alias name.
    /// Example: "{kingdom}-storage"
    /// </summary>
    public string AliasNameTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template for the storage account name.
    /// Example: "king{abbrev}sharpbe"
    /// </summary>
    public string AccountTemplate { get; set; } = string.Empty;
}
