using NimbusStation.Core.Search;
using NimbusStation.Providers.Azure.Blob;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Providers.Azure.Blob;

public sealed class BlobSearchServiceTests
{
    private readonly MockBlobService _blobService;
    private readonly BlobSearchService _searchService;

    public BlobSearchServiceTests()
    {
        _blobService = new MockBlobService();
        _searchService = new BlobSearchService(_blobService);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyResults_ReturnsEmptySearchResult()
    {
        _blobService.SetupBlobListResult(new BlobListResult([]));

        var result = await _searchService.SearchAsync("test-alias", prefix: null);

        Assert.Empty(result.Items);
        Assert.Equal(string.Empty, result.CurrentPrefix);
        Assert.False(result.IsTruncated);
    }

    [Fact]
    public async Task SearchAsync_WithNullPrefix_PassesNullToService()
    {
        _blobService.SetupBlobListResult(new BlobListResult([]));

        await _searchService.SearchAsync("test-alias", prefix: null);

        var call = Assert.Single(_blobService.ListBlobsCalls);
        Assert.Equal("test-alias", call.AliasName);
        Assert.Null(call.Prefix);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyPrefix_PassesNullToService()
    {
        _blobService.SetupBlobListResult(new BlobListResult([]));

        await _searchService.SearchAsync("test-alias", prefix: "");

        var call = Assert.Single(_blobService.ListBlobsCalls);
        Assert.Null(call.Prefix);
    }

    [Fact]
    public async Task SearchAsync_WithPrefix_PassesPrefixToService()
    {
        _blobService.SetupBlobListResult(new BlobListResult([]));

        await _searchService.SearchAsync("test-alias", prefix: "data/logs/");

        var call = Assert.Single(_blobService.ListBlobsCalls);
        Assert.Equal("data/logs/", call.Prefix);
    }

    [Fact]
    public async Task SearchAsync_LimitsResultsToDefault()
    {
        _blobService.SetupBlobListResult(new BlobListResult([]));

        await _searchService.SearchAsync("test-alias", prefix: null);

        var call = Assert.Single(_blobService.ListBlobsCalls);
        Assert.Equal(BlobSearchService.DefaultMaxResults, call.MaxResults);
    }

    [Fact]
    public async Task SearchAsync_ParsesBlobsIntoDirectoriesAndFiles()
    {
        var blobs = new List<BlobInfo>
        {
            new("data/logs/file1.json", 100, DateTimeOffset.UtcNow, "application/json"),
            new("data/exports/file2.json", 200, DateTimeOffset.UtcNow, "application/json"),
            new("config.json", 50, DateTimeOffset.UtcNow, "application/json")
        };
        _blobService.SetupBlobListResult(new BlobListResult(blobs));

        var result = await _searchService.SearchAsync("test-alias", prefix: null);

        Assert.Equal(2, result.Items.Count);

        var directory = result.Items.First(i => i.Kind == SearchItemKind.Directory);
        Assert.Equal("data/", directory.Name);

        var file = result.Items.First(i => i.Kind == SearchItemKind.File);
        Assert.Equal("config.json", file.Name);
    }

    [Fact]
    public async Task SearchAsync_WithPrefix_ReturnsItemsRelativeToPrefix()
    {
        var blobs = new List<BlobInfo>
        {
            new("data/logs/2024/file1.json", 100, DateTimeOffset.UtcNow, "application/json"),
            new("data/logs/summary.txt", 50, DateTimeOffset.UtcNow, "text/plain")
        };
        _blobService.SetupBlobListResult(new BlobListResult(blobs));

        var result = await _searchService.SearchAsync("test-alias", prefix: "data/logs/");

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("data/logs/", result.CurrentPrefix);

        var directory = result.Items.First(i => i.Kind == SearchItemKind.Directory);
        Assert.Equal("2024/", directory.Name);

        var file = result.Items.First(i => i.Kind == SearchItemKind.File);
        Assert.Equal("summary.txt", file.Name);
    }

    [Fact]
    public async Task SearchAsync_DirectoriesAppearBeforeFiles()
    {
        var blobs = new List<BlobInfo>
        {
            new("zebra.txt", 100, DateTimeOffset.UtcNow, "text/plain"),
            new("alpha/file.json", 200, DateTimeOffset.UtcNow, "application/json")
        };
        _blobService.SetupBlobListResult(new BlobListResult(blobs));

        var result = await _searchService.SearchAsync("test-alias", prefix: null);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(SearchItemKind.Directory, result.Items[0].Kind);
        Assert.Equal(SearchItemKind.File, result.Items[1].Kind);
    }

    [Fact]
    public async Task SearchAsync_WhenTruncated_SetsIsTruncatedFlag()
    {
        var blobs = new List<BlobInfo>
        {
            new("file.json", 100, DateTimeOffset.UtcNow, "application/json")
        };
        _blobService.SetupBlobListResult(new BlobListResult(blobs, IsTruncated: true));

        var result = await _searchService.SearchAsync("test-alias", prefix: null);

        Assert.True(result.IsTruncated);
    }

    [Fact]
    public async Task SearchAsync_WhenNotTruncated_ClearsIsTruncatedFlag()
    {
        var blobs = new List<BlobInfo>
        {
            new("file.json", 100, DateTimeOffset.UtcNow, "application/json")
        };
        _blobService.SetupBlobListResult(new BlobListResult(blobs, IsTruncated: false));

        var result = await _searchService.SearchAsync("test-alias", prefix: null);

        Assert.False(result.IsTruncated);
    }

    [Fact]
    public async Task SearchAsync_PreservesFileMetadata()
    {
        var modified = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var blobs = new List<BlobInfo>
        {
            new("file.json", 12345, modified, "application/json")
        };
        _blobService.SetupBlobListResult(new BlobListResult(blobs));

        var result = await _searchService.SearchAsync("test-alias", prefix: null);

        var file = Assert.Single(result.Items);
        Assert.Equal(12345, file.Size);
        Assert.Equal(modified, file.LastModified);
    }

    [Fact]
    public void Constructor_ThrowsOnNullBlobService()
    {
        Assert.Throws<ArgumentNullException>(() => new BlobSearchService(null!));
    }
}
