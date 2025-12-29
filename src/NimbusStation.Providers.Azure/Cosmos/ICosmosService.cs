namespace NimbusStation.Providers.Azure.Cosmos;

/// <summary>
/// Service for executing queries against Azure Cosmos DB.
/// </summary>
public interface ICosmosService
{
    /// <summary>
    /// Executes a SQL query against the Cosmos DB container configured for the specified alias.
    /// </summary>
    /// <param name="aliasName">The name of the configured cosmos alias.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="maxItems">Maximum number of items to return. Defaults to 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The query result containing items and metadata.</returns>
    Task<CosmosQueryResult> ExecuteQueryAsync(
        string aliasName,
        string sql,
        int maxItems = 100,
        CancellationToken cancellationToken = default);
}
