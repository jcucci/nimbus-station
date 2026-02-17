using NimbusStation.Infrastructure.Configuration.Generators;

namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Service for loading and managing application configuration from TOML files.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads configuration from the default configuration path (~/.config/nimbus/config.toml).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The loaded configuration.</returns>
    Task<NimbusConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a CosmosDB alias configuration by name.
    /// </summary>
    /// <param name="name">The alias name.</param>
    /// <returns>The alias configuration, or null if not found.</returns>
    CosmosAliasConfig? GetCosmosAlias(string name);

    /// <summary>
    /// Gets a Blob storage alias configuration by name.
    /// </summary>
    /// <param name="name">The alias name.</param>
    /// <returns>The alias configuration, or null if not found.</returns>
    BlobAliasConfig? GetBlobAlias(string name);

    /// <summary>
    /// Gets a Storage account alias configuration by name.
    /// </summary>
    /// <param name="name">The alias name.</param>
    /// <returns>The alias configuration, or null if not found.</returns>
    StorageAliasConfig? GetStorageAlias(string name);

    /// <summary>
    /// Gets the current theme configuration.
    /// </summary>
    /// <returns>The theme configuration.</returns>
    ThemeConfig GetTheme();

    /// <summary>
    /// Gets all CosmosDB alias configurations.
    /// </summary>
    /// <returns>All Cosmos alias configurations keyed by name.</returns>
    IReadOnlyDictionary<string, CosmosAliasConfig> GetAllCosmosAliases();

    /// <summary>
    /// Gets all Blob storage alias configurations.
    /// </summary>
    /// <returns>All Blob alias configurations keyed by name.</returns>
    IReadOnlyDictionary<string, BlobAliasConfig> GetAllBlobAliases();

    /// <summary>
    /// Gets all Storage account alias configurations.
    /// </summary>
    /// <returns>All Storage alias configurations keyed by name.</returns>
    IReadOnlyDictionary<string, StorageAliasConfig> GetAllStorageAliases();

    /// <summary>
    /// Gets the generators configuration, or null if none configured.
    /// </summary>
    /// <returns>The generators configuration, or null.</returns>
    GeneratorsConfig? GetGeneratorsConfig();
}
