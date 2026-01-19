namespace NimbusStation.Infrastructure.Configuration.Generators;

/// <summary>
/// Configuration for generating Blob storage aliases from templates.
/// </summary>
public sealed class BlobGeneratorConfig
{
    /// <summary>
    /// Gets or sets whether this generator is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the template for generating the alias name.
    /// Example: "{kingdom}-{backend}-blob"
    /// </summary>
    public string AliasNameTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template for the storage account name.
    /// Example: "king{abbrev}sharpbe{storage_tier}"
    /// </summary>
    public string AccountTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template for the blob container name.
    /// Example: "{backend}-blobs"
    /// </summary>
    public string ContainerTemplate { get; set; } = string.Empty;
}
