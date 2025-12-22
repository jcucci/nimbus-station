namespace NimbusStation.Infrastructure.Aliases;

/// <summary>
/// Represents the result of alias name validation.
/// </summary>
public sealed record AliasNameValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the alias name is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the error message if validation failed, or null if valid.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A valid result.</returns>
    public static AliasNameValidationResult Valid() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>An invalid result.</returns>
    public static AliasNameValidationResult Invalid(string errorMessage) =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}
