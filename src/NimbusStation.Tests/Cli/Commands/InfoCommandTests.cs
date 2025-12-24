using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Infrastructure.Output;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class InfoCommandTests
{
    private readonly StubSessionService _sessionService;
    private readonly StubConfigurationService _configurationService;
    private readonly InfoCommand _command;
    private readonly CaptureOutputWriter _outputWriter;

    public InfoCommandTests()
    {
        _sessionService = new StubSessionService();
        _configurationService = new StubConfigurationService();
        _command = new InfoCommand(_configurationService);
        _outputWriter = new CaptureOutputWriter();
    }

    [Fact]
    public async Task ExecuteAsync_NoSession_ReturnsError()
    {
        var context = new CommandContext(_sessionService, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.False(result.Success);
        Assert.Contains("No active session", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_NoActiveContext_ReturnsSuccess()
    {
        _sessionService.CurrentSession = Session.Create("TEST-123");
        var context = new CommandContext(_sessionService, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyContext_ReturnsSuccess()
    {
        _sessionService.CurrentSession = Session.Create("TEST-123").WithContext(SessionContext.Empty);
        var context = new CommandContext(_sessionService, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithCosmosContext_ReturnsSuccess()
    {
        _configurationService.AddCosmosAlias("prod-main", new CosmosAliasConfig(
            "https://prod.documents.azure.com:443/",
            "MainDb",
            "Users"));
        _sessionService.CurrentSession = Session.Create("TEST-123")
            .WithContext(new SessionContext("prod-main", null));
        var context = new CommandContext(_sessionService, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithCosmosContextMissingConfig_ReturnsSuccess()
    {
        _sessionService.CurrentSession = Session.Create("TEST-123")
            .WithContext(new SessionContext("missing-alias", null));
        var context = new CommandContext(_sessionService, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithBlobContext_ReturnsSuccess()
    {
        _configurationService.AddBlobAlias("prod-logs", new BlobAliasConfig("prodlogs", "applogs"));
        _sessionService.CurrentSession = Session.Create("TEST-123")
            .WithContext(new SessionContext(null, "prod-logs"));
        var context = new CommandContext(_sessionService, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithBlobContextMissingConfig_ReturnsSuccess()
    {
        _sessionService.CurrentSession = Session.Create("TEST-123")
            .WithContext(new SessionContext(null, "missing-blob"));
        var context = new CommandContext(_sessionService, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithBothContexts_ReturnsSuccess()
    {
        _configurationService.AddCosmosAlias("prod-main", new CosmosAliasConfig(
            "https://prod.documents.azure.com:443/",
            "MainDb",
            "Users"));
        _configurationService.AddBlobAlias("prod-logs", new BlobAliasConfig("prodlogs", "applogs"));
        _sessionService.CurrentSession = Session.Create("TEST-123")
            .WithContext(new SessionContext("prod-main", "prod-logs"));
        var context = new CommandContext(_sessionService, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public void Name_ReturnsInfo()
    {
        Assert.Equal("info", _command.Name);
    }

    [Fact]
    public void Subcommands_IsEmpty()
    {
        Assert.Empty(_command.Subcommands);
    }

    [Fact]
    public void Usage_ReturnsExpectedFormat()
    {
        Assert.Equal("info", _command.Usage);
    }
}
