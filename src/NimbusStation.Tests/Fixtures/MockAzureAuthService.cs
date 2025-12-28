using NimbusStation.Providers.Azure.Auth;

namespace NimbusStation.Tests.Fixtures;

/// <summary>
/// Mock implementation of IAzureAuthService for testing commands.
/// </summary>
public sealed class MockAzureAuthService : IAzureAuthService
{
    public bool IsInstalled { get; set; } = true;
    public string? Version { get; set; } = "azure-cli 2.58.0";
    public AzureAuthStatus StatusResult { get; set; } = AzureAuthStatus.NotAuthenticated("Not logged in");
    public AzureAuthStatus LoginResult { get; set; } = AzureAuthStatus.NotAuthenticated("Login cancelled");

    public Task<AzureAuthStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(StatusResult);

    public Task<AzureAuthStatus> LoginAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(LoginResult);

    public Task<string?> GetCliVersionAsync() =>
        Task.FromResult(Version);

    public Task<bool> IsCliInstalledAsync() =>
        Task.FromResult(IsInstalled);
}
