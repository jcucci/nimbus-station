namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for the Cosmos DB alias generator.
/// </summary>
public sealed record CosmosGeneratorConfig
{
    /// <summary>
    /// Whether this generator is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Template for the generated alias name (e.g., "{kingdoms}-{backends}-{type}").
    /// </summary>
    public required string AliasNameTemplate { get; init; }

    /// <summary>
    /// Template for the Cosmos endpoint URL.
    /// </summary>
    public required string EndpointTemplate { get; init; }

    /// <summary>
    /// Template for the database name.
    /// </summary>
    public required string DatabaseTemplate { get; init; }

    /// <summary>
    /// Template for the container name.
    /// </summary>
    public string? ContainerTemplate { get; init; }

    /// <summary>
    /// Type variations for Cosmos aliases.
    /// Key is the type name (e.g., "event"), value is the suffix (e.g., "events").
    /// When present, generates one alias per type per dimension combination.
    /// </summary>
    public IReadOnlyDictionary<string, string> Types { get; init; } =
        new Dictionary<string, string>();
}
