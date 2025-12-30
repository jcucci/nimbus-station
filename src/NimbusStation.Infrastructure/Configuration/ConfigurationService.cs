using Microsoft.Extensions.Logging;
using Tomlyn;
using Tomlyn.Model;

namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Service for loading and managing application configuration from TOML files.
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    private const string DefaultConfigTemplate =
        """
        # Nimbus Station Configuration
        # See: https://github.com/jcucci/nimbus-station

        # Theme settings for terminal output
        # [theme]
        # prompt_color = "green"
        # table_header_color = "blue"
        # json_key_color = "cyan"

        # CosmosDB connection aliases
        # [aliases.cosmos]
        # my-alias = { endpoint = "https://example.documents.azure.com:443/", database = "MyDb", container = "MyContainer" }

        # Blob storage aliases (container-level operations)
        # [aliases.blob]
        # my-storage = { account = "mystorageaccount", container = "mycontainer" }

        # Storage account aliases (account-level operations like container listing)
        # [aliases.storage]
        # prod = { account = "prodstorageaccount" }
        """;

    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configPath;
    private NimbusConfiguration? _cachedConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ConfigurationService(ILogger<ConfigurationService> logger)
        : this(logger, GetDefaultConfigPath())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class with a custom config path.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configPath">The path to the configuration file.</param>
    public ConfigurationService(ILogger<ConfigurationService> logger, string configPath)
    {
        _logger = logger;
        _configPath = configPath;
    }

    /// <inheritdoc/>
    public async Task<NimbusConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedConfiguration is not null)
        {
            return _cachedConfiguration;
        }

        var config = new NimbusConfiguration();

        if (!File.Exists(_configPath))
        {
            await CreateDefaultConfigFileAsync(cancellationToken);
            _cachedConfiguration = config;
            return config;
        }

        try
        {
            var tomlContent = await File.ReadAllTextAsync(_configPath, cancellationToken);
            var tomlTable = Toml.ToModel(tomlContent);

            ParseTheme(tomlTable, config);
            ParseCosmosAliases(tomlTable, config);
            ParseBlobAliases(tomlTable, config);
            ParseStorageAliases(tomlTable, config);
        }
        catch (TomlException ex)
        {
            _logger.LogError(ex, "Failed to parse TOML configuration at {ConfigPath}", _configPath);
        }

        _cachedConfiguration = config;
        return config;
    }

    /// <inheritdoc/>
    public CosmosAliasConfig? GetCosmosAlias(string name)
    {
        if (_cachedConfiguration is null)
        {
            _logger.LogWarning("Configuration not loaded. Call LoadConfigurationAsync first.");
            return null;
        }

        return _cachedConfiguration.CosmosAliases.GetValueOrDefault(name);
    }

    /// <inheritdoc/>
    public BlobAliasConfig? GetBlobAlias(string name)
    {
        if (_cachedConfiguration is null)
        {
            _logger.LogWarning("Configuration not loaded. Call LoadConfigurationAsync first.");
            return null;
        }

        return _cachedConfiguration.BlobAliases.GetValueOrDefault(name);
    }

    /// <inheritdoc/>
    public StorageAliasConfig? GetStorageAlias(string name)
    {
        if (_cachedConfiguration is null)
        {
            _logger.LogWarning("Configuration not loaded. Call LoadConfigurationAsync first.");
            return null;
        }

        return _cachedConfiguration.StorageAliases.GetValueOrDefault(name);
    }

    /// <inheritdoc/>
    public ThemeConfig GetTheme()
    {
        return _cachedConfiguration?.Theme ?? ThemeConfig.Default;
    }

    private static string GetDefaultConfigPath()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrEmpty(configHome))
        {
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }

        return Path.Combine(configHome, "nimbus", "config.toml");
    }

    private async Task CreateDefaultConfigFileAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(_configPath, DefaultConfigTemplate, cancellationToken);
        _logger.LogInformation("Created default configuration file at {ConfigPath}", _configPath);
    }

    private void ParseTheme(TomlTable tomlTable, NimbusConfiguration config)
    {
        if (!tomlTable.TryGetValue("theme", out var themeObj) || themeObj is not TomlTable themeTable)
        {
            return;
        }

        var promptColor = GetStringValue(themeTable, "prompt_color") ?? ThemeConfig.Default.PromptColor;
        var tableHeaderColor = GetStringValue(themeTable, "table_header_color") ?? ThemeConfig.Default.TableHeaderColor;
        var jsonKeyColor = GetStringValue(themeTable, "json_key_color") ?? ThemeConfig.Default.JsonKeyColor;

        config.Theme = new ThemeConfig(promptColor, tableHeaderColor, jsonKeyColor);
    }

    private void ParseCosmosAliases(TomlTable tomlTable, NimbusConfiguration config)
    {
        if (!tomlTable.TryGetValue("aliases", out var aliasesObj) || aliasesObj is not TomlTable aliasesTable)
        {
            return;
        }

        if (!aliasesTable.TryGetValue("cosmos", out var cosmosObj) || cosmosObj is not TomlTable cosmosTable)
        {
            return;
        }

        foreach (var (aliasName, aliasValue) in cosmosTable)
        {
            if (aliasValue is not TomlTable aliasTable)
            {
                _logger.LogWarning("Skipping Cosmos alias '{AliasName}': expected a table", aliasName);
                continue;
            }

            var endpoint = GetStringValue(aliasTable, "endpoint");
            var database = GetStringValue(aliasTable, "database");
            var container = GetStringValue(aliasTable, "container");

            var missingFields = new List<string>();
            if (string.IsNullOrEmpty(endpoint)) missingFields.Add("endpoint");
            if (string.IsNullOrEmpty(database)) missingFields.Add("database");
            if (string.IsNullOrEmpty(container)) missingFields.Add("container");

            if (missingFields.Count > 0)
            {
                _logger.LogWarning(
                    "Skipping Cosmos alias '{AliasName}': missing required field(s) '{MissingFields}'",
                    aliasName,
                    string.Join("', '", missingFields));
                continue;
            }

            config.CosmosAliases[aliasName] = new CosmosAliasConfig(endpoint!, database!, container!);
        }
    }

    private void ParseBlobAliases(TomlTable tomlTable, NimbusConfiguration config)
    {
        if (!tomlTable.TryGetValue("aliases", out var aliasesObj) || aliasesObj is not TomlTable aliasesTable)
        {
            return;
        }

        if (!aliasesTable.TryGetValue("blob", out var blobObj) || blobObj is not TomlTable blobTable)
        {
            return;
        }

        foreach (var (aliasName, aliasValue) in blobTable)
        {
            if (aliasValue is not TomlTable aliasTable)
            {
                _logger.LogWarning("Skipping Blob alias '{AliasName}': expected a table", aliasName);
                continue;
            }

            var account = GetStringValue(aliasTable, "account");
            var container = GetStringValue(aliasTable, "container");

            var missingFields = new List<string>();
            if (string.IsNullOrEmpty(account)) missingFields.Add("account");
            if (string.IsNullOrEmpty(container)) missingFields.Add("container");

            if (missingFields.Count > 0)
            {
                _logger.LogWarning(
                    "Skipping Blob alias '{AliasName}': missing required field(s) '{MissingFields}'",
                    aliasName,
                    string.Join("', '", missingFields));
                continue;
            }

            config.BlobAliases[aliasName] = new BlobAliasConfig(account!, container!);
        }
    }

    private void ParseStorageAliases(TomlTable tomlTable, NimbusConfiguration config)
    {
        if (!tomlTable.TryGetValue("aliases", out var aliasesObj) || aliasesObj is not TomlTable aliasesTable)
        {
            return;
        }

        if (!aliasesTable.TryGetValue("storage", out var storageObj) || storageObj is not TomlTable storageTable)
        {
            return;
        }

        foreach (var (aliasName, aliasValue) in storageTable)
        {
            if (aliasValue is not TomlTable aliasTable)
            {
                _logger.LogWarning("Skipping Storage alias '{AliasName}': expected a table", aliasName);
                continue;
            }

            var account = GetStringValue(aliasTable, "account");

            if (string.IsNullOrEmpty(account))
            {
                _logger.LogWarning(
                    "Skipping Storage alias '{AliasName}': missing required field 'account'",
                    aliasName);
                continue;
            }

            config.StorageAliases[aliasName] = new StorageAliasConfig(account);
        }
    }

    private static string? GetStringValue(TomlTable table, string key)
    {
        return table.TryGetValue(key, out var value) && value is string str ? str : null;
    }
}
