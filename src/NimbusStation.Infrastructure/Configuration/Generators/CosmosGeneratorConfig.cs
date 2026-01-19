namespace NimbusStation.Infrastructure.Configuration.Generators;

/// <summary>
/// Configuration for generating Cosmos DB aliases from templates.
/// </summary>
public sealed class CosmosGeneratorConfig
{
    /// <summary>
    /// Gets or sets whether this generator is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the template for generating the alias name.
    /// Example: "{kingdom}-{backend}-{type}"
    /// </summary>
    public string AliasNameTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template for the Cosmos endpoint URL.
    /// Example: "https://king-{abbrev}-sharp-be-cdb.documents.azure.com:443/"
    /// </summary>
    public string EndpointTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template for the database name.
    /// Example: "{database}"
    /// </summary>
    public string DatabaseTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template for the container name.
    /// Example: "{container_base}-{type_suffix}"
    /// </summary>
    public string ContainerTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type mappings for Cosmos aliases.
    /// Keys are type names (e.g., "event", "data"), values are suffixes used in container names.
    /// </summary>
    public Dictionary<string, string> Types { get; set; } = [];

    /// <summary>
    /// Gets or sets the key environment variable template.
    /// Example: "{key_env}"
    /// </summary>
    public string? KeyEnvTemplate { get; set; }
}
