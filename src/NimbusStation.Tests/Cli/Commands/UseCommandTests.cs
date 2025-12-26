using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Infrastructure.Output;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class UseCommandTests
{
    private readonly StubSessionService _sessionService;
    private readonly StubSessionStateManager _sessionStateManager;
    private readonly StubConfigurationService _configurationService;
    private readonly UseCommand _command;
    private readonly CaptureOutputWriter _outputWriter;

    public UseCommandTests()
    {
        _sessionService = new StubSessionService();
        _sessionStateManager = new StubSessionStateManager();
        _configurationService = new StubConfigurationService();
        _command = new UseCommand(_sessionService, _sessionStateManager, _configurationService);
        _outputWriter = new CaptureOutputWriter();
    }

    [Fact]
    public async Task ExecuteAsync_NoSession_ReturnsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.False(result.Success);
        Assert.Contains("No active session", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_NoSessionWithArgs_ReturnsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["cosmos", "test-alias"], context);

        Assert.False(result.Success);
        Assert.Contains("No active session", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_NoArgs_ShowsCurrentContext()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_NoArgsWithActiveContext_ShowsContextDetails()
    {
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext("my-cosmos", "my-blob")));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_CosmosWithValidAlias_SetsContext()
    {
        _configurationService.AddCosmosAlias("prod-main", new CosmosAliasConfig(
            "https://prod.documents.azure.com:443/",
            "MainDb",
            "Users"));
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["cosmos", "prod-main"], context);

        Assert.True(result.Success);
        Assert.Equal("prod-main", _sessionStateManager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
    }

    [Fact]
    public async Task ExecuteAsync_CosmosWithValidAlias_PreservesBlobContext()
    {
        _configurationService.AddCosmosAlias("prod-main", new CosmosAliasConfig(
            "https://prod.documents.azure.com:443/",
            "MainDb",
            "Users"));
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext(null, "existing-blob")));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["cosmos", "prod-main"], context);

        Assert.True(result.Success);
        Assert.Equal("prod-main", _sessionStateManager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
        Assert.Equal("existing-blob", _sessionStateManager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public async Task ExecuteAsync_CosmosWithInvalidAlias_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["cosmos", "nonexistent"], context);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
        Assert.Contains("nonexistent", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_CosmosWithoutAlias_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["cosmos"], context);

        Assert.False(result.Success);
        Assert.Contains("Usage", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_BlobWithValidAlias_SetsContext()
    {
        _configurationService.AddBlobAlias("prod-logs", new BlobAliasConfig("prodlogs", "applogs"));
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["blob", "prod-logs"], context);

        Assert.True(result.Success);
        Assert.Equal("prod-logs", _sessionStateManager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public async Task ExecuteAsync_BlobWithValidAlias_PreservesCosmosContext()
    {
        _configurationService.AddBlobAlias("prod-logs", new BlobAliasConfig("prodlogs", "applogs"));
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext("existing-cosmos", null)));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["blob", "prod-logs"], context);

        Assert.True(result.Success);
        Assert.Equal("existing-cosmos", _sessionStateManager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
        Assert.Equal("prod-logs", _sessionStateManager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public async Task ExecuteAsync_BlobWithInvalidAlias_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["blob", "nonexistent"], context);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
        Assert.Contains("nonexistent", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_BlobWithoutAlias_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["blob"], context);

        Assert.False(result.Success);
        Assert.Contains("Usage", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Clear_ClearsAllContexts()
    {
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext("my-cosmos", "my-blob")));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["clear"], context);

        Assert.True(result.Success);
        Assert.Null(_sessionStateManager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
        Assert.Null(_sessionStateManager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public async Task ExecuteAsync_ClearCosmos_ClearsOnlyCosmos()
    {
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext("my-cosmos", "my-blob")));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["clear", "cosmos"], context);

        Assert.True(result.Success);
        Assert.Null(_sessionStateManager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
        Assert.Equal("my-blob", _sessionStateManager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public async Task ExecuteAsync_ClearBlob_ClearsOnlyBlob()
    {
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext("my-cosmos", "my-blob")));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["clear", "blob"], context);

        Assert.True(result.Success);
        Assert.Equal("my-cosmos", _sessionStateManager.CurrentSession?.ActiveContext?.ActiveCosmosAlias);
        Assert.Null(_sessionStateManager.CurrentSession?.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public async Task ExecuteAsync_ClearUnknownProvider_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["clear", "queue"], context);

        Assert.False(result.Success);
        Assert.Contains("Unknown provider", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownProvider_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["queue", "some-alias"], context);

        Assert.False(result.Success);
        Assert.Contains("Unknown provider", result.Message);
        Assert.Contains("queue", result.Message);
    }

    [Fact]
    public void Name_ReturnsUse()
    {
        Assert.Equal("use", _command.Name);
    }

    [Fact]
    public void Subcommands_ContainsExpectedValues()
    {
        Assert.Contains("cosmos", _command.Subcommands);
        Assert.Contains("blob", _command.Subcommands);
        Assert.Contains("clear", _command.Subcommands);
    }

    private CommandContext CreateContextWithSession()
    {
        _sessionStateManager.ActivateSession(Session.Create("TEST-123"));
        return new CommandContext(_sessionStateManager, _outputWriter);
    }
}
