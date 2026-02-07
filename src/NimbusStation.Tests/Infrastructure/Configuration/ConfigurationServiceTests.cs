using Microsoft.Extensions.Logging;
using NimbusStation.Infrastructure.Configuration;

namespace NimbusStation.Tests.Infrastructure.Configuration;

public sealed class ConfigurationServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly LoggerFactory _loggerFactory;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"nimbus-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        _loggerFactory = new LoggerFactory();
        _logger = _loggerFactory.CreateLogger<ConfigurationService>();
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
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

    [Fact]
    public async Task LoadConfigurationAsync_WithInclude_MergesIncludedFile()
    {
        // Arrange - create included file
        const string includedToml =
            """
            [aliases.cosmos]
            included-alias = { endpoint = "https://included.documents.azure.com:443/", database = "IncDb", container = "IncContainer" }
            """;
        await WriteConfigAsync(includedToml, "included.toml");

        // Create main config with include
        const string mainToml =
            """
            [include]
            files = ["included.toml"]

            [aliases.cosmos]
            main-alias = { endpoint = "https://main.documents.azure.com:443/", database = "MainDb", container = "MainContainer" }
            """;
        await WriteConfigAsync(mainToml);

        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert - should have both aliases
        Assert.Equal(2, config.CosmosAliases.Count);
        Assert.True(config.CosmosAliases.ContainsKey("included-alias"));
        Assert.True(config.CosmosAliases.ContainsKey("main-alias"));
    }

    [Fact]
    public async Task LoadConfigurationAsync_MainConfigOverridesIncluded()
    {
        // Arrange - create included file with an alias
        const string includedToml =
            """
            [aliases.cosmos]
            shared-alias = { endpoint = "https://included.documents.azure.com:443/", database = "IncDb", container = "IncContainer" }
            """;
        await WriteConfigAsync(includedToml, "included.toml");

        // Create main config that overrides the alias
        const string mainToml =
            """
            [include]
            files = ["included.toml"]

            [aliases.cosmos]
            shared-alias = { endpoint = "https://main.documents.azure.com:443/", database = "MainDb", container = "MainContainer" }
            """;
        await WriteConfigAsync(mainToml);

        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert - main config should override included
        Assert.Single(config.CosmosAliases);
        Assert.Equal("https://main.documents.azure.com:443/", config.CosmosAliases["shared-alias"].Endpoint);
        Assert.Equal("MainDb", config.CosmosAliases["shared-alias"].Database);
    }

    [Fact]
    public async Task LoadConfigurationAsync_CircularInclude_HandledGracefully()
    {
        // Arrange - create circular include
        const string file1 =
            """
            [include]
            files = ["file2.toml"]

            [aliases.cosmos]
            alias1 = { endpoint = "https://one.documents.azure.com:443/", database = "Db1", container = "C1" }
            """;
        await WriteConfigAsync(file1, "file1.toml");

        const string file2 =
            """
            [include]
            files = ["file1.toml"]

            [aliases.cosmos]
            alias2 = { endpoint = "https://two.documents.azure.com:443/", database = "Db2", container = "C2" }
            """;
        await WriteConfigAsync(file2, "file2.toml");

        var service = new ConfigurationService(_logger, GetConfigPath("file1.toml"));

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert - should not crash, should have aliases from both files
        Assert.Equal(2, config.CosmosAliases.Count);
    }

    [Fact]
    public async Task LoadConfigurationAsync_MissingIncludeFile_ContinuesWithWarning()
    {
        // Arrange
        const string mainToml =
            """
            [include]
            files = ["nonexistent.toml"]

            [aliases.cosmos]
            main-alias = { endpoint = "https://main.documents.azure.com:443/", database = "MainDb", container = "MainContainer" }
            """;
        await WriteConfigAsync(mainToml);

        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert - should still load main config
        Assert.Single(config.CosmosAliases);
        Assert.True(config.CosmosAliases.ContainsKey("main-alias"));
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithGenerators_GeneratesAliases()
    {
        // Arrange
        const string toml =
            """
            [generators.kingdoms]
            ninja = { abbrev = "nja" }

            [generators.backends]
            activities = { database = "activities-db" }

            [generators.cosmos]
            enabled = true
            alias_name_template = "{kingdoms}-{backends}-{type}"
            endpoint_template = "https://king-{abbrev}-be.documents.azure.com:443/"
            database_template = "{database}"

            [generators.cosmos.types]
            event = "event-store"
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        Assert.Single(config.CosmosAliases);
        Assert.True(config.CosmosAliases.TryGetValue("ninja-activities-event", out var alias));
        Assert.Equal("https://king-nja-be.documents.azure.com:443/", alias.Endpoint);
        Assert.Equal("activities-db", alias.Database);
        Assert.Equal("event-store", alias.Container);
    }

    [Fact]
    public async Task LoadConfigurationAsync_ExplicitAliasOverridesGenerated()
    {
        // Arrange
        const string toml =
            """
            [generators.kingdoms]
            ninja = { abbrev = "nja" }

            [generators.backends]
            activities = { database = "activities-db" }

            [generators.cosmos]
            enabled = true
            alias_name_template = "{kingdoms}-{backends}-{type}"
            endpoint_template = "https://king-{abbrev}-be.documents.azure.com:443/"
            database_template = "{database}"

            [generators.cosmos.types]
            event = "event-store"

            [aliases.cosmos]
            ninja-activities-event = { endpoint = "https://override.documents.azure.com:443/", database = "OverrideDb", container = "OverrideContainer" }
            """;

        await WriteConfigAsync(toml);
        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert - explicit should override generated
        Assert.Single(config.CosmosAliases);
        var alias = config.CosmosAliases["ninja-activities-event"];
        Assert.Equal("https://override.documents.azure.com:443/", alias.Endpoint);
        Assert.Equal("OverrideDb", alias.Database);
        Assert.Equal("OverrideContainer", alias.Container);
    }

    [Fact]
    public async Task LoadConfigurationAsync_GeneratorsInIncludedFile_GeneratesAliases()
    {
        // Arrange - create generators file
        const string generatorsToml =
            """
            [generators.kingdoms]
            ninja = { abbrev = "nja" }

            [generators.backends]
            activities = { database = "activities-db" }

            [generators.cosmos]
            enabled = true
            alias_name_template = "{kingdoms}-{backends}-{type}"
            endpoint_template = "https://king-{abbrev}-be.documents.azure.com:443/"
            database_template = "{database}"

            [generators.cosmos.types]
            event = "event-store"
            """;
        await WriteConfigAsync(generatorsToml, "generators.toml");

        // Create main config with include
        const string mainToml =
            """
            [include]
            files = ["generators.toml"]

            [theme]
            prompt_color = "cyan"
            """;
        await WriteConfigAsync(mainToml);

        var service = new ConfigurationService(_logger, GetConfigPath());

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        Assert.Single(config.CosmosAliases);
        Assert.True(config.CosmosAliases.ContainsKey("ninja-activities-event"));
        Assert.Equal("cyan", config.Theme.PromptColor);
    }
}
