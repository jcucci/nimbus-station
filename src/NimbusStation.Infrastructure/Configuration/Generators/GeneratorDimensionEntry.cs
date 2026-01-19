namespace NimbusStation.Infrastructure.Configuration.Generators;

/// <summary>
/// A single entry in a generator dimension (e.g., a single kingdom or backend).
/// Contains properties that can be referenced in templates.
/// </summary>
public sealed class GeneratorDimensionEntry
{
    /// <summary>
    /// Gets or sets the name of this entry (used as the key in the dimension).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key/value pairs for this entry.
    /// These values can be referenced in templates using {dimension_property} syntax.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = [];
}
