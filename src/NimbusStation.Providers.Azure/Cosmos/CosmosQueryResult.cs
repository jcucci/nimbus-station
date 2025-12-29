using System.Text.Json;

namespace NimbusStation.Providers.Azure.Cosmos;

/// <summary>
/// Represents the result of a Cosmos DB query execution.
/// </summary>
/// <param name="Items">The query result items as JSON elements.</param>
/// <param name="RequestCharge">The total request units (RU) consumed by the query.</param>
/// <param name="HasMoreResults">Whether there are more results available beyond the returned items.</param>
/// <param name="ContinuationToken">Token for continuing pagination, if available.</param>
public sealed record CosmosQueryResult(
    IReadOnlyList<JsonElement> Items,
    double RequestCharge,
    bool HasMoreResults,
    string? ContinuationToken = null);
