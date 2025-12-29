using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Infrastructure.Output;
using NimbusStation.Providers.Azure.Cosmos;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class CosmosCommandTests
{
    private readonly MockCosmosService _cosmosService;
    private readonly StubConfigurationService _configurationService;
    private readonly StubSessionService _sessionService;
    private readonly StubSessionStateManager _sessionStateManager;
    private readonly CosmosCommand _command;
    private readonly CaptureOutputWriter _outputWriter;

    public CosmosCommandTests()
    {
        _cosmosService = new MockCosmosService();
        _configurationService = new StubConfigurationService();
        _sessionService = new StubSessionService();
        _sessionStateManager = new StubSessionStateManager();
        _command = new CosmosCommand(_cosmosService, _configurationService, _sessionService);
        _outputWriter = new CaptureOutputWriter();
    }

    [Fact]
    public async Task ExecuteAsync_NoSession_ReturnsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["query", "SELECT * FROM c"], context);

        Assert.False(result.Success);
        Assert.Contains("No active session", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_NoArgs_ReturnsUsageError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync([], context);

        Assert.False(result.Success);
        Assert.Contains("Usage", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownSubcommand_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["unknown"], context);

        Assert.False(result.Success);
        Assert.Contains("Unknown subcommand", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_QueryWithoutCosmosContext_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["query", "SELECT * FROM c"], context);

        Assert.False(result.Success);
        Assert.Contains("No active cosmos context", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_QueryWithMissingAlias_ReturnsError()
    {
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext("missing-alias", null)));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["query", "SELECT * FROM c"], context);

        Assert.False(result.Success);
        Assert.Contains("not found in config", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_QueryWithoutSql_ReturnsUsageError()
    {
        SetupValidCosmosContext();
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["query"], context);

        Assert.False(result.Success);
        Assert.Contains("Usage", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_QuerySuccess_OutputsJson()
    {
        SetupValidCosmosContext();
        _cosmosService.SetupQueryResult(MockCosmosService.CreateResult(
            [new { id = "1", name = "Test" }],
            requestCharge: 5.5));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["query", "SELECT * FROM c"], context);

        Assert.True(result.Success);
        var output = _outputWriter.GetOutput();
        Assert.Contains("\"id\"", output);
        Assert.Contains("\"name\"", output);
    }

    [Fact]
    public async Task ExecuteAsync_QuerySuccess_OutputsRuChargeToStderr()
    {
        SetupValidCosmosContext();
        _cosmosService.SetupQueryResult(MockCosmosService.CreateResult(
            [new { id = "1" }],
            requestCharge: 3.42));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["query", "SELECT * FROM c"], context);

        var errorOutput = _outputWriter.GetErrorOutput();
        Assert.Contains("Request charge:", errorOutput);
        Assert.Contains("3.42", errorOutput);
    }

    [Fact]
    public async Task ExecuteAsync_QueryWithMoreResults_NotesInStderr()
    {
        SetupValidCosmosContext();
        _cosmosService.SetupQueryResult(MockCosmosService.CreateResult(
            [new { id = "1" }],
            hasMoreResults: true));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["query", "SELECT * FROM c"], context);

        var errorOutput = _outputWriter.GetErrorOutput();
        Assert.Contains("More results available", errorOutput);
    }

    [Fact]
    public async Task ExecuteAsync_QueryWithMaxItems_PassesToService()
    {
        SetupValidCosmosContext();
        _cosmosService.SetupQueryResult(MockCosmosService.CreateEmptyResult());
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["query", "SELECT * FROM c", "--max-items", "50"], context);

        var call = Assert.Single(_cosmosService.QueryCalls);
        Assert.Equal(50, call.MaxItems);
    }

    [Fact]
    public async Task ExecuteAsync_QueryDefaultMaxItems_Is100()
    {
        SetupValidCosmosContext();
        _cosmosService.SetupQueryResult(MockCosmosService.CreateEmptyResult());
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["query", "SELECT * FROM c"], context);

        var call = Assert.Single(_cosmosService.QueryCalls);
        Assert.Equal(100, call.MaxItems);
    }

    [Fact]
    public async Task ExecuteAsync_QueryPassesSqlToService()
    {
        SetupValidCosmosContext();
        _cosmosService.SetupQueryResult(MockCosmosService.CreateEmptyResult());
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["query", "SELECT c.id FROM c WHERE c.type = 'order'"], context);

        var call = Assert.Single(_cosmosService.QueryCalls);
        Assert.Equal("SELECT c.id FROM c WHERE c.type = 'order'", call.Sql);
    }

    [Fact]
    public async Task ExecuteAsync_QueryPassesAliasToService()
    {
        SetupValidCosmosContext();
        _cosmosService.SetupQueryResult(MockCosmosService.CreateEmptyResult());
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["query", "SELECT * FROM c"], context);

        var call = Assert.Single(_cosmosService.QueryCalls);
        Assert.Equal("prod-orders", call.AliasName);
    }

    [Fact]
    public async Task ExecuteAsync_ServiceThrowsInvalidOperation_ReturnsError()
    {
        SetupValidCosmosContext();
        _cosmosService.SetupQueryException(new InvalidOperationException("Set environment variable COSMOS_KEY"));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["query", "SELECT * FROM c"], context);

        Assert.False(result.Success);
        Assert.Contains("COSMOS_KEY", result.Message);
    }

    [Fact]
    public void Name_ReturnsCosmos()
    {
        Assert.Equal("cosmos", _command.Name);
    }

    [Fact]
    public void Subcommands_ContainsQuery()
    {
        Assert.Contains("query", _command.Subcommands);
    }

    private void SetupValidCosmosContext()
    {
        _configurationService.AddCosmosAlias("prod-orders", new CosmosAliasConfig(
            "https://prod.documents.azure.com:443/",
            "OrdersDb",
            "Orders",
            "COSMOS_PROD_ORDERS_KEY"));
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext("prod-orders", null)));
    }

    private CommandContext CreateContextWithSession()
    {
        _sessionStateManager.ActivateSession(Session.Create("TEST-123"));
        return new CommandContext(_sessionStateManager, _outputWriter);
    }
}
