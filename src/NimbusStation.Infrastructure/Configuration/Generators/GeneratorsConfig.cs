namespace NimbusStation.Infrastructure.Configuration.Generators;

/// <summary>
/// Container for all generator configurations and dimensions.
/// </summary>
public sealed class GeneratorsConfig
{
    /// <summary>
    /// Gets or sets the named dimensions (e.g., "kingdoms", "backends").
    /// Each dimension contains named entries with their properties.
    /// </summary>
    public Dictionary<string, Dictionary<string, GeneratorDimensionEntry>> Dimensions { get; set; } = [];

    /// <summary>
    /// Gets or sets the Cosmos DB alias generator configuration.
    /// </summary>
    public CosmosGeneratorConfig? Cosmos { get; set; }

    /// <summary>
    /// Gets or sets the Blob storage alias generator configuration.
    /// </summary>
    public BlobGeneratorConfig? Blob { get; set; }

    /// <summary>
    /// Gets or sets the Storage account alias generator configuration.
    /// </summary>
    public StorageGeneratorConfig? Storage { get; set; }
}
