using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using NimbusStation.Infrastructure.Configuration;

namespace NimbusStation.Providers.Azure.Cosmos;

/// <summary>
/// Service for executing queries against Azure Cosmos DB using the SDK.
/// Caches CosmosClient instances per endpoint for connection reuse.
/// </summary>
public sealed class CosmosService : ICosmosService, IDisposable
{
    private readonly IConfigurationService _configurationService;
    private readonly ConcurrentDictionary<string, CosmosClient> _clients = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosService"/> class.
    /// </summary>
    /// <param name="configurationService">The configuration service for resolving aliases.</param>
    public CosmosService(IConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
    }

    /// <inheritdoc/>
    public async Task<CosmosQueryResult> ExecuteQueryAsync(
        string aliasName,
        string sql,
        int maxItems = 100,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var aliasConfig = _configurationService.GetCosmosAlias(aliasName)
            ?? throw new InvalidOperationException($"Cosmos alias '{aliasName}' not found in config.");

        var client = GetOrCreateClient(aliasConfig);
        var container = client.GetContainer(aliasConfig.Database, aliasConfig.Container);

        var queryDefinition = new QueryDefinition(sql);
        var requestOptions = new QueryRequestOptions { MaxItemCount = maxItems };

        var items = new List<JsonElement>();
        double totalRequestCharge = 0;
        bool hasMoreResults = false;

        using var iterator = container.GetItemQueryIterator<JsonElement>(
            queryDefinition,
            requestOptions: requestOptions);

        while (iterator.HasMoreResults && items.Count < maxItems)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            totalRequestCharge += response.RequestCharge;

            foreach (var item in response)
            {
                if (items.Count >= maxItems)
                {
                    hasMoreResults = true;
                    break;
                }
                items.Add(item);
            }

            hasMoreResults = iterator.HasMoreResults;
        }

        return new CosmosQueryResult(
            Items: items,
            RequestCharge: totalRequestCharge,
            HasMoreResults: hasMoreResults);
    }

    private CosmosClient GetOrCreateClient(CosmosAliasConfig aliasConfig)
    {
        return _clients.GetOrAdd(aliasConfig.Endpoint, endpoint =>
        {
            var key = ResolveKey(aliasConfig);
            var options = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            return new CosmosClient(endpoint, key, options);
        });
    }

    private static string ResolveKey(CosmosAliasConfig aliasConfig)
    {
        if (string.IsNullOrWhiteSpace(aliasConfig.KeyEnv))
        {
            throw new InvalidOperationException(
                $"No key_env configured for cosmos alias. Add key_env to your config.");
        }

        var key = Environment.GetEnvironmentVariable(aliasConfig.KeyEnv);

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                $"Set environment variable {aliasConfig.KeyEnv} with your Cosmos DB read-only key.");
        }

        return key;
    }

    /// <summary>
    /// Disposes all cached CosmosClient instances.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        _clients.Clear();
        _disposed = true;
    }
}
