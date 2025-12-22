using System.Text.RegularExpressions;

namespace NimbusStation.Core.Session;

/// <summary>
/// Validates session names to ensure they are valid directory names across all operating systems.
/// </summary>
public static partial class SessionNameValidator
{
    /// <summary>
    /// Maximum allowed length for a session name.
    /// </summary>
    public const int MaxLength = 255;

    /// <summary>
    /// Characters that are not allowed in session names.
    /// </summary>
    private static readonly char[] InvalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    /// <summary>
    /// Reserved names on Windows that cannot be used as directory names.
    /// </summary>
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// Validates a session name and returns whether it is valid.
    /// </summary>
    /// <param name="name">The session name to validate.</param>
    /// <param name="errorMessage">The error message if validation fails, null otherwise.</param>
    /// <returns>True if the name is valid, false otherwise.</returns>
    public static bool IsValid(string? name, out string? errorMessage)
    {
        // Check for null or empty
        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = "Session name cannot be empty.";
            return false;
        }

        // Check length
        if (name.Length > MaxLength)
        {
            errorMessage = $"Session name cannot exceed {MaxLength} characters.";
            return false;
        }

        // Check for leading/trailing whitespace
        if (name != name.Trim())
        {
            errorMessage = "Session name cannot have leading or trailing whitespace.";
            return false;
        }

        // Check for leading/trailing dots
        if (name.StartsWith('.') || name.EndsWith('.'))
        {
            errorMessage = "Session name cannot start or end with a dot.";
            return false;
        }

        // Check for invalid characters
        var invalidCharIndex = name.IndexOfAny(InvalidChars);
        if (invalidCharIndex >= 0)
        {
            errorMessage = $"Session name cannot contain '{name[invalidCharIndex]}' character.";
            return false;
        }

        // Check for control characters
        if (name.Any(char.IsControl))
        {
            errorMessage = "Session name cannot contain control characters.";
            return false;
        }

        // Check for reserved Windows names (with or without extension)
        var nameWithoutExtension = GetNameWithoutExtension(name);
        if (ReservedNames.Contains(nameWithoutExtension))
        {
            errorMessage = $"Session name '{nameWithoutExtension}' is reserved on Windows.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Validates a session name and throws an exception if invalid.
    /// </summary>
    /// <param name="name">The session name to validate.</param>
    /// <exception cref="InvalidSessionNameException">Thrown if the name is invalid.</exception>
    public static void Validate(string? name)
    {
        if (!IsValid(name, out var errorMessage))
        {
            throw new InvalidSessionNameException(name ?? string.Empty, errorMessage!);
        }
    }

    /// <summary>
    /// Extracts the name without extension for reserved name checking.
    /// </summary>
    private static string GetNameWithoutExtension(string name)
    {
        var dotIndex = name.IndexOf('.');
        return dotIndex >= 0 ? name[..dotIndex] : name;
    }
}
