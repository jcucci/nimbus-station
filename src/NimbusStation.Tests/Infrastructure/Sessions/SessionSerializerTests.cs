using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Sessions;

namespace NimbusStation.Tests.Infrastructure.Sessions;

public sealed class SessionSerializerTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly SessionSerializer _serializer;

    public SessionSerializerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"nimbus-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        _serializer = new SessionSerializer();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    private string GetFilePath(string fileName = "session.json") =>
        Path.Combine(_tempDirectory, fileName);

    [Fact]
    public async Task WriteAndRead_RoundTrip_PreservesData()
    {
        var session = new Session(
            "SUP-123",
            new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 15, 14, 45, 0, TimeSpan.Zero),
            new SessionContext("prod-cosmos", "prod-blob"));

        var filePath = GetFilePath();

        await _serializer.WriteSessionAsync(session, filePath);
        var result = await _serializer.ReadSessionAsync(filePath);

        Assert.Equal(session.TicketId, result.TicketId);
        Assert.Equal(session.CreatedAt, result.CreatedAt);
        Assert.Equal(session.LastAccessedAt, result.LastAccessedAt);
        Assert.NotNull(result.ActiveContext);
        Assert.Equal(session.ActiveContext!.ActiveCosmosAlias, result.ActiveContext.ActiveCosmosAlias);
        Assert.Equal(session.ActiveContext.ActiveBlobAlias, result.ActiveContext.ActiveBlobAlias);
    }

    [Fact]
    public async Task WriteAndRead_NullContext_PreservesNull()
    {
        var session = Session.Create("SUP-456");
        var filePath = GetFilePath();

        await _serializer.WriteSessionAsync(session, filePath);
        var result = await _serializer.ReadSessionAsync(filePath);

        Assert.Equal(session.TicketId, result.TicketId);
        Assert.Null(result.ActiveContext);
    }

    [Fact]
    public async Task WriteAndRead_PartialContext_PreservesPartial()
    {
        var session = new Session(
            "SUP-789",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            new SessionContext("only-cosmos", null));

        var filePath = GetFilePath();

        await _serializer.WriteSessionAsync(session, filePath);
        var result = await _serializer.ReadSessionAsync(filePath);

        Assert.NotNull(result.ActiveContext);
        Assert.Equal("only-cosmos", result.ActiveContext.ActiveCosmosAlias);
        Assert.Null(result.ActiveContext.ActiveBlobAlias);
    }

    [Fact]
    public async Task ReadSessionAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var filePath = GetFilePath("nonexistent.json");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _serializer.ReadSessionAsync(filePath));
    }

    [Fact]
    public async Task WriteSessionAsync_CreatesValidJson()
    {
        var session = Session.Create("TEST-001");
        var filePath = GetFilePath();

        await _serializer.WriteSessionAsync(session, filePath);

        var json = await File.ReadAllTextAsync(filePath);
        Assert.Contains("\"ticketId\"", json);
        Assert.Contains("\"createdAt\"", json);
        Assert.Contains("\"lastAccessedAt\"", json);
        Assert.Contains("TEST-001", json);
    }
}
