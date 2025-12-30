using NimbusStation.Providers.Azure.Cli;

namespace NimbusStation.Tests.Fixtures;

/// <summary>
/// Mock implementation of IAzureCliExecutor for unit testing.
/// </summary>
public sealed class MockAzureCliExecutor : IAzureCliExecutor
{
    public bool IsInstalled { get; set; } = true;
    public string? Version { get; set; }
    public AzureCliResult? AccountShowResult { get; set; }
    public AzureCliResult? LoginResult { get; set; }

    private string? _nextJsonResult;
    private string? _nextFailureMessage;
    private readonly List<string> _executeCalls = [];

    public IReadOnlyList<string> ExecuteCalls => _executeCalls;

    public void SetupJsonResult(string json)
    {
        _nextJsonResult = json;
        _nextFailureMessage = null;
    }

    public void SetupFailure(string message)
    {
        _nextFailureMessage = message;
        _nextJsonResult = null;
    }

    public Task<bool> IsInstalledAsync() => Task.FromResult(IsInstalled);

    public Task<string?> GetVersionAsync() => Task.FromResult(Version);

    public Task<AzureCliResult> ExecuteAsync(
        string arguments, int timeoutMs = 30000, CancellationToken cancellationToken = default)
    {
        _executeCalls.Add(arguments);

        if (_nextFailureMessage is not null)
            return Task.FromResult(AzureCliResult.Failed(_nextFailureMessage));

        if (arguments == "login" && LoginResult is not null)
            return Task.FromResult(LoginResult);

        if (arguments.Contains("account show") && AccountShowResult is not null)
            return Task.FromResult(AccountShowResult);

        return Task.FromResult(AzureCliResult.Succeeded(_nextJsonResult ?? "", "", 0));
    }

    public Task<AzureCliResult<T>> ExecuteJsonAsync<T>(
        string arguments, int timeoutMs = 30000, CancellationToken cancellationToken = default)
    {
        _executeCalls.Add(arguments);

        if (_nextFailureMessage is not null)
            return Task.FromResult(AzureCliResult<T>.Failed(_nextFailureMessage));

        if (_nextJsonResult is not null)
        {
            try
            {
                var cliResult = AzureCliResult.Succeeded(_nextJsonResult, "", 0);
                var data = System.Text.Json.JsonSerializer.Deserialize<T>(_nextJsonResult);
                if (data is null)
                    return Task.FromResult(AzureCliResult<T>.Failed("Null result", cliResult));
                return Task.FromResult(AzureCliResult<T>.Succeeded(data, cliResult));
            }
            catch (Exception ex)
            {
                return Task.FromResult(AzureCliResult<T>.Failed(ex.Message));
            }
        }

        if (arguments.Contains("account show") && AccountShowResult is not null)
        {
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

        return Task.FromResult(AzureCliResult<T>.Failed("Unexpected command"));
    }
}
