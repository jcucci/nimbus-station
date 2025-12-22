using Microsoft.Extensions.Logging;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Sessions;

namespace NimbusStation.Tests.Infrastructure.Sessions;

public sealed class SessionServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ILogger<SessionService> _logger;
    private readonly SessionService _service;

    public SessionServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"nimbus-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        _logger = new LoggerFactory().CreateLogger<SessionService>();
        _service = new SessionService(_logger, _tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    [Fact]
    public async Task StartSessionAsync_NewSession_CreatesDirectoryStructure()
    {
        var session = await _service.StartSessionAsync("SUP-123");

        Assert.Equal("SUP-123", session.TicketId);
        Assert.True(Directory.Exists(_service.GetSessionDirectory("SUP-123")));
        Assert.True(Directory.Exists(_service.GetDownloadsDirectory("SUP-123")));
        Assert.True(Directory.Exists(_service.GetQueriesDirectory("SUP-123")));
        Assert.True(File.Exists(Path.Combine(_service.GetSessionDirectory("SUP-123"), "session.json")));
    }

    [Fact]
    public async Task StartSessionAsync_ExistingSession_ResumesInstead()
    {
        var original = await _service.StartSessionAsync("SUP-123");
        await Task.Delay(10); // Ensure time passes

        var resumed = await _service.StartSessionAsync("SUP-123");

        Assert.Equal(original.TicketId, resumed.TicketId);
        Assert.Equal(original.CreatedAt, resumed.CreatedAt);
        Assert.True(resumed.LastAccessedAt > original.LastAccessedAt);
    }

    [Fact]
    public async Task StartSessionAsync_InvalidName_ThrowsInvalidSessionNameException()
    {
        await Assert.ThrowsAsync<InvalidSessionNameException>(
            () => _service.StartSessionAsync("invalid/name"));
    }

    [Fact]
    public async Task ListSessionsAsync_NoSessions_ReturnsEmptyList()
    {
        var sessions = await _service.ListSessionsAsync();

        Assert.Empty(sessions);
    }

    [Fact]
    public async Task ListSessionsAsync_MultipleSessions_ReturnsAllOrderedByLastAccessed()
    {
        await _service.StartSessionAsync("SUP-001");
        await Task.Delay(10);
        await _service.StartSessionAsync("SUP-002");
        await Task.Delay(10);
        await _service.StartSessionAsync("SUP-003");

        var sessions = await _service.ListSessionsAsync();

        Assert.Equal(3, sessions.Count);
        Assert.Equal("SUP-003", sessions[0].TicketId); // Most recent first
        Assert.Equal("SUP-002", sessions[1].TicketId);
        Assert.Equal("SUP-001", sessions[2].TicketId);
    }

    [Fact]
    public async Task ResumeSessionAsync_ExistingSession_UpdatesLastAccessedAt()
    {
        var original = await _service.StartSessionAsync("SUP-123");
        await Task.Delay(10);

        var resumed = await _service.ResumeSessionAsync("SUP-123");

        Assert.Equal(original.CreatedAt, resumed.CreatedAt);
        Assert.True(resumed.LastAccessedAt > original.LastAccessedAt);
    }

    [Fact]
    public async Task ResumeSessionAsync_NonExistent_ThrowsSessionNotFoundException()
    {
        await Assert.ThrowsAsync<SessionNotFoundException>(
            () => _service.ResumeSessionAsync("NONEXISTENT-999"));
    }

    [Fact]
    public async Task DeleteSessionAsync_ExistingSession_RemovesDirectory()
    {
        await _service.StartSessionAsync("SUP-123");
        Assert.True(_service.SessionExists("SUP-123"));

        await _service.DeleteSessionAsync("SUP-123");

        Assert.False(_service.SessionExists("SUP-123"));
        Assert.False(Directory.Exists(_service.GetSessionDirectory("SUP-123")));
    }

    [Fact]
    public async Task DeleteSessionAsync_NonExistent_ThrowsSessionNotFoundException()
    {
        await Assert.ThrowsAsync<SessionNotFoundException>(
            () => _service.DeleteSessionAsync("NONEXISTENT-999"));
    }

    [Fact]
    public async Task UpdateSessionContextAsync_ExistingSession_UpdatesContext()
    {
        await _service.StartSessionAsync("SUP-123");
        var context = new SessionContext("my-cosmos", "my-blob");

        var updated = await _service.UpdateSessionContextAsync("SUP-123", context);

        Assert.NotNull(updated.ActiveContext);
        Assert.Equal("my-cosmos", updated.ActiveContext.ActiveCosmosAlias);
        Assert.Equal("my-blob", updated.ActiveContext.ActiveBlobAlias);
    }

    [Fact]
    public async Task UpdateSessionContextAsync_NonExistent_ThrowsSessionNotFoundException()
    {
        var context = new SessionContext("my-cosmos", null);

        await Assert.ThrowsAsync<SessionNotFoundException>(
            () => _service.UpdateSessionContextAsync("NONEXISTENT-999", context));
    }

    [Fact]
    public async Task SessionExists_ExistingSession_ReturnsTrue()
    {
        await _service.StartSessionAsync("SUP-123");

        Assert.True(_service.SessionExists("SUP-123"));
    }

    [Fact]
    public void SessionExists_NonExistent_ReturnsFalse()
    {
        Assert.False(_service.SessionExists("NONEXISTENT-999"));
    }

    [Fact]
    public void GetSessionDirectory_ReturnsCorrectPath()
    {
        var path = _service.GetSessionDirectory("SUP-123");

        Assert.Equal(Path.Combine(_tempDirectory, "SUP-123"), path);
    }

    [Fact]
    public void GetDownloadsDirectory_ReturnsCorrectPath()
    {
        var path = _service.GetDownloadsDirectory("SUP-123");

        Assert.Equal(Path.Combine(_tempDirectory, "SUP-123", "downloads"), path);
    }

    [Fact]
    public void GetQueriesDirectory_ReturnsCorrectPath()
    {
        var path = _service.GetQueriesDirectory("SUP-123");

        Assert.Equal(Path.Combine(_tempDirectory, "SUP-123", "queries"), path);
    }
}
