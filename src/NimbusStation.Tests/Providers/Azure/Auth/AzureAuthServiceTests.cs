using NimbusStation.Providers.Azure.Auth;
using NimbusStation.Providers.Azure.Cli;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Providers.Azure.Auth;

/// <summary>
/// Unit tests for AzureAuthService using a mock CLI executor.
/// </summary>
public class AzureAuthServiceTests
{
    [Fact]
    public async Task GetStatusAsync_WhenCliNotInstalled_ReturnsCliNotInstalled()
    {
        var mockExecutor = new MockAzureCliExecutor { IsInstalled = false };
        var service = new AzureAuthService(mockExecutor);

        var status = await service.GetStatusAsync();

        Assert.False(status.IsAuthenticated);
        Assert.False(status.IsCliInstalled);
        Assert.Contains("not found", status.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStatusAsync_WhenNotLoggedIn_ReturnsNotAuthenticated()
    {
        var mockExecutor = new MockAzureCliExecutor
        {
            IsInstalled = true,
            Version = "azure-cli 2.58.0",
            AccountShowResult = AzureCliResult.Failed("Please run 'az login' to setup account.")
        };
        var service = new AzureAuthService(mockExecutor);

        var status = await service.GetStatusAsync();

        Assert.False(status.IsAuthenticated);
        Assert.True(status.IsCliInstalled);
        Assert.NotNull(status.ErrorMessage);
    }

    [Fact]
    public async Task GetStatusAsync_WhenLoggedIn_ReturnsAuthenticated()
    {
        var accountJson = """
        {
            "id": "subscription-id-123",
            "name": "My Subscription",
            "tenantId": "tenant-id-456",
            "user": {
                "name": "user@example.com",
                "type": "user"
            }
        }
        """;

        var mockExecutor = new MockAzureCliExecutor
        {
            IsInstalled = true,
            Version = "azure-cli 2.58.0",
            AccountShowResult = AzureCliResult.Succeeded(accountJson)
        };
        var service = new AzureAuthService(mockExecutor);

        var status = await service.GetStatusAsync();

        Assert.True(status.IsAuthenticated);
        Assert.True(status.IsCliInstalled);
        Assert.Equal("user@example.com", status.Identity);
        Assert.Equal("My Subscription", status.SubscriptionName);
        Assert.Equal("subscription-id-123", status.SubscriptionId);
        Assert.Equal("tenant-id-456", status.TenantId);
        Assert.Equal("azure-cli 2.58.0", status.CliVersion);
    }

    [Fact]
    public async Task IsCliInstalledAsync_DelegatesToExecutor()
    {
        var mockExecutor = new MockAzureCliExecutor { IsInstalled = true };
        var service = new AzureAuthService(mockExecutor);

        var result = await service.IsCliInstalledAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task GetCliVersionAsync_DelegatesToExecutor()
    {
        var mockExecutor = new MockAzureCliExecutor
        {
            IsInstalled = true,
            Version = "azure-cli 2.58.0"
        };
        var service = new AzureAuthService(mockExecutor);

        var version = await service.GetCliVersionAsync();

        Assert.Equal("azure-cli 2.58.0", version);
    }

    [Fact]
    public async Task LoginAsync_WhenCliNotInstalled_ReturnsCliNotInstalled()
    {
        var mockExecutor = new MockAzureCliExecutor { IsInstalled = false };
        var service = new AzureAuthService(mockExecutor);

        var status = await service.LoginAsync();

        Assert.False(status.IsAuthenticated);
        Assert.False(status.IsCliInstalled);
    }

    [Fact]
    public async Task LoginAsync_WhenLoginFails_ReturnsNotAuthenticated()
    {
        var mockExecutor = new MockAzureCliExecutor
        {
            IsInstalled = true,
            Version = "azure-cli 2.58.0",
            LoginResult = AzureCliResult.Failed("Login cancelled by user")
        };
        var service = new AzureAuthService(mockExecutor);

        var status = await service.LoginAsync();

        Assert.False(status.IsAuthenticated);
        Assert.Contains("cancelled", status.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_WhenLoginSucceeds_ReturnsAuthenticatedStatus()
    {
        var accountJson = """
        {
            "id": "subscription-id-123",
            "name": "My Subscription",
            "tenantId": "tenant-id-456",
            "user": {
                "name": "user@example.com",
                "type": "user"
            }
        }
        """;

        var mockExecutor = new MockAzureCliExecutor
        {
            IsInstalled = true,
            Version = "azure-cli 2.58.0",
            LoginResult = AzureCliResult.Succeeded("Login succeeded"),
            AccountShowResult = AzureCliResult.Succeeded(accountJson)
        };
        var service = new AzureAuthService(mockExecutor);

        var status = await service.LoginAsync();

        Assert.True(status.IsAuthenticated);
        Assert.Equal("user@example.com", status.Identity);
    }
}
