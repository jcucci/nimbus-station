namespace NimbusStation.Providers.Azure.Auth;

/// <summary>
/// Represents the current Azure authentication status.
/// </summary>
public sealed record AzureAuthStatus
{
    /// <summary>
    /// Gets whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>
    /// Gets the authenticated user's identity (email or service principal).
    /// </summary>
    public string? Identity { get; init; }

    /// <summary>
    /// Gets the active subscription name.
    /// </summary>
    public string? SubscriptionName { get; init; }

    /// <summary>
    /// Gets the active subscription ID.
    /// </summary>
    public string? SubscriptionId { get; init; }

    /// <summary>
    /// Gets the tenant ID.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets the error message if authentication check failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets whether the Azure CLI is installed.
    /// </summary>
    public bool IsCliInstalled { get; init; }

    /// <summary>
    /// Gets the Azure CLI version.
    /// </summary>
    public string? CliVersion { get; init; }

    /// <summary>
    /// Creates an authenticated status.
    /// </summary>
    public static AzureAuthStatus Authenticated(
        string identity,
        string subscriptionName,
        string subscriptionId,
        string tenantId,
        string? cliVersion = null) =>
        new()
        {
            IsAuthenticated = true,
            Identity = identity,
            SubscriptionName = subscriptionName,
            SubscriptionId = subscriptionId,
            TenantId = tenantId,
            IsCliInstalled = true,
            CliVersion = cliVersion
        };

    /// <summary>
    /// Creates a not authenticated status.
    /// </summary>
    public static AzureAuthStatus NotAuthenticated(string errorMessage, string? cliVersion = null) =>
        new()
        {
            IsAuthenticated = false,
            ErrorMessage = errorMessage,
            IsCliInstalled = true,
            CliVersion = cliVersion
        };

    /// <summary>
    /// Creates a status indicating CLI is not installed.
    /// </summary>
    public static AzureAuthStatus CliNotInstalled() =>
        new()
        {
            IsAuthenticated = false,
            IsCliInstalled = false,
            ErrorMessage = "Azure CLI not found. Install from https://docs.microsoft.com/cli/azure/install-azure-cli"
        };
}
