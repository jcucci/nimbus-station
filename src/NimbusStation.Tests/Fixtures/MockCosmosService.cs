using System.Text.Json;
using NimbusStation.Providers.Azure.Cosmos;

namespace NimbusStation.Tests.Fixtures;

/// <summary>
/// Mock implementation of <see cref="ICosmosService"/> for testing.
/// </summary>
public sealed class MockCosmosService : ICosmosService
{
    private CosmosQueryResult? _nextResult;
    private Exception? _nextException;
    private readonly List<(string AliasName, string Sql, int MaxItems)> _queryCalls = [];

    /// <summary>
    /// Gets the list of query calls made to this mock.
    /// </summary>
    public IReadOnlyList<(string AliasName, string Sql, int MaxItems)> QueryCalls => _queryCalls;

    /// <summary>
    /// Sets up the mock to return the specified result on the next query.
    /// </summary>
    /// <param name="result">The result to return.</param>
    public void SetupQueryResult(CosmosQueryResult result)
    {
        _nextResult = result;
        _nextException = null;
    }

    /// <summary>
    /// Sets up the mock to throw the specified exception on the next query.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    public void SetupQueryException(Exception exception)
    {
        _nextException = exception;
        _nextResult = null;
    }

    /// <summary>
    /// Creates an empty query result for testing.
    /// </summary>
    /// <returns>An empty CosmosQueryResult.</returns>
    public static CosmosQueryResult CreateEmptyResult() =>
        new(Items: [], RequestCharge: 0, HasMoreResults: false);

    /// <summary>
    /// Creates a query result with the specified items for testing.
    /// </summary>
    /// <param name="items">The JSON items to include.</param>
    /// <param name="requestCharge">The request charge to include.</param>
    /// <param name="hasMoreResults">Whether there are more results.</param>
    /// <returns>A CosmosQueryResult with the specified items.</returns>
    public static CosmosQueryResult CreateResult(
        IEnumerable<object> items,
        double requestCharge = 3.42,
        bool hasMoreResults = false)
    {
        var jsonItems = items
            .Select(item => JsonSerializer.SerializeToElement(item))
            .ToList();

        return new CosmosQueryResult(
            Items: jsonItems,
            RequestCharge: requestCharge,
            HasMoreResults: hasMoreResults);
    }

    /// <inheritdoc/>
    public Task<CosmosQueryResult> ExecuteQueryAsync(
        string aliasName,
        string sql,
        int maxItems = 100,
        CancellationToken cancellationToken = default)
    {
        _queryCalls.Add((aliasName, sql, maxItems));

        if (_nextException is not null)
            throw _nextException;

        return Task.FromResult(_nextResult ?? CreateEmptyResult());
    }
}
