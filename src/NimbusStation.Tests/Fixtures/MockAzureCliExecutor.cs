using NimbusStation.Providers.Azure.Cli;

namespace NimbusStation.Tests.Fixtures;

/// <summary>
/// Mock implementation of IAzureCliExecutor for unit testing.
/// </summary>
public sealed class MockAzureCliExecutor : IAzureCliExecutor
{
    public bool IsInstalled { get; set; }
    public string? Version { get; set; }
    public AzureCliResult? AccountShowResult { get; set; }
    public AzureCliResult? LoginResult { get; set; }

    public Task<bool> IsInstalledAsync() => Task.FromResult(IsInstalled);

    public Task<string?> GetVersionAsync() => Task.FromResult(Version);

    public Task<AzureCliResult> ExecuteAsync(
        string arguments,
        int timeoutMs = 30000,
        CancellationToken cancellationToken = default)
    {
        if (arguments == "login" && LoginResult is not null)
            return Task.FromResult(LoginResult);

        if (arguments.Contains("account show") && AccountShowResult is not null)
            return Task.FromResult(AccountShowResult);

        return Task.FromResult(AzureCliResult.Failed("Unexpected command"));
    }

    public Task<AzureCliResult<T>> ExecuteJsonAsync<T>(
        string arguments,
        int timeoutMs = 30000,
        CancellationToken cancellationToken = default)
    {
        if (!arguments.Contains("account show") || AccountShowResult is null)
            return Task.FromResult(AzureCliResult<T>.Failed("Unexpected command"));

        if (!AccountShowResult.Success)
            return Task.FromResult(AzureCliResult<T>.Failed(AccountShowResult.ErrorMessage ?? "Failed", AccountShowResult));

        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<T>(AccountShowResult.StandardOutput);
            if (data is null)
                return Task.FromResult(AzureCliResult<T>.Failed("Null result", AccountShowResult));

            return Task.FromResult(AzureCliResult<T>.Succeeded(data, AccountShowResult));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AzureCliResult<T>.Failed(ex.Message, AccountShowResult));
        }
    }
}
