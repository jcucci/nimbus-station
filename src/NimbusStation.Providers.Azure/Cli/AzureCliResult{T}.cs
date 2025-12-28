namespace NimbusStation.Providers.Azure.Cli;

/// <summary>
/// Result of an Azure CLI command execution with parsed JSON output.
/// </summary>
/// <typeparam name="T">The type of the parsed data.</typeparam>
public sealed record AzureCliResult<T>
{
    /// <summary>
    /// Gets whether the command executed successfully and the output was parsed.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the parsed data from the JSON output.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Gets the error message if the command or parsing failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the raw CLI result.
    /// </summary>
    public AzureCliResult? RawResult { get; init; }

    /// <summary>
    /// Creates a successful result with parsed data.
    /// </summary>
    public static AzureCliResult<T> Succeeded(T data, AzureCliResult rawResult) =>
        new() { Success = true, Data = data, RawResult = rawResult };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static AzureCliResult<T> Failed(string errorMessage, AzureCliResult? rawResult = null) =>
        new() { Success = false, ErrorMessage = errorMessage, RawResult = rawResult };
}
