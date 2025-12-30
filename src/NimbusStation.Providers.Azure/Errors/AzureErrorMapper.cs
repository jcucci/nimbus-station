using System.Net;
using Microsoft.Azure.Cosmos;
using NimbusStation.Core.Errors;

namespace NimbusStation.Providers.Azure.Errors;

/// <summary>
/// Maps Azure SDK exceptions to user-facing errors with helpful messages and suggestions.
/// </summary>
public static class AzureErrorMapper
{
    /// <summary>
    /// Maps a CosmosException to a user-facing error.
    /// </summary>
    /// <param name="ex">The Cosmos exception.</param>
    /// <param name="endpoint">Optional endpoint URL for additional context.</param>
    /// <param name="aliasName">Optional alias name for additional context.</param>
    public static UserFacingError FromCosmosException(
        CosmosException ex,
        string? endpoint = null,
        string? aliasName = null)
    {
        var details = BuildDetails(endpoint, aliasName, ex.Message);

        return ex.StatusCode switch
        {
            HttpStatusCode.Unauthorized => UserFacingError.Authentication(
                "Authentication failed for Cosmos DB",
                details),

            HttpStatusCode.Forbidden => new UserFacingError(
                ErrorCategory.Authentication,
                "Access denied to Cosmos DB resource",
                details,
                new[]
                {
                    "Verify your account has the required permissions",
                    "Check that the key_env variable is set correctly",
                    "Run 'auth status' to verify authentication"
                }),

            HttpStatusCode.NotFound => UserFacingError.NotFound(
                "Cosmos DB resource not found",
                details),

            (HttpStatusCode)429 => MapThrottlingError(ex),

            HttpStatusCode.BadRequest => new UserFacingError(
                ErrorCategory.General,
                "Invalid Cosmos DB request",
                details,
                new[]
                {
                    "Check your SQL query syntax",
                    "Verify the partition key value if provided"
                }),

            HttpStatusCode.ServiceUnavailable => new UserFacingError(
                ErrorCategory.Network,
                "Cosmos DB service is temporarily unavailable",
                details,
                new[]
                {
                    "Wait a moment and try again",
                    "Check the Azure status page for any outages"
                }),

            HttpStatusCode.RequestTimeout => UserFacingError.Network(
                "Request to Cosmos DB timed out",
                details),

            _ => UserFacingError.General(
                $"Cosmos DB error: {ex.StatusCode}",
                details)
        };
    }

    /// <summary>
    /// Maps an InvalidOperationException (typically from configuration errors) to a user-facing error.
    /// </summary>
    public static UserFacingError FromInvalidOperation(InvalidOperationException ex, string? aliasName = null)
    {
        var message = ex.Message;

        // Check for common patterns
        if (message.Contains("not found", StringComparison.OrdinalIgnoreCase) &&
            message.Contains("alias", StringComparison.OrdinalIgnoreCase))
        {
            return UserFacingError.Configuration(
                aliasName is not null
                    ? $"Alias '{aliasName}' not found in configuration"
                    : "Alias not found in configuration",
                null);
        }

        if (message.Contains("environment variable", StringComparison.OrdinalIgnoreCase))
        {
            return new UserFacingError(
                ErrorCategory.Configuration,
                message,
                null,
                new[]
                {
                    "Set the required environment variable",
                    "Check your alias configuration for the correct key_env value"
                });
        }

        return UserFacingError.Configuration(message);
    }

    /// <summary>
    /// Maps a general exception to a user-facing error, attempting to categorize it.
    /// </summary>
    public static UserFacingError FromException(Exception ex, string? endpoint = null)
    {
        // Handle specific exception types
        if (ex is CosmosException cosmosEx)
            return FromCosmosException(cosmosEx, endpoint);

        if (ex is InvalidOperationException invalidOpEx)
            return FromInvalidOperation(invalidOpEx);

        if (ex is OperationCanceledException)
            return UserFacingError.Cancelled();

        // Check for network-related exceptions
        if (IsNetworkException(ex))
        {
            var details = endpoint is not null ? $"Endpoint: {endpoint}" : null;
            return UserFacingError.Network(
                "Failed to connect to Azure service",
                details);
        }

        // Default to general error
        return UserFacingError.General(ex.Message);
    }

    /// <summary>
    /// Maps an Azure CLI error message to a user-facing error.
    /// </summary>
    public static UserFacingError FromCliError(string? errorMessage, string? resourceName = null)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return UserFacingError.General("Azure CLI command failed");

        var message = errorMessage.Trim();

        // Authentication errors
        if (message.Contains("az login", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("not logged in", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("AADSTS", StringComparison.OrdinalIgnoreCase))
        {
            return UserFacingError.Authentication("Not authenticated with Azure CLI");
        }

        // Authorization errors
        if (message.Contains("AuthorizationFailed", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("does not have authorization", StringComparison.OrdinalIgnoreCase))
        {
            return new UserFacingError(
                ErrorCategory.Authentication,
                "Access denied",
                resourceName is not null ? $"Resource: {resourceName}" : null,
                new[]
                {
                    "Verify your account has the required permissions",
                    "Check that you're logged into the correct Azure subscription"
                });
        }

        // Not found errors
        if (message.Contains("ResourceNotFound", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
        {
            return UserFacingError.NotFound(
                "Azure resource not found",
                resourceName is not null ? $"Resource: {resourceName}" : null);
        }

        // Network errors
        if (message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return UserFacingError.Network("Azure CLI network error", message);
        }

        // Default
        return UserFacingError.General(message);
    }

    private static UserFacingError MapThrottlingError(CosmosException ex)
    {
        TimeSpan? retryAfter = null;

        // Try to get retry-after from headers
        if (ex.RetryAfter.HasValue)
            retryAfter = ex.RetryAfter.Value;

        return UserFacingError.Throttling(
            "Request was throttled by Cosmos DB",
            retryAfter);
    }

    private static string? BuildDetails(string? endpoint, string? aliasName, string? exceptionMessage)
    {
        var parts = new List<string>();

        if (endpoint is not null)
            parts.Add($"Endpoint: {endpoint}");

        if (aliasName is not null)
            parts.Add($"Alias: {aliasName}");

        // Add a condensed version of the exception message if it contains useful info
        if (exceptionMessage is not null && !exceptionMessage.Contains("StatusCode"))
        {
            var condensed = exceptionMessage.Length > 100
                ? exceptionMessage[..100] + "..."
                : exceptionMessage;
            parts.Add($"Reason: {condensed}");
        }

        return parts.Count > 0 ? string.Join(Environment.NewLine + "       ", parts) : null;
    }

    private static bool IsNetworkException(Exception ex)
    {
        var exType = ex.GetType().Name;
        var message = ex.Message.ToLowerInvariant();

        return exType.Contains("Socket") ||
               exType.Contains("Http") ||
               exType.Contains("Network") ||
               message.Contains("network") ||
               message.Contains("connection") ||
               message.Contains("could not be resolved") ||
               message.Contains("unable to connect") ||
               message.Contains("timeout");
    }
}
