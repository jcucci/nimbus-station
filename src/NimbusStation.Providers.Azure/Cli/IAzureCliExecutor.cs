namespace NimbusStation.Providers.Azure.Cli;

/// <summary>
/// Executes Azure CLI commands and parses their output.
/// </summary>
public interface IAzureCliExecutor
{
    /// <summary>
    /// Executes an Azure CLI command and returns the raw output.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the az command (e.g., "account show").</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Defaults to 30000 (30 seconds).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the CLI execution.</returns>
    Task<AzureCliResult> ExecuteAsync(
        string arguments,
        int timeoutMs = 30000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an Azure CLI command and parses the JSON output.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON output to.</typeparam>
    /// <param name="arguments">The arguments to pass to the az command.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Defaults to 30000 (30 seconds).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed result or an error.</returns>
    Task<AzureCliResult<T>> ExecuteJsonAsync<T>(
        string arguments,
        int timeoutMs = 30000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the Azure CLI is installed.
    /// </summary>
    /// <returns>True if az CLI is available, false otherwise.</returns>
    Task<bool> IsInstalledAsync();

    /// <summary>
    /// Gets the installed Azure CLI version.
    /// </summary>
    /// <returns>The version string, or null if not installed.</returns>
    Task<string?> GetVersionAsync();
}
