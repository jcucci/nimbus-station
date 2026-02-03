using NimbusStation.Infrastructure.Configuration;

namespace NimbusStation.Tests.Infrastructure.Configuration;

public sealed class IncludeResolverTests
{
    private readonly IncludeResolver _resolver = new();

    [Fact]
    public void ResolvePath_AbsolutePath_ReturnsNormalized()
    {
        var result = _resolver.ResolvePath("/absolute/path/file.toml", "/base/config.toml");

        Assert.Equal(Path.GetFullPath("/absolute/path/file.toml"), result);
    }

    [Fact]
    public void ResolvePath_RelativePath_ResolvesFromBaseFile()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "nimbus", "config.toml");
        var result = _resolver.ResolvePath("generators.toml", basePath);

        var expected = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "nimbus", "generators.toml"));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolvePath_RelativePathWithSubdirectory_ResolvesCorrectly()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "nimbus", "config.toml");
        var result = _resolver.ResolvePath("includes/generators.toml", basePath);

        var expected = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "nimbus", "includes", "generators.toml"));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolvePath_TildePath_ExpandsToHomeDirectory()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var result = _resolver.ResolvePath("~/.config/nimbus/generators.toml", "/base/config.toml");

        var expected = Path.GetFullPath(Path.Combine(homeDir, ".config/nimbus/generators.toml"));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryMarkVisited_FirstVisit_ReturnsTrue()
    {
        var result = _resolver.TryMarkVisited("/path/to/file.toml");

        Assert.True(result);
    }

    [Fact]
    public void TryMarkVisited_SecondVisit_ReturnsFalse()
    {
        _resolver.TryMarkVisited("/path/to/file.toml");
        var result = _resolver.TryMarkVisited("/path/to/file.toml");

        Assert.False(result);
    }

    [Fact]
    public void TryMarkVisited_DifferentPaths_ReturnsTrue()
    {
        _resolver.TryMarkVisited("/path/to/file1.toml");
        var result = _resolver.TryMarkVisited("/path/to/file2.toml");

        Assert.True(result);
    }

    [Fact]
    public void HasVisited_AfterMarking_ReturnsTrue()
    {
        _resolver.TryMarkVisited("/path/to/file.toml");

        Assert.True(_resolver.HasVisited("/path/to/file.toml"));
    }

    [Fact]
    public void HasVisited_NotMarked_ReturnsFalse()
    {
        Assert.False(_resolver.HasVisited("/path/to/file.toml"));
    }

    [Fact]
    public void Reset_ClearsVisitedPaths()
    {
        _resolver.TryMarkVisited("/path/to/file.toml");
        _resolver.Reset();

        Assert.True(_resolver.TryMarkVisited("/path/to/file.toml"));
    }
}
