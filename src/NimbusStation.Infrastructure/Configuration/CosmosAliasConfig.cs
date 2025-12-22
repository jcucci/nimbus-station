namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for a CosmosDB connection alias.
/// </summary>
/// <param name="Endpoint">The CosmosDB account endpoint URL.</param>
/// <param name="Database">The database name.</param>
/// <param name="Container">The container name.</param>
public sealed record CosmosAliasConfig(string Endpoint, string Database, string Container);
