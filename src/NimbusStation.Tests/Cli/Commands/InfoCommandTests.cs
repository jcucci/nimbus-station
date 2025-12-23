using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Tests.Helpers;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class InfoCommandTests
{
    private readonly StubConfigurationService _configurationService;
    private readonly InfoCommand _command;

    public InfoCommandTests()
    {
        _configurationService = new StubConfigurationService();
        _command = new InfoCommand(_configurationService);
    }

    #region No Session Tests

    [Fact]
    public async Task ExecuteAsync_NoSession_ReturnsError()
    {
        var context = new CommandContext();

        var result = await _command.ExecuteAsync([], context);

        Assert.False(result.Success);
        Assert.Contains("No active session", result.Message);
    }

    #endregion

    #region No Context Tests

    [Fact]
    public async Task ExecuteAsync_NoActiveContext_ReturnsSuccess()
    {
        var session = Session.Create("TEST-123");
        var context = new CommandContext { CurrentSession = session };

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyContext_ReturnsSuccess()
    {
        var session = Session.Create("TEST-123")
            .WithContext(SessionContext.Empty);
        var context = new CommandContext { CurrentSession = session };

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    #endregion

    #region Cosmos Context Tests

    [Fact]
    public async Task ExecuteAsync_WithCosmosContext_ReturnsSuccess()
    {
        _configurationService.AddCosmosAlias("prod-main", new CosmosAliasConfig(
            "https://prod.documents.azure.com:443/",
            "MainDb",
            "Users"));
        var session = Session.Create("TEST-123")
            .WithContext(new SessionContext("prod-main", null));
        var context = new CommandContext { CurrentSession = session };

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithCosmosContextMissingConfig_ReturnsSuccess()
    {
        // Alias exists in session but not in config (e.g., config was edited)
        var session = Session.Create("TEST-123")
            .WithContext(new SessionContext("missing-alias", null));
        var context = new CommandContext { CurrentSession = session };

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    #endregion

    #region Blob Context Tests

    [Fact]
    public async Task ExecuteAsync_WithBlobContext_ReturnsSuccess()
    {
        _configurationService.AddBlobAlias("prod-logs", new BlobAliasConfig("prodlogs", "applogs"));
        var session = Session.Create("TEST-123")
            .WithContext(new SessionContext(null, "prod-logs"));
        var context = new CommandContext { CurrentSession = session };

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithBlobContextMissingConfig_ReturnsSuccess()
    {
        // Alias exists in session but not in config
        var session = Session.Create("TEST-123")
            .WithContext(new SessionContext(null, "missing-blob"));
        var context = new CommandContext { CurrentSession = session };

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    #endregion

    #region Both Contexts Tests

    [Fact]
    public async Task ExecuteAsync_WithBothContexts_ReturnsSuccess()
    {
        _configurationService.AddCosmosAlias("prod-main", new CosmosAliasConfig(
            "https://prod.documents.azure.com:443/",
            "MainDb",
            "Users"));
        _configurationService.AddBlobAlias("prod-logs", new BlobAliasConfig("prodlogs", "applogs"));
        var session = Session.Create("TEST-123")
            .WithContext(new SessionContext("prod-main", "prod-logs"));
        var context = new CommandContext { CurrentSession = session };

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    #endregion

    #region Command Properties Tests

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

    #endregion
}
