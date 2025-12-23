using NimbusStation.Infrastructure.Configuration;

namespace NimbusStation.Tests.Helpers;

/// <summary>
/// Stub implementation of <see cref="IConfigurationService"/> for testing.
/// </summary>
public sealed class StubConfigurationService : IConfigurationService
{
    private readonly Dictionary<string, CosmosAliasConfig> _cosmosAliases = new();
    private readonly Dictionary<string, BlobAliasConfig> _blobAliases = new();

    /// <summary>
    /// Adds a Cosmos alias configuration for testing.
    /// </summary>
    public void AddCosmosAlias(string name, CosmosAliasConfig config) => _cosmosAliases[name] = config;

    /// <summary>
    /// Adds a Blob alias configuration for testing.
    /// </summary>
    public void AddBlobAlias(string name, BlobAliasConfig config) => _blobAliases[name] = config;

    /// <inheritdoc/>
    public Task<NimbusConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new NimbusConfiguration());

    /// <inheritdoc/>
    public CosmosAliasConfig? GetCosmosAlias(string name)
        => _cosmosAliases.GetValueOrDefault(name);

    /// <inheritdoc/>
    public BlobAliasConfig? GetBlobAlias(string name)
        => _blobAliases.GetValueOrDefault(name);

    /// <inheritdoc/>
    public ThemeConfig GetTheme() => ThemeConfig.Default;
}
