namespace NimbusStation.Infrastructure.Configuration.Generators;

/// <summary>
/// Represents a dimension entry used in alias generation.
/// A dimension has a key (e.g., "ninja") and arbitrary properties (e.g., abbrev, key).
/// </summary>
public sealed class GeneratorDimension
{
    /// <summary>
    /// The dimension key used in template substitution (e.g., "ninja", "invoices").
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Additional properties defined for this dimension entry.
    /// These become available as template variables (e.g., {abbrev}, {database}).
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        new Dictionary<string, string>();
}
