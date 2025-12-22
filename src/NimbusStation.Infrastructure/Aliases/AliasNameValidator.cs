using System.Text.RegularExpressions;

namespace NimbusStation.Infrastructure.Aliases;

/// <summary>
/// Validates command alias names.
/// </summary>
public static partial class AliasNameValidator
{
    /// <summary>
    /// Reserved command names that cannot be used as aliases.
    /// </summary>
    public static readonly IReadOnlySet<string> ReservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "session",
        "alias",
        "help",
        "exit",
        "quit"
    };

    /// <summary>
    /// Validates an alias name.
    /// </summary>
    /// <param name="name">The alias name to validate.</param>
    /// <returns>A validation result indicating success or the error message.</returns>
    public static AliasNameValidationResult Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return AliasNameValidationResult.Invalid("Alias name cannot be empty");
        }

        if (ReservedNames.Contains(name))
        {
            return AliasNameValidationResult.Invalid($"Cannot create alias '{name}': reserved command name");
        }

        if (!ValidNamePattern().IsMatch(name))
        {
            return AliasNameValidationResult.Invalid(
                $"Invalid alias name '{name}': must start with a letter and contain only letters, numbers, hyphens, and underscores");
        }

        return AliasNameValidationResult.Valid();
    }

    /// <summary>
    /// Checks if the specified name is a reserved command.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the name is reserved; otherwise, false.</returns>
    public static bool IsReserved(string name) => ReservedNames.Contains(name);

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_-]*$", RegexOptions.Compiled)]
    private static partial Regex ValidNamePattern();
}
