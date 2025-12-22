namespace NimbusStation.Infrastructure.Aliases;

/// <summary>
/// Represents the command aliases configuration loaded from aliases.toml.
/// </summary>
/// <param name="Aliases">A dictionary mapping alias names to their expansion templates.</param>
public sealed record AliasesConfiguration(IReadOnlyDictionary<string, string> Aliases)
{
    /// <summary>
    /// Gets an empty configuration with no aliases.
    /// </summary>
    public static AliasesConfiguration Empty => new(new Dictionary<string, string>());

    /// <summary>
    /// Tries to get the expansion template for the specified alias name.
    /// </summary>
    /// <param name="name">The alias name.</param>
    /// <param name="expansion">The expansion template, if found.</param>
    /// <returns>True if the alias was found; otherwise, false.</returns>
    public bool TryGetAlias(string name, out string? expansion)
    {
        return Aliases.TryGetValue(name, out expansion);
    }

    /// <summary>
    /// Checks whether an alias with the specified name exists.
    /// </summary>
    /// <param name="name">The alias name.</param>
    /// <returns>True if the alias exists; otherwise, false.</returns>
    public bool HasAlias(string name) => Aliases.ContainsKey(name);
}
