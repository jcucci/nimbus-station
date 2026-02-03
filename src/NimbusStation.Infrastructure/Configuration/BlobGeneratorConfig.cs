namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for the Blob storage alias generator.
/// </summary>
public sealed record BlobGeneratorConfig
{
    /// <summary>
    /// Whether this generator is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Template for the generated alias name (e.g., "{kingdoms}-{backends}-blob").
    /// </summary>
    public required string AliasNameTemplate { get; init; }

    /// <summary>
    /// Template for the storage account name.
    /// </summary>
    public required string AccountTemplate { get; init; }

    /// <summary>
    /// Template for the container name.
    /// </summary>
    public required string ContainerTemplate { get; init; }
}
