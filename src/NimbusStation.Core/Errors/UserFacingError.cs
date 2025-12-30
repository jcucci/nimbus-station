namespace NimbusStation.Core.Errors;

/// <summary>
/// A structured error with category, message, optional details, and actionable suggestions.
/// </summary>
/// <param name="Category">The error category for classification and exit code mapping.</param>
/// <param name="Message">The primary error message.</param>
/// <param name="Details">Optional additional details (e.g., endpoint URL, resource name).</param>
/// <param name="Suggestions">Optional list of actionable suggestions for the user.</param>
public record UserFacingError(
    ErrorCategory Category,
    string Message,
    string? Details = null,
    IReadOnlyList<string>? Suggestions = null)
{
    /// <summary>
    /// Creates a general error with a message.
    /// </summary>
    public static UserFacingError General(string message, string? details = null) =>
        new(ErrorCategory.General, message, details);

    /// <summary>
    /// Creates an authentication error with standard suggestions.
    /// </summary>
    public static UserFacingError Authentication(string message, string? details = null) =>
        new(ErrorCategory.Authentication, message, details, new[]
        {
            "Run 'az login' to authenticate via Azure CLI",
            "Or set environment variables: AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TENANT_ID",
            "Run 'auth status' for more details"
        });

    /// <summary>
    /// Creates a configuration error with standard suggestions.
    /// </summary>
    public static UserFacingError Configuration(string message, string? details = null) =>
        new(ErrorCategory.Configuration, message, details, new[]
        {
            "Check your configuration file at ~/.config/nimbus/config.toml",
            "Run 'info' to see current configuration"
        });

    /// <summary>
    /// Creates a network error with standard suggestions.
    /// </summary>
    public static UserFacingError Network(string message, string? details = null) =>
        new(ErrorCategory.Network, message, details, new[]
        {
            "Check your network connection",
            "Verify the endpoint URL in your config",
            "Run 'auth status' to verify authentication"
        });

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static UserFacingError NotFound(string message, string? details = null) =>
        new(ErrorCategory.NotFound, message, details, new[]
        {
            "Verify the resource name is correct",
            "Check that you have access to the resource"
        });

    /// <summary>
    /// Creates a throttling error with retry information.
    /// </summary>
    public static UserFacingError Throttling(string message, TimeSpan? retryAfter = null)
    {
        var suggestions = new List<string>();
        if (retryAfter.HasValue)
            suggestions.Add($"Wait {retryAfter.Value.TotalSeconds:F0} seconds before retrying");
        suggestions.Add("Reduce query frequency");
        suggestions.Add("Consider adding filters to reduce result size");

        return new(ErrorCategory.Throttling, message, null, suggestions);
    }

    /// <summary>
    /// Creates a cancelled error (user cancellation).
    /// </summary>
    public static UserFacingError Cancelled(string message = "Operation cancelled") =>
        new(ErrorCategory.Cancelled, message);
}
