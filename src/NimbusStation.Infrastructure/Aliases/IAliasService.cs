namespace NimbusStation.Infrastructure.Aliases;

/// <summary>
/// Service for managing command aliases stored in aliases.toml.
/// </summary>
public interface IAliasService
{
    /// <summary>
    /// Loads all aliases from the configuration file.
    /// Creates a default file if it doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The aliases configuration.</returns>
    Task<AliasesConfiguration> LoadAliasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the expansion template for the specified alias.
    /// </summary>
    /// <param name="name">The alias name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The expansion template, or null if the alias doesn't exist.</returns>
    Task<string?> GetAliasAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates an alias.
    /// </summary>
    /// <param name="name">The alias name.</param>
    /// <param name="expansion">The expansion template.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task AddAliasAsync(string name, string expansion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an alias.
    /// </summary>
    /// <param name="name">The alias name to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the alias was removed; false if it didn't exist.</returns>
    Task<bool> RemoveAliasAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently loaded aliases.
    /// </summary>
    /// <returns>A read-only dictionary of alias names to expansion templates.</returns>
    IReadOnlyDictionary<string, string> GetAllAliases();

    /// <summary>
    /// Reloads aliases from the configuration file, clearing any cached state.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The reloaded aliases configuration.</returns>
    Task<AliasesConfiguration> ReloadAliasesAsync(CancellationToken cancellationToken = default);
}
