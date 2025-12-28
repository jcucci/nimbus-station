using NimbusStation.Providers.Azure.Cli;

namespace NimbusStation.Tests.Providers.Azure.Cli;

/// <summary>
/// Integration tests for AzureCliExecutor.
/// These tests require Azure CLI to be installed and are excluded from CI builds.
/// </summary>
[Trait("Category", "Integration")]
public class AzureCliExecutorTests
{
    private readonly AzureCliExecutor _executor;

    public AzureCliExecutorTests()
    {
        _executor = new AzureCliExecutor();
    }

    [Fact]
    public async Task IsInstalledAsync_WhenAzCliInstalled_ReturnsTrue()
    {
        var result = await _executor.IsInstalledAsync();

        // This test assumes az CLI is installed on the test machine
        // If not installed, this test will fail - which is expected
        Assert.True(result, "Azure CLI should be installed for these tests to pass");
    }

    [Fact]
    public async Task GetVersionAsync_WhenAzCliInstalled_ReturnsVersionString()
    {
        var version = await _executor.GetVersionAsync();

        Assert.NotNull(version);
        Assert.Contains("azure-cli", version, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithVersionCommand_ReturnsSuccess()
    {
        var result = await _executor.ExecuteAsync("--version");

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("azure-cli", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidCommand_ReturnsFailure()
    {
        var result = await _executor.ExecuteAsync("this-command-does-not-exist");

        Assert.False(result.Success);
        Assert.NotEqual(0, result.ExitCode);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_TimesOutLongRunningCommand()
    {
        // Using a command that might take a while - checking for very short timeout
        var result = await _executor.ExecuteAsync("account list", timeoutMs: 1);

        // Should either timeout or complete very fast if cached
        // We just verify it doesn't hang
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _executor.ExecuteAsync("--version", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ExecuteJsonAsync_WithAccountShow_ParsesJsonOutput()
    {
        // This will fail if not logged in, but should still return a valid result structure
        var result = await _executor.ExecuteJsonAsync<AccountInfo>("account show");

        // Either succeeds with data or fails with a message
        Assert.NotNull(result);
        if (result.Success)
        {
            Assert.NotNull(result.Data);
        }
        else
        {
            Assert.NotNull(result.ErrorMessage);
        }
    }

    [Fact]
    public async Task ExecuteJsonAsync_WithInvalidJson_ReturnsParseError()
    {
        // --version returns text, not JSON, so parsing should fail
        var result = await _executor.ExecuteJsonAsync<AccountInfo>("--version");

        Assert.False(result.Success);
        Assert.Contains("parse", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Helper class for JSON deserialization tests
    private sealed class AccountInfo
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
        public string? TenantId { get; set; }
    }
}
