using NimbusStation.Core.Session;

namespace NimbusStation.Core.Aliases;

/// <summary>
/// Provides alias expansion for REPL input.
/// </summary>
public interface IAliasResolver
{
    /// <summary>
    /// Expands any command alias in the input string.
    /// </summary>
    /// <param name="input">The raw input from the REPL.</param>
    /// <param name="currentSession">The current session, if any (used for built-in variables).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The expansion result containing the expanded input or an error.</returns>
    Task<AliasExpansionResult> ExpandAsync(
        string input,
        Session.Session? currentSession,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests alias expansion without executing the result.
    /// Returns detailed information about the expansion process.
    /// </summary>
    /// <param name="aliasName">The alias name to test.</param>
    /// <param name="arguments">The arguments to pass to the alias.</param>
    /// <param name="currentSession">The current session, if any.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The expansion result.</returns>
    Task<AliasExpansionResult> TestExpandAsync(
        string aliasName,
        string[] arguments,
        Session.Session? currentSession,
        CancellationToken cancellationToken = default);
}
