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
    /// Gets the current theme configuration.
    /// </summary>
    /// <returns>The theme configuration.</returns>
    ThemeConfig GetTheme();
}
