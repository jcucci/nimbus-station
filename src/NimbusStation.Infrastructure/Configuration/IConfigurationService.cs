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
}
