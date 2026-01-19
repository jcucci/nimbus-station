namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for included files.
/// </summary>
public sealed class IncludeConfig
{
    /// <summary>
    /// Gets or sets the list of files to include.
    /// Paths can be absolute, relative to the including file, or start with ~ for home directory.
    /// </summary>
    public List<string> Files { get; set; } = [];
}
