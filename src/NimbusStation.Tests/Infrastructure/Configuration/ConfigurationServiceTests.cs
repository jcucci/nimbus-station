using Microsoft.Extensions.Logging;
using NimbusStation.Infrastructure.Configuration;

namespace NimbusStation.Tests.Infrastructure.Configuration;

public sealed class ConfigurationServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"nimbus-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        _logger = new LoggerFactory().CreateLogger<ConfigurationService>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private string GetConfigPath(string fileName = "config.toml")
    {
        return Path.Combine(_tempDirectory, fileName);
    }

    private async Task WriteConfigAsync(string content, string fileName = "config.toml")
    {
        await File.WriteAllTextAsync(GetConfigPath(fileName), content);
    }

    [Fact]
    public async Task LoadConfigurationAsync_MissingFile_CreatesDefaultAndReturnsEmptyConfig()
    {
        // Arrange
        var configPath = GetConfigPath();
        var service = new ConfigurationService(_logger, configPath);

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        Assert.True(File.Exists(configPath));
        Assert.Empty(config.CosmosAliases);
        Assert.Empty(config.BlobAliases);
        Assert.Equal(ThemeConfig.Default, config.Theme);
    }

    [Fact]
    public async Task LoadConfigurationAsync_ValidToml_ParsesAllSections()
    {
        // Arrange
        const string toml =
            """
            [theme]
            prompt_color = "red"
            table_header_color = "yellow"
            json_key_color = "magenta"

            [aliases.cosmos]
            prod-main = { endpoint = "https://prod.documents.azure.com:443/", database = "MainDb", container = "Users" }

            [aliases.blob]
            prod-logs = { account = "prodlogs", container = "applogs" }
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        Assert.Equal("red", config.Theme.PromptColor);
        Assert.Equal("yellow", config.Theme.TableHeaderColor);
        Assert.Equal("magenta", config.Theme.JsonKeyColor);

        Assert.Single(config.CosmosAliases);
        Assert.True(config.CosmosAliases.ContainsKey("prod-main"));
        Assert.Equal("https://prod.documents.azure.com:443/", config.CosmosAliases["prod-main"].Endpoint);
        Assert.Equal("MainDb", config.CosmosAliases["prod-main"].Database);
        Assert.Equal("Users", config.CosmosAliases["prod-main"].Container);

        Assert.Single(config.BlobAliases);
        Assert.True(config.BlobAliases.ContainsKey("prod-logs"));
        Assert.Equal("prodlogs", config.BlobAliases["prod-logs"].Account);
        Assert.Equal("applogs", config.BlobAliases["prod-logs"].Container);
    }

    [Fact]
    public async Task LoadConfigurationAsync_PartialTheme_MergesWithDefaults()
    {
        // Arrange
        const string toml =
            """
            [theme]
            prompt_color = "red"
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        Assert.Equal("red", config.Theme.PromptColor);
        Assert.Equal(ThemeConfig.Default.TableHeaderColor, config.Theme.TableHeaderColor);
        Assert.Equal(ThemeConfig.Default.JsonKeyColor, config.Theme.JsonKeyColor);
    }

    [Fact]
    public async Task LoadConfigurationAsync_InvalidCosmosAlias_SkipsInvalidEntry()
    {
        // Arrange
        const string toml =
            """
            [aliases.cosmos]
            valid-alias = { endpoint = "https://valid.documents.azure.com:443/", database = "Db", container = "Container" }
            missing-container = { endpoint = "https://invalid.documents.azure.com:443/", database = "Db" }
            missing-database = { endpoint = "https://invalid2.documents.azure.com:443/", container = "Container" }
            missing-endpoint = { database = "Db", container = "Container" }
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        Assert.Single(config.CosmosAliases);
        Assert.True(config.CosmosAliases.ContainsKey("valid-alias"));
        Assert.False(config.CosmosAliases.ContainsKey("missing-container"));
        Assert.False(config.CosmosAliases.ContainsKey("missing-database"));
        Assert.False(config.CosmosAliases.ContainsKey("missing-endpoint"));
    }

    [Fact]
    public async Task LoadConfigurationAsync_InvalidBlobAlias_SkipsInvalidEntry()
    {
        // Arrange
        const string toml =
            """
            [aliases.blob]
            valid-alias = { account = "validaccount", container = "validcontainer" }
            missing-container = { account = "someaccount" }
            missing-account = { container = "somecontainer" }
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        Assert.Single(config.BlobAliases);
        Assert.True(config.BlobAliases.ContainsKey("valid-alias"));
        Assert.False(config.BlobAliases.ContainsKey("missing-container"));
        Assert.False(config.BlobAliases.ContainsKey("missing-account"));
    }

    [Fact]
    public async Task GetCosmosAlias_ExistingAlias_ReturnsConfig()
    {
        // Arrange
        const string toml =
            """
            [aliases.cosmos]
            my-alias = { endpoint = "https://my.documents.azure.com:443/", database = "MyDb", container = "MyContainer" }
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());
        await service.LoadConfigurationAsync();

        // Act
        var alias = service.GetCosmosAlias("my-alias");

        // Assert
        Assert.NotNull(alias);
        Assert.Equal("https://my.documents.azure.com:443/", alias.Endpoint);
        Assert.Equal("MyDb", alias.Database);
        Assert.Equal("MyContainer", alias.Container);
    }

    [Fact]
    public async Task GetCosmosAlias_NonExistent_ReturnsNull()
    {
        // Arrange
        const string toml =
            """
            [aliases.cosmos]
            my-alias = { endpoint = "https://my.documents.azure.com:443/", database = "MyDb", container = "MyContainer" }
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());
        await service.LoadConfigurationAsync();

        // Act
        var alias = service.GetCosmosAlias("non-existent");

        // Assert
        Assert.Null(alias);
    }

    [Fact]
    public async Task GetBlobAlias_ExistingAlias_ReturnsConfig()
    {
        // Arrange
        const string toml =
            """
            [aliases.blob]
            my-storage = { account = "myaccount", container = "mycontainer" }
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());
        await service.LoadConfigurationAsync();

        // Act
        var alias = service.GetBlobAlias("my-storage");

        // Assert
        Assert.NotNull(alias);
        Assert.Equal("myaccount", alias.Account);
        Assert.Equal("mycontainer", alias.Container);
    }

    [Fact]
    public async Task GetBlobAlias_NonExistent_ReturnsNull()
    {
        // Arrange
        const string toml =
            """
            [aliases.blob]
            my-storage = { account = "myaccount", container = "mycontainer" }
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());
        await service.LoadConfigurationAsync();

        // Act
        var alias = service.GetBlobAlias("non-existent");

        // Assert
        Assert.Null(alias);
    }

    [Fact]
    public async Task GetTheme_NoThemeSection_ReturnsDefaults()
    {
        // Arrange
        const string toml =
            """
            [aliases.cosmos]
            my-alias = { endpoint = "https://my.documents.azure.com:443/", database = "MyDb", container = "MyContainer" }
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());
        await service.LoadConfigurationAsync();

        // Act
        var theme = service.GetTheme();

        // Assert
        Assert.Equal(ThemeConfig.Default, theme);
    }

    [Fact]
    public async Task GetTheme_BeforeLoad_ReturnsDefaults()
    {
        // Arrange
        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var theme = service.GetTheme();

        // Assert
        Assert.Equal(ThemeConfig.Default, theme);
    }

    [Fact]
    public async Task LoadConfigurationAsync_MultipleCosmosAliases_ParsesAll()
    {
        // Arrange
        const string toml =
            """
            [aliases.cosmos]
            alias1 = { endpoint = "https://one.documents.azure.com:443/", database = "Db1", container = "Container1" }
            alias2 = { endpoint = "https://two.documents.azure.com:443/", database = "Db2", container = "Container2" }
            alias3 = { endpoint = "https://three.documents.azure.com:443/", database = "Db3", container = "Container3" }
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        Assert.Equal(3, config.CosmosAliases.Count);
        Assert.True(config.CosmosAliases.ContainsKey("alias1"));
        Assert.True(config.CosmosAliases.ContainsKey("alias2"));
        Assert.True(config.CosmosAliases.ContainsKey("alias3"));
    }

    [Fact]
    public async Task LoadConfigurationAsync_CachesConfiguration()
    {
        // Arrange
        const string toml =
            """
            [theme]
            prompt_color = "red"
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config1 = await service.LoadConfigurationAsync();

        // Modify file after first load
        await WriteConfigAsync("[theme]\nprompt_color = \"blue\"");

        var config2 = await service.LoadConfigurationAsync();

        // Assert - should return cached config, not re-read file
        Assert.Same(config1, config2);
        Assert.Equal("red", config2.Theme.PromptColor);
    }
}
