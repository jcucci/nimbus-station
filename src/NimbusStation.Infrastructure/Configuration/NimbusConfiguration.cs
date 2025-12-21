namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Represents the application configuration loaded from TOML.
/// </summary>
public class NimbusConfiguration
{
    /// <summary>
    /// Gets or sets the default cloud provider identifier.
    /// </summary>
    public string? DefaultProvider { get; set; }
}
