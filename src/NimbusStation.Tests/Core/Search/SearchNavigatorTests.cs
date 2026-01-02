using NimbusStation.Core.Search;

namespace NimbusStation.Tests.Core.Search;

public sealed class SearchNavigatorTests
{
    private record TestBlob(string Path, long Size, DateTimeOffset Modified);

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("/", "")]
    [InlineData("data/", "")]
    [InlineData("data/logs/", "data/")]
    [InlineData("data/logs/2024/", "data/logs/")]
    [InlineData("data/logs/2024/01/", "data/logs/2024/")]
    public void GetParentPrefix_ReturnsParentPrefix(string? prefix, string expected)
    {
        var result = SearchNavigator.GetParentPrefix(prefix);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetParentPrefix_HandlesTrailingSlashCorrectly()
    {
        Assert.Equal("data/", SearchNavigator.GetParentPrefix("data/logs/"));
        Assert.Equal("data/", SearchNavigator.GetParentPrefix("data/logs"));
    }

    [Theory]
    [InlineData(null, "logs/", "logs/")]
    [InlineData("", "logs/", "logs/")]
    [InlineData("data/", "logs/", "data/logs/")]
    [InlineData("data", "logs/", "data/logs/")]
    [InlineData("data/logs/", "2024/", "data/logs/2024/")]
    public void CombinePrefix_CombinesPrefixWithSegment(string? prefix, string segment, string expected)
    {
        var result = SearchNavigator.CombinePrefix(prefix, segment);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("data/", false)]
    [InlineData("/", false)]
    public void IsRootPrefix_IdentifiesRootPrefix(string? prefix, bool expected)
    {
        var result = SearchNavigator.IsRootPrefix(prefix);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("file.json", null, "file.json")]
    [InlineData("file.json", "", "file.json")]
    [InlineData("data/logs/file.json", "data/logs/", "file.json")]
    [InlineData("data/logs/2024/", "data/logs/", "2024/")]
    [InlineData("unrelated/path.json", "data/logs/", "unrelated/path.json")]
    public void GetDisplayName_ExtractsDisplayName(string fullPath, string? currentPrefix, string expected)
    {
        var result = SearchNavigator.GetDisplayName(fullPath, currentPrefix);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("data", "data/")]
    [InlineData("data/", "data/")]
    [InlineData("data/logs", "data/logs/")]
    [InlineData("data/logs/", "data/logs/")]
    public void NormalizePrefix_NormalizesPrefix(string? prefix, string expected)
    {
        var result = SearchNavigator.NormalizePrefix(prefix);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseItems_ParsesEmptyList()
    {
        var blobs = Array.Empty<TestBlob>();

        var result = SearchNavigator.ParseItems(
            blobs,
            currentPrefix: null,
            pathSelector: b => b.Path,
            sizeSelector: b => b.Size,
            lastModifiedSelector: b => b.Modified);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseItems_ParsesFilesAtRoot()
    {
        var now = DateTimeOffset.UtcNow;
        var blobs = new[]
        {
            new TestBlob("file1.json", 100, now),
            new TestBlob("file2.txt", 200, now)
        };

        var result = SearchNavigator.ParseItems(
            blobs,
            currentPrefix: null,
            pathSelector: b => b.Path,
            sizeSelector: b => b.Size,
            lastModifiedSelector: b => b.Modified);

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal(SearchItemKind.File, item.Kind));
        Assert.Equal("file1.json", result[0].Name);
        Assert.Equal("file2.txt", result[1].Name);
    }

    [Fact]
    public void ParseItems_ParsesDirectoriesFromPaths()
    {
        var now = DateTimeOffset.UtcNow;
        var blobs = new[]
        {
            new TestBlob("data/logs/file1.json", 100, now),
            new TestBlob("data/exports/file2.json", 200, now),
            new TestBlob("config.json", 50, now)
        };

        var result = SearchNavigator.ParseItems(
            blobs,
            currentPrefix: null,
            pathSelector: b => b.Path,
            sizeSelector: b => b.Size,
            lastModifiedSelector: b => b.Modified);

        Assert.Equal(2, result.Count);

        var directory = result.First(r => r.Kind == SearchItemKind.Directory);
        Assert.Equal("data/", directory.Name);
        Assert.Equal("data/", directory.FullPath);

        var file = result.First(r => r.Kind == SearchItemKind.File);
        Assert.Equal("config.json", file.Name);
    }

    [Fact]
    public void ParseItems_ParsesItemsWithPrefix()
    {
        var now = DateTimeOffset.UtcNow;
        var blobs = new[]
        {
            new TestBlob("data/logs/2024/file1.json", 100, now),
            new TestBlob("data/logs/2023/file2.json", 200, now),
            new TestBlob("data/logs/summary.txt", 50, now)
        };

        var result = SearchNavigator.ParseItems(
            blobs,
            currentPrefix: "data/logs/",
            pathSelector: b => b.Path,
            sizeSelector: b => b.Size,
            lastModifiedSelector: b => b.Modified);

        Assert.Equal(3, result.Count);

        var directories = result.Where(r => r.Kind == SearchItemKind.Directory).ToList();
        Assert.Equal(2, directories.Count);
        Assert.Contains(directories, d => d.Name == "2023/");
        Assert.Contains(directories, d => d.Name == "2024/");

        var file = result.Single(r => r.Kind == SearchItemKind.File);
        Assert.Equal("summary.txt", file.Name);
        Assert.Equal("data/logs/summary.txt", file.FullPath);
    }

    [Fact]
    public void ParseItems_DirectoriesAppearBeforeFiles()
    {
        var now = DateTimeOffset.UtcNow;
        var blobs = new[]
        {
            new TestBlob("zebra.txt", 100, now),
            new TestBlob("alpha/file.json", 200, now),
            new TestBlob("beta/file.json", 300, now),
            new TestBlob("apple.txt", 50, now)
        };

        var result = SearchNavigator.ParseItems(
            blobs,
            currentPrefix: null,
            pathSelector: b => b.Path,
            sizeSelector: b => b.Size,
            lastModifiedSelector: b => b.Modified);

        Assert.Equal(4, result.Count);
        Assert.Equal(SearchItemKind.Directory, result[0].Kind);
        Assert.Equal(SearchItemKind.Directory, result[1].Kind);
        Assert.Equal(SearchItemKind.File, result[2].Kind);
        Assert.Equal(SearchItemKind.File, result[3].Kind);

        Assert.Equal("alpha/", result[0].Name);
        Assert.Equal("beta/", result[1].Name);
        Assert.Equal("apple.txt", result[2].Name);
        Assert.Equal("zebra.txt", result[3].Name);
    }

    [Fact]
    public void ParseItems_DeduplicatesDirectories()
    {
        var now = DateTimeOffset.UtcNow;
        var blobs = new[]
        {
            new TestBlob("data/file1.json", 100, now),
            new TestBlob("data/file2.json", 200, now),
            new TestBlob("data/nested/file3.json", 300, now)
        };

        var result = SearchNavigator.ParseItems(
            blobs,
            currentPrefix: null,
            pathSelector: b => b.Path,
            sizeSelector: b => b.Size,
            lastModifiedSelector: b => b.Modified);

        Assert.Single(result);
        Assert.Equal("data/", result[0].Name);
        Assert.Equal(SearchItemKind.Directory, result[0].Kind);
    }

    [Fact]
    public void ParseItems_FiltersItemsNotMatchingPrefix()
    {
        var now = DateTimeOffset.UtcNow;
        var blobs = new[]
        {
            new TestBlob("data/logs/file.json", 100, now),
            new TestBlob("other/file.json", 200, now)
        };

        var result = SearchNavigator.ParseItems(
            blobs,
            currentPrefix: "data/",
            pathSelector: b => b.Path,
            sizeSelector: b => b.Size,
            lastModifiedSelector: b => b.Modified);

        Assert.Single(result);
        Assert.Equal("logs/", result[0].Name);
    }

    [Fact]
    public void ParseItems_PreservesFileMetadata()
    {
        var modified = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var blobs = new[]
        {
            new TestBlob("file.json", 12345, modified)
        };

        var result = SearchNavigator.ParseItems(
            blobs,
            currentPrefix: null,
            pathSelector: b => b.Path,
            sizeSelector: b => b.Size,
            lastModifiedSelector: b => b.Modified);

        var file = Assert.Single(result);
        Assert.Equal(12345, file.Size);
        Assert.Equal(modified, file.LastModified);
    }
}
