namespace NimbusStation.Providers.Azure.Auth;

/// <summary>
/// Provides Azure authentication status and login functionality.
/// </summary>
public interface IAzureAuthService
{
    /// <summary>
    /// Gets the current Azure authentication status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current authentication status.</returns>
    Task<AzureAuthStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates interactive login via the Azure CLI.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authentication status after login attempt.</returns>
    Task<AzureAuthStatus> LoginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the installed Azure CLI version.
    /// </summary>
    /// <returns>The version string, or null if not installed.</returns>
    Task<string?> GetCliVersionAsync();

    /// <summary>
    /// Checks if the Azure CLI is installed.
    /// </summary>
    /// <returns>True if installed, false otherwise.</returns>
    Task<bool> IsCliInstalledAsync();
}
