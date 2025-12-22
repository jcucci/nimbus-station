using Microsoft.Extensions.Logging;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Aliases;

namespace NimbusStation.Tests.Infrastructure.Aliases;

public sealed class AliasResolverTests : IDisposable
{
    private readonly LoggerFactory _loggerFactory;
    private readonly ILogger<AliasResolver> _logger;
    private readonly string _sessionsBasePath;

    public AliasResolverTests()
    {
        _loggerFactory = new LoggerFactory();
        _logger = _loggerFactory.CreateLogger<AliasResolver>();
        _sessionsBasePath = "/test/sessions";
    }

    public void Dispose() => _loggerFactory.Dispose();

    private AliasResolver CreateResolver(Dictionary<string, string> aliases)
    {
        var mockService = new MockAliasService(aliases);
        return new AliasResolver(mockService, _logger, _sessionsBasePath);
    }

    private static Session CreateSession(string ticketId) =>
        Session.Create(ticketId);

    #region No Expansion Tests

    [Fact]
    public async Task ExpandAsync_EmptyInput_ReturnsNoExpansion()
    {
        var resolver = CreateResolver([]);

        var result = await resolver.ExpandAsync("", currentSession: null);

        Assert.False(result.WasExpanded);
        Assert.Equal("", result.ExpandedInput);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExpandAsync_WhitespaceInput_ReturnsNoExpansion()
    {
        var resolver = CreateResolver([]);

        var result = await resolver.ExpandAsync("   ", currentSession: null);

        Assert.False(result.WasExpanded);
    }

    [Fact]
    public async Task ExpandAsync_NotAnAlias_ReturnsNoExpansion()
    {
        var resolver = CreateResolver(new() { ["cq"] = "azure cosmos query" });

        var result = await resolver.ExpandAsync("session start SUP-123", currentSession: null);

        Assert.False(result.WasExpanded);
        Assert.Equal("session start SUP-123", result.ExpandedInput);
    }

    #endregion

    #region Simple Expansion Tests

    [Fact]
    public async Task ExpandAsync_SimpleAlias_Expands()
    {
        var resolver = CreateResolver(new() { ["cq"] = "azure cosmos query" });

        var result = await resolver.ExpandAsync("cq @prod", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Equal("azure cosmos query @prod", result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_AliasWithMultipleArgs_AppendsArgs()
    {
        var resolver = CreateResolver(new() { ["cq"] = "azure cosmos query" });

        var result = await resolver.ExpandAsync("cq @prod \"SELECT * FROM c\"", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Equal("azure cosmos query @prod \"SELECT * FROM c\"", result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_AliasNoArgs_ExpandsWithoutTrailingSpace()
    {
        var resolver = CreateResolver(new() { ["cq"] = "azure cosmos query" });

        var result = await resolver.ExpandAsync("cq", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Equal("azure cosmos query", result.ExpandedInput);
    }

    #endregion

    #region Positional Parameter Tests

    [Fact]
    public async Task ExpandAsync_PositionalParameter_Substitutes()
    {
        var resolver = CreateResolver(new()
        {
            ["user"] = "azure cosmos query @prod \"SELECT * FROM c WHERE c.userId = '{0}'\""
        });

        var result = await resolver.ExpandAsync("user abc-123", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Equal("azure cosmos query @prod \"SELECT * FROM c WHERE c.userId = 'abc-123'\"", result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_MultiplePositionalParameters_SubstitutesAll()
    {
        var resolver = CreateResolver(new()
        {
            ["find"] = "azure cosmos query @prod \"SELECT * FROM c WHERE c.userId = '{0}' AND c.status = '{1}'\""
        });

        var result = await resolver.ExpandAsync("find abc-123 active", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Contains("'abc-123'", result.ExpandedInput);
        Assert.Contains("'active'", result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_ExtraArgumentsBeyondPositional_Appends()
    {
        var resolver = CreateResolver(new()
        {
            ["user"] = "azure cosmos query @prod \"SELECT * FROM c WHERE c.userId = '{0}'\""
        });

        var result = await resolver.ExpandAsync("user abc-123 --limit 10", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.EndsWith("--limit 10", result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_MissingPositionalParameter_ReturnsError()
    {
        var resolver = CreateResolver(new()
        {
            ["user"] = "azure cosmos query @prod \"SELECT * FROM c WHERE c.userId = '{0}'\""
        });

        var result = await resolver.ExpandAsync("user", currentSession: null);

        Assert.False(result.IsSuccess);
        Assert.Contains("requires 1 argument(s)", result.Error);
        Assert.Contains("Usage: user <arg0>", result.Error);
    }

    [Fact]
    public async Task ExpandAsync_MissingMultiplePositionalParameters_ReturnsError()
    {
        var resolver = CreateResolver(new()
        {
            ["find"] = "query {0} {1}"
        });

        var result = await resolver.ExpandAsync("find only-one", currentSession: null);

        Assert.False(result.IsSuccess);
        Assert.Contains("requires 2 argument(s)", result.Error);
    }

    #endregion

    #region Built-in Variable Tests

    [Fact]
    public async Task ExpandAsync_TodayVariable_Substitutes()
    {
        var resolver = CreateResolver(new() { ["logs"] = "azure blob list @logs/{today}/" });

        var result = await resolver.ExpandAsync("logs", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Contains(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"), result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_UserVariable_Substitutes()
    {
        var resolver = CreateResolver(new() { ["whoami"] = "echo {user}" });

        var result = await resolver.ExpandAsync("whoami", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Contains(Environment.UserName, result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_NowVariable_Substitutes()
    {
        var resolver = CreateResolver(new() { ["timestamp"] = "echo {now}" });

        var result = await resolver.ExpandAsync("timestamp", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Matches(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z", result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_TicketVariable_WithSession_Substitutes()
    {
        var resolver = CreateResolver(new() { ["save"] = "azure blob download {0} /tmp/{ticket}/" });
        var session = CreateSession("SUP-123");

        var result = await resolver.ExpandAsync("save myfile.json", currentSession: session);

        Assert.True(result.WasExpanded);
        Assert.Contains("SUP-123", result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_SessionDirVariable_WithSession_Substitutes()
    {
        var resolver = CreateResolver(new() { ["save"] = "download {0} {session-dir}/downloads/" });
        var session = CreateSession("SUP-123");

        var result = await resolver.ExpandAsync("save myfile.json", currentSession: session);

        Assert.True(result.WasExpanded);
        Assert.Contains(Path.Combine(_sessionsBasePath, "SUP-123"), result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_TicketVariable_NoSession_ReturnsError()
    {
        var resolver = CreateResolver(new() { ["save"] = "save to {ticket}" });

        var result = await resolver.ExpandAsync("save", currentSession: null);

        Assert.False(result.IsSuccess);
        Assert.Contains("requires an active session", result.Error);
        Assert.Contains("{ticket}", result.Error);
    }

    [Fact]
    public async Task ExpandAsync_SessionDirVariable_NoSession_ReturnsError()
    {
        var resolver = CreateResolver(new() { ["save"] = "save to {session-dir}" });

        var result = await resolver.ExpandAsync("save", currentSession: null);

        Assert.False(result.IsSuccess);
        Assert.Contains("requires an active session", result.Error);
        Assert.Contains("{session-dir}", result.Error);
    }

    [Fact]
    public async Task ExpandAsync_VariablesCaseInsensitive()
    {
        var resolver = CreateResolver(new() { ["test"] = "{TODAY} {USER} {TICKET}" });
        var session = CreateSession("SUP-123");

        var result = await resolver.ExpandAsync("test", currentSession: session);

        Assert.True(result.WasExpanded);
        Assert.Contains(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"), result.ExpandedInput);
        Assert.Contains(Environment.UserName, result.ExpandedInput);
        Assert.Contains("SUP-123", result.ExpandedInput);
    }

    #endregion

    #region Alias Chaining Tests

    [Fact]
    public async Task ExpandAsync_AliasChaining_ExpandsRecursively()
    {
        var resolver = CreateResolver(new()
        {
            ["cq"] = "azure cosmos query",
            ["prod"] = "cq @prod-db"
        });

        var result = await resolver.ExpandAsync("prod \"SELECT * FROM c\"", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Equal("azure cosmos query @prod-db \"SELECT * FROM c\"", result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_CircularAlias_ReturnsError()
    {
        var resolver = CreateResolver(new()
        {
            ["a"] = "b foo",
            ["b"] = "a bar"
        });

        var result = await resolver.ExpandAsync("a test", currentSession: null);

        Assert.False(result.IsSuccess);
        Assert.Contains("Circular alias detected", result.Error);
    }

    [Fact]
    public async Task ExpandAsync_SelfReferencingAlias_ReturnsError()
    {
        var resolver = CreateResolver(new() { ["loop"] = "loop forever" });

        var result = await resolver.ExpandAsync("loop", currentSession: null);

        Assert.False(result.IsSuccess);
        Assert.Contains("Circular alias detected", result.Error);
    }

    [Fact]
    public async Task ExpandAsync_DeepChaining_Works()
    {
        var resolver = CreateResolver(new()
        {
            ["a"] = "b",
            ["b"] = "c",
            ["c"] = "d",
            ["d"] = "final-command"
        });

        var result = await resolver.ExpandAsync("a arg1 arg2", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Equal("final-command arg1 arg2", result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_ExceedsMaxDepth_ReturnsError()
    {
        var aliases = new Dictionary<string, string>();
        for (var i = 0; i < 15; i++)
            aliases[$"alias{i}"] = $"alias{i + 1}";
        aliases["alias15"] = "final";

        var resolver = CreateResolver(aliases);

        var result = await resolver.ExpandAsync("alias0", currentSession: null);

        Assert.False(result.IsSuccess);
        Assert.Contains("exceeded maximum depth", result.Error);
    }

    #endregion

    #region TestExpandAsync Tests

    [Fact]
    public async Task TestExpandAsync_ValidAlias_ReturnsExpansion()
    {
        var resolver = CreateResolver(new() { ["cq"] = "azure cosmos query" });

        var result = await resolver.TestExpandAsync(
            aliasName: "cq",
            arguments: ["@prod", "SELECT * FROM c"],
            currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Equal("azure cosmos query @prod SELECT * FROM c", result.ExpandedInput);
    }

    [Fact]
    public async Task TestExpandAsync_NonExistentAlias_ReturnsNoExpansion()
    {
        var resolver = CreateResolver([]);

        var result = await resolver.TestExpandAsync(
            aliasName: "nonexistent",
            arguments: [],
            currentSession: null);

        Assert.False(result.WasExpanded);
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Resource Alias Preservation Tests

    [Fact]
    public async Task ExpandAsync_ResourceAliasesPreserved()
    {
        var resolver = CreateResolver(new() { ["cq"] = "azure cosmos query" });

        var result = await resolver.ExpandAsync("cq @prod-users @staging-data", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Contains("@prod-users", result.ExpandedInput);
        Assert.Contains("@staging-data", result.ExpandedInput);
    }

    [Fact]
    public async Task ExpandAsync_ResourceAliasInExpansion_Preserved()
    {
        var resolver = CreateResolver(new() { ["prod"] = "azure cosmos query @prod-db" });

        var result = await resolver.ExpandAsync("prod \"SELECT * FROM c\"", currentSession: null);

        Assert.True(result.WasExpanded);
        Assert.Contains("@prod-db", result.ExpandedInput);
    }

    #endregion

    /// <summary>
    /// Mock implementation of IAliasService for testing.
    /// </summary>
    private sealed class MockAliasService : IAliasService
    {
        private readonly Dictionary<string, string> _aliases;

        public MockAliasService(Dictionary<string, string> aliases)
        {
            _aliases = new Dictionary<string, string>(aliases, StringComparer.OrdinalIgnoreCase);
        }

        public Task<AliasesConfiguration> LoadAliasesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AliasesConfiguration(_aliases));

        public Task<string?> GetAliasAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(_aliases.GetValueOrDefault(name));

        public Task AddAliasAsync(string name, string expansion, CancellationToken cancellationToken = default)
        {
            _aliases[name] = expansion;
            return Task.CompletedTask;
        }

        public Task<bool> RemoveAliasAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(_aliases.Remove(name));

        public IReadOnlyDictionary<string, string> GetAllAliases() => _aliases;

        public Task<AliasesConfiguration> ReloadAliasesAsync(CancellationToken cancellationToken = default) =>
            LoadAliasesAsync(cancellationToken);
    }
}
