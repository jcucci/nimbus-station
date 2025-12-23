using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class UseCommandTests
{
    private readonly StubSessionService _sessionService;
    private readonly StubConfigurationService _configurationService;
    private readonly UseCommand _command;

    public UseCommandTests()
    {
        _sessionService = new StubSessionService();
        _configurationService = new StubConfigurationService();
        _command = new UseCommand(_sessionService, _configurationService);
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

    [Fact]
    public async Task ExecuteAsync_NoSessionWithArgs_ReturnsError()
    {
        var context = new CommandContext();

        var result = await _command.ExecuteAsync(["cosmos", "test-alias"], context);

        Assert.False(result.Success);
        Assert.Contains("No active session", result.Message);
    }

    #endregion

    #region Show Context Tests

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
        var session = Session.Create("TEST-123")
            .WithContext(new SessionContext("my-cosmos", "my-blob"));
        var context = new CommandContext { CurrentSession = session };

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    #endregion

    #region Use Cosmos Tests

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
        Assert.Equal("prod-main", context.CurrentSession!.ActiveContext?.ActiveCosmosAlias);
    }

    [Fact]
    public async Task ExecuteAsync_CosmosWithValidAlias_PreservesBlobContext()
    {
        _configurationService.AddCosmosAlias("prod-main", new CosmosAliasConfig(
            "https://prod.documents.azure.com:443/",
            "MainDb",
            "Users"));
        var session = Session.Create("TEST-123")
            .WithContext(new SessionContext(null, "existing-blob"));
        var context = new CommandContext { CurrentSession = session };
        _sessionService.SetSession(session);

        var result = await _command.ExecuteAsync(["cosmos", "prod-main"], context);

        Assert.True(result.Success);
        Assert.Equal("prod-main", context.CurrentSession!.ActiveContext?.ActiveCosmosAlias);
        Assert.Equal("existing-blob", context.CurrentSession!.ActiveContext?.ActiveBlobAlias);
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

    #endregion

    #region Use Blob Tests

    [Fact]
    public async Task ExecuteAsync_BlobWithValidAlias_SetsContext()
    {
        _configurationService.AddBlobAlias("prod-logs", new BlobAliasConfig("prodlogs", "applogs"));
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["blob", "prod-logs"], context);

        Assert.True(result.Success);
        Assert.Equal("prod-logs", context.CurrentSession!.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public async Task ExecuteAsync_BlobWithValidAlias_PreservesCosmosContext()
    {
        _configurationService.AddBlobAlias("prod-logs", new BlobAliasConfig("prodlogs", "applogs"));
        var session = Session.Create("TEST-123")
            .WithContext(new SessionContext("existing-cosmos", null));
        var context = new CommandContext { CurrentSession = session };
        _sessionService.SetSession(session);

        var result = await _command.ExecuteAsync(["blob", "prod-logs"], context);

        Assert.True(result.Success);
        Assert.Equal("existing-cosmos", context.CurrentSession!.ActiveContext?.ActiveCosmosAlias);
        Assert.Equal("prod-logs", context.CurrentSession!.ActiveContext?.ActiveBlobAlias);
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

    #endregion

    #region Use Clear Tests

    [Fact]
    public async Task ExecuteAsync_Clear_ClearsAllContexts()
    {
        var session = Session.Create("TEST-123")
            .WithContext(new SessionContext("my-cosmos", "my-blob"));
        var context = new CommandContext { CurrentSession = session };
        _sessionService.SetSession(session);

        var result = await _command.ExecuteAsync(["clear"], context);

        Assert.True(result.Success);
        Assert.Null(context.CurrentSession!.ActiveContext?.ActiveCosmosAlias);
        Assert.Null(context.CurrentSession!.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public async Task ExecuteAsync_ClearCosmos_ClearsOnlyCosmos()
    {
        var session = Session.Create("TEST-123")
            .WithContext(new SessionContext("my-cosmos", "my-blob"));
        var context = new CommandContext { CurrentSession = session };
        _sessionService.SetSession(session);

        var result = await _command.ExecuteAsync(["clear", "cosmos"], context);

        Assert.True(result.Success);
        Assert.Null(context.CurrentSession!.ActiveContext?.ActiveCosmosAlias);
        Assert.Equal("my-blob", context.CurrentSession!.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public async Task ExecuteAsync_ClearBlob_ClearsOnlyBlob()
    {
        var session = Session.Create("TEST-123")
            .WithContext(new SessionContext("my-cosmos", "my-blob"));
        var context = new CommandContext { CurrentSession = session };
        _sessionService.SetSession(session);

        var result = await _command.ExecuteAsync(["clear", "blob"], context);

        Assert.True(result.Success);
        Assert.Equal("my-cosmos", context.CurrentSession!.ActiveContext?.ActiveCosmosAlias);
        Assert.Null(context.CurrentSession!.ActiveContext?.ActiveBlobAlias);
    }

    [Fact]
    public async Task ExecuteAsync_ClearUnknownProvider_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["clear", "queue"], context);

        Assert.False(result.Success);
        Assert.Contains("Unknown provider", result.Message);
    }

    #endregion

    #region Unknown Provider Tests

    [Fact]
    public async Task ExecuteAsync_UnknownProvider_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["queue", "some-alias"], context);

        Assert.False(result.Success);
        Assert.Contains("Unknown provider", result.Message);
        Assert.Contains("queue", result.Message);
    }

    #endregion

    #region Command Properties Tests

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

    #endregion

    #region Helpers

    private CommandContext CreateContextWithSession()
    {
        var session = Session.Create("TEST-123");
        _sessionService.SetSession(session);
        return new CommandContext { CurrentSession = session };
    }

    #endregion

    #region Stub Implementations

    private sealed class StubSessionService : ISessionService
    {
        private Session? _session;

        public void SetSession(Session session) => _session = session;

        public Task<Session> StartSessionAsync(string sessionName, CancellationToken cancellationToken = default)
            => Task.FromResult(_session ?? Session.Create(sessionName));

        public Task<IReadOnlyList<Session>> ListSessionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Session>>(_session is not null ? [_session] : []);

        public Task<Session> ResumeSessionAsync(string sessionName, CancellationToken cancellationToken = default)
            => Task.FromResult(_session ?? throw new SessionNotFoundException(sessionName));

        public Task DeleteSessionAsync(string sessionName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Session> UpdateSessionContextAsync(string sessionName, SessionContext context, CancellationToken cancellationToken = default)
        {
            if (_session is null)
                throw new SessionNotFoundException(sessionName);

            _session = _session.WithContext(context);
            return Task.FromResult(_session);
        }

        public bool SessionExists(string sessionName) => _session?.TicketId == sessionName;

        public string GetSessionDirectory(string sessionName) => $"/tmp/nimbus/{sessionName}";

        public string GetDownloadsDirectory(string sessionName) => $"/tmp/nimbus/{sessionName}/downloads";

        public string GetQueriesDirectory(string sessionName) => $"/tmp/nimbus/{sessionName}/queries";
    }

    #endregion
}
