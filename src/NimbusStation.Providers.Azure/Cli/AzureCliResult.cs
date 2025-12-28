namespace NimbusStation.Providers.Azure.Cli;

/// <summary>
/// Result of an Azure CLI command execution.
/// </summary>
public sealed record AzureCliResult
{
    /// <summary>
    /// Gets whether the command executed successfully (exit code 0).
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the standard output from the command.
    /// </summary>
    public string StandardOutput { get; init; } = string.Empty;

    /// <summary>
    /// Gets the standard error from the command.
    /// </summary>
    public string StandardError { get; init; } = string.Empty;

    /// <summary>
    /// Gets the exit code from the command.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Gets the error message if the command failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static AzureCliResult Succeeded(string stdout, string stderr = "", int exitCode = 0) =>
        new() { Success = true, StandardOutput = stdout, StandardError = stderr, ExitCode = exitCode };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static AzureCliResult Failed(string errorMessage, string stderr = "", int exitCode = 1) =>
        new() { Success = false, ErrorMessage = errorMessage, StandardError = stderr, ExitCode = exitCode };

    /// <summary>
    /// Creates a result indicating the CLI is not installed.
    /// </summary>
    public static AzureCliResult NotInstalled() =>
        new() { Success = false, ErrorMessage = "Azure CLI not found. Install from https://aka.ms/installazurecli", ExitCode = -1 };
}
