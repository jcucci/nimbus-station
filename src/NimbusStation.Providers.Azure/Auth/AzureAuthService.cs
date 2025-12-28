using NimbusStation.Providers.Azure.Cli;

namespace NimbusStation.Providers.Azure.Auth;

/// <summary>
/// Provides Azure authentication status and login functionality via the Azure CLI.
/// </summary>
public sealed class AzureAuthService : IAzureAuthService
{
    private readonly IAzureCliExecutor _cliExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAuthService"/> class.
    /// </summary>
    /// <param name="cliExecutor">The Azure CLI executor.</param>
    public AzureAuthService(IAzureCliExecutor cliExecutor) =>
        _cliExecutor = cliExecutor ?? throw new ArgumentNullException(nameof(cliExecutor));

    /// <inheritdoc/>
    public async Task<AzureAuthStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!await _cliExecutor.IsInstalledAsync())
            return AzureAuthStatus.CliNotInstalled();

        var cliVersion = await _cliExecutor.GetVersionAsync();
        var result = await _cliExecutor.ExecuteJsonAsync<AzureAccountInfo>(
            arguments: "account show",
            cancellationToken: cancellationToken);

        if (!result.Success || result.Data is null)
        {
            var errorMessage = result.ErrorMessage ?? "Not authenticated. Run 'auth login' or 'az login' to authenticate.";
            return AzureAuthStatus.NotAuthenticated(errorMessage, cliVersion);
        }

        var account = result.Data;
        return AzureAuthStatus.Authenticated(
            identity: account.User?.Name ?? "Unknown",
            subscriptionName: account.Name ?? "Unknown",
            subscriptionId: account.Id ?? "Unknown",
            tenantId: account.TenantId ?? "Unknown",
            cliVersion: cliVersion);
    }

    /// <inheritdoc/>
    public async Task<AzureAuthStatus> LoginAsync(CancellationToken cancellationToken = default)
    {
        if (!await _cliExecutor.IsInstalledAsync())
            return AzureAuthStatus.CliNotInstalled();

        var cliVersion = await _cliExecutor.GetVersionAsync();

        // Run az login interactively - this opens a browser
        var result = await _cliExecutor.ExecuteAsync(
            arguments: "login",
            timeoutMs: 300000, // 5 minute timeout for interactive login
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            return AzureAuthStatus.NotAuthenticated(
                result.ErrorMessage ?? "Login failed",
                cliVersion);
        }

        // After successful login, get the current account status
        return await GetStatusAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<string?> GetCliVersionAsync() =>
        _cliExecutor.GetVersionAsync();

    /// <inheritdoc/>
    public Task<bool> IsCliInstalledAsync() =>
        _cliExecutor.IsInstalledAsync();
}
