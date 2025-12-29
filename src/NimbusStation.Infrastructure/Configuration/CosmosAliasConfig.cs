namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for a CosmosDB connection alias.
/// </summary>
/// <param name="Endpoint">The CosmosDB account endpoint URL.</param>
/// <param name="Database">The database name.</param>
/// <param name="Container">The container name.</param>
/// <param name="KeyEnv">The environment variable name containing the read-only key. Nullable for future RBAC support.</param>
public sealed record CosmosAliasConfig(
    string Endpoint,
    string Database,
    string Container,
    string? KeyEnv = null);
