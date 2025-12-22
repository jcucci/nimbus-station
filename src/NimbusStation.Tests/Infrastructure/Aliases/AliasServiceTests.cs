using Microsoft.Extensions.Logging;
using NimbusStation.Infrastructure.Aliases;

namespace NimbusStation.Tests.Infrastructure.Aliases;

public sealed class AliasServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _aliasesPath;
    private readonly ILogger<AliasService> _logger;

    public AliasServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"nimbus-alias-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        _aliasesPath = Path.Combine(_tempDirectory, "aliases.toml");
        _logger = new LoggerFactory().CreateLogger<AliasService>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    private AliasService CreateService() => new(_logger, _aliasesPath);

    [Fact]
    public async Task LoadAliasesAsync_NoFile_CreatesDefaultFile()
    {
        var service = CreateService();

        var config = await service.LoadAliasesAsync();

        Assert.Empty(config.Aliases);
        Assert.True(File.Exists(_aliasesPath));
    }

    [Fact]
    public async Task LoadAliasesAsync_ValidToml_ParsesAliases()
    {
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            cq = "azure cosmos query"
            bd = "azure blob download"
            """);
        var service = CreateService();

        var config = await service.LoadAliasesAsync();

        Assert.Equal(2, config.Aliases.Count);
        Assert.Equal("azure cosmos query", config.Aliases["cq"]);
        Assert.Equal("azure blob download", config.Aliases["bd"]);
    }

    [Fact]
    public async Task LoadAliasesAsync_CachesResult()
    {
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            cq = "azure cosmos query"
            """);
        var service = CreateService();

        var config1 = await service.LoadAliasesAsync();

        // Modify file after first load
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            different = "something else"
            """);

        var config2 = await service.LoadAliasesAsync();

        // Should still return cached result
        Assert.Equal(config1.Aliases.Count, config2.Aliases.Count);
        Assert.True(config2.Aliases.ContainsKey("cq"));
    }

    [Fact]
    public async Task ReloadAliasesAsync_RefreshesFromDisk()
    {
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            cq = "azure cosmos query"
            """);
        var service = CreateService();
        await service.LoadAliasesAsync();

        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            different = "something else"
            """);

        var config = await service.ReloadAliasesAsync();

        Assert.Single(config.Aliases);
        Assert.True(config.Aliases.ContainsKey("different"));
    }

    [Fact]
    public async Task GetAliasAsync_ExistingAlias_ReturnsExpansion()
    {
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            cq = "azure cosmos query"
            """);
        var service = CreateService();

        var expansion = await service.GetAliasAsync("cq");

        Assert.Equal("azure cosmos query", expansion);
    }

    [Fact]
    public async Task GetAliasAsync_NonExistentAlias_ReturnsNull()
    {
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            cq = "azure cosmos query"
            """);
        var service = CreateService();

        var expansion = await service.GetAliasAsync("nonexistent");

        Assert.Null(expansion);
    }

    [Fact]
    public async Task GetAliasAsync_CaseInsensitive()
    {
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            MyAlias = "azure cosmos query"
            """);
        var service = CreateService();

        Assert.Equal("azure cosmos query", await service.GetAliasAsync("myalias"));
        Assert.Equal("azure cosmos query", await service.GetAliasAsync("MYALIAS"));
        Assert.Equal("azure cosmos query", await service.GetAliasAsync("MyAlias"));
    }

    [Fact]
    public async Task AddAliasAsync_NewAlias_PersistsToFile()
    {
        var service = CreateService();

        await service.AddAliasAsync("cq", "azure cosmos query");

        var content = await File.ReadAllTextAsync(_aliasesPath);
        Assert.Contains("cq", content);
        Assert.Contains("azure cosmos query", content);

        var aliases = service.GetAllAliases();
        Assert.Single(aliases);
        Assert.Equal("azure cosmos query", aliases["cq"]);
    }

    [Fact]
    public async Task AddAliasAsync_UpdateExisting_OverwritesValue()
    {
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            cq = "old value"
            """);
        var service = CreateService();
        await service.LoadAliasesAsync();

        await service.AddAliasAsync("cq", "new value");

        var expansion = await service.GetAliasAsync("cq");
        Assert.Equal("new value", expansion);
    }

    [Fact]
    public async Task AddAliasAsync_InvalidName_ThrowsArgumentException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AddAliasAsync("session", "some expansion"));
    }

    [Fact]
    public async Task RemoveAliasAsync_ExistingAlias_RemovesAndReturnsTrue()
    {
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            cq = "azure cosmos query"
            bd = "azure blob download"
            """);
        var service = CreateService();
        await service.LoadAliasesAsync();

        var removed = await service.RemoveAliasAsync("cq");

        Assert.True(removed);
        Assert.Null(await service.GetAliasAsync("cq"));
        Assert.Equal("azure blob download", await service.GetAliasAsync("bd"));
    }

    [Fact]
    public async Task RemoveAliasAsync_NonExistentAlias_ReturnsFalse()
    {
        var service = CreateService();

        var removed = await service.RemoveAliasAsync("nonexistent");

        Assert.False(removed);
    }

    [Fact]
    public async Task GetAllAliases_ReturnsAllLoadedAliases()
    {
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            cq = "azure cosmos query"
            bd = "azure blob download"
            """);
        var service = CreateService();
        await service.LoadAliasesAsync();

        var aliases = service.GetAllAliases();

        Assert.Equal(2, aliases.Count);
    }

    [Fact]
    public async Task LoadAliasesAsync_InvalidToml_ReturnsEmptyAndLogsError()
    {
        await File.WriteAllTextAsync(_aliasesPath, "this is { not valid } toml");
        var service = CreateService();

        var config = await service.LoadAliasesAsync();

        Assert.Empty(config.Aliases);
    }

    [Fact]
    public async Task LoadAliasesAsync_SkipsInvalidAliasNames()
    {
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            valid = "good alias"
            session = "reserved name"
            "has space" = "invalid name"
            """);
        var service = CreateService();

        var config = await service.LoadAliasesAsync();

        Assert.Single(config.Aliases);
        Assert.True(config.Aliases.ContainsKey("valid"));
    }

    [Fact]
    public async Task LoadAliasesAsync_SkipsNonStringValues()
    {
        await File.WriteAllTextAsync(_aliasesPath, """
            [aliases]
            valid = "good alias"
            invalid = 123
            """);
        var service = CreateService();

        var config = await service.LoadAliasesAsync();

        Assert.Single(config.Aliases);
        Assert.True(config.Aliases.ContainsKey("valid"));
    }

    [Fact]
    public async Task AddAliasAsync_EscapesSpecialCharacters()
    {
        var service = CreateService();

        await service.AddAliasAsync("user", "azure cosmos query @prod \"SELECT * FROM c WHERE c.id = '{0}'\"");

        var content = await File.ReadAllTextAsync(_aliasesPath);
        Assert.Contains("\\\"SELECT", content); // Quotes should be escaped
    }
}
