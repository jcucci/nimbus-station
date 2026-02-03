namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for the Storage account alias generator.
/// </summary>
public sealed record StorageGeneratorConfig
{
    /// <summary>
    /// Whether this generator is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Template for the generated alias name (e.g., "{kingdoms}-{tier}").
    /// </summary>
    public required string AliasNameTemplate { get; init; }

    /// <summary>
    /// Template for the storage account name.
    /// </summary>
    public required string AccountTemplate { get; init; }
}
