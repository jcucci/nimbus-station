namespace NimbusStation.Core.Aliases;

/// <summary>
/// Represents the result of an alias expansion attempt.
/// </summary>
public sealed record AliasExpansionResult
{
    /// <summary>
    /// Gets a value indicating whether the input was expanded from an alias.
    /// </summary>
    public bool WasExpanded { get; init; }

    /// <summary>
    /// Gets the expanded input string (or the original input if no expansion occurred).
    /// </summary>
    public string ExpandedInput { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error message if expansion failed, or null if successful.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets a value indicating whether the expansion was successful (no error).
    /// </summary>
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Creates a result indicating the input was not an alias and was not expanded.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <returns>A result with no expansion.</returns>
    public static AliasExpansionResult NoExpansion(string input) =>
        new() { WasExpanded = false, ExpandedInput = input };

    /// <summary>
    /// Creates a result indicating successful alias expansion.
    /// </summary>
    /// <param name="expandedInput">The expanded input string.</param>
    /// <returns>A successful expansion result.</returns>
    public static AliasExpansionResult Expanded(string expandedInput) =>
        new() { WasExpanded = true, ExpandedInput = expandedInput };

    /// <summary>
    /// Creates a result indicating expansion failed with an error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed expansion result.</returns>
    public static AliasExpansionResult Failed(string error) =>
        new() { WasExpanded = false, Error = error };
}
