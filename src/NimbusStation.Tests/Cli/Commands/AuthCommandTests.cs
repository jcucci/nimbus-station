using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Output;
using NimbusStation.Providers.Azure.Auth;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class AuthCommandTests
{
    private readonly StubSessionStateManager _sessionStateManager;
    private readonly MockAzureAuthService _authService;
    private readonly AuthCommand _command;
    private readonly CaptureOutputWriter _outputWriter;

    public AuthCommandTests()
    {
        _sessionStateManager = new StubSessionStateManager();
        _authService = new MockAzureAuthService();
        _command = new AuthCommand(_authService);
        _outputWriter = new CaptureOutputWriter();
    }

    [Fact]
    public void Name_ReturnsAuth() =>
        Assert.Equal("auth", _command.Name);

    [Fact]
    public void Description_ReturnsExpectedText() =>
        Assert.Equal("Manage Azure authentication", _command.Description);

    [Fact]
    public void Usage_ReturnsExpectedFormat() =>
        Assert.Equal("auth <status|login>", _command.Usage);

    [Fact]
    public void Subcommands_ContainsStatusAndLogin()
    {
        Assert.Contains("status", _command.Subcommands);
        Assert.Contains("login", _command.Subcommands);
        Assert.Equal(2, _command.Subcommands.Count);
    }

    [Fact]
    public async Task ExecuteAsync_NoArgs_ShowsStatus()
    {
        _authService.StatusResult = AzureAuthStatus.Authenticated(
            identity: "user@example.com",
            subscriptionName: "My Subscription",
            subscriptionId: "sub-123",
            tenantId: "tenant-456",
            cliVersion: "azure-cli 2.58.0");
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
        Assert.IsType<AzureAuthStatus>(result.Data);
    }

    [Fact]
    public async Task ExecuteAsync_StatusSubcommand_ShowsStatus()
    {
        _authService.StatusResult = AzureAuthStatus.Authenticated(
            identity: "user@example.com",
            subscriptionName: "My Subscription",
            subscriptionId: "sub-123",
            tenantId: "tenant-456");
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["status"], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_StatusWhenCliNotInstalled_ReturnsError()
    {
        _authService.StatusResult = AzureAuthStatus.CliNotInstalled();
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["status"], context);

        Assert.False(result.Success);
        Assert.Contains("Azure CLI not found", result.Message);
        Assert.Contains("not installed", _outputWriter.GetOutput());
    }

    [Fact]
    public async Task ExecuteAsync_StatusWhenNotAuthenticated_ReturnsSuccess()
    {
        _authService.StatusResult = AzureAuthStatus.NotAuthenticated(
            errorMessage: "Please run 'az login'",
            cliVersion: "azure-cli 2.58.0");
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["status"], context);

        Assert.True(result.Success);
        Assert.IsType<AzureAuthStatus>(result.Data);
    }

    [Fact]
    public async Task ExecuteAsync_StatusWhenAuthenticated_ReturnsStatusData()
    {
        var expectedStatus = AzureAuthStatus.Authenticated(
            identity: "user@example.com",
            subscriptionName: "My Subscription",
            subscriptionId: "sub-123",
            tenantId: "tenant-456",
            cliVersion: "azure-cli 2.58.0");
        _authService.StatusResult = expectedStatus;
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["status"], context);

        Assert.True(result.Success);
        var status = Assert.IsType<AzureAuthStatus>(result.Data);
        Assert.True(status.IsAuthenticated);
        Assert.Equal("user@example.com", status.Identity);
    }

    [Fact]
    public async Task ExecuteAsync_LoginWhenCliNotInstalled_ReturnsError()
    {
        _authService.IsInstalled = false;
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["login"], context);

        Assert.False(result.Success);
        Assert.Contains("Azure CLI not found", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_LoginWhenLoginFails_ReturnsError()
    {
        _authService.IsInstalled = true;
        _authService.LoginResult = AzureAuthStatus.NotAuthenticated("Login cancelled by user");
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["login"], context);

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_LoginWhenLoginSucceeds_ReturnsSuccess()
    {
        _authService.IsInstalled = true;
        _authService.LoginResult = AzureAuthStatus.Authenticated(
            identity: "user@example.com",
            subscriptionName: "My Subscription",
            subscriptionId: "sub-123",
            tenantId: "tenant-456");
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["login"], context);

        Assert.True(result.Success);
        var status = Assert.IsType<AzureAuthStatus>(result.Data);
        Assert.True(status.IsAuthenticated);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownSubcommand_ReturnsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["unknown"], context);

        Assert.False(result.Success);
        Assert.Contains("Unknown subcommand", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_StatusSubcommandCaseInsensitive_ShowsStatus()
    {
        _authService.StatusResult = AzureAuthStatus.Authenticated(
            identity: "user@example.com",
            subscriptionName: "My Subscription",
            subscriptionId: "sub-123",
            tenantId: "tenant-456");
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["STATUS"], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_LoginSubcommandCaseInsensitive_AttemptsLogin()
    {
        _authService.IsInstalled = true;
        _authService.LoginResult = AzureAuthStatus.Authenticated(
            identity: "user@example.com",
            subscriptionName: "My Subscription",
            subscriptionId: "sub-123",
            tenantId: "tenant-456");
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["LOGIN"], context);

        Assert.True(result.Success);
    }
}
