using Microsoft.Extensions.Logging;
using NimbusStation.Infrastructure.Configuration.Generators;
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
        # Use a preset theme as a base (optional):
        # preset = "catppuccin-mocha"
        #
        # Available presets: default, catppuccin-mocha, catppuccin-macchiato,
        # catppuccin-frappe, catppuccin-latte, dracula, one-dark, gruvbox-dark,
        # gruvbox-light, ayu-dark, ayu-mirage, ayu-light, github-dark, github-light,
        # xcode-dark, xcode-light, tokyonight-night, tokyonight-storm, tokyonight-day,
        # nord, vscode-dark, vscode-light, material-darker, material-ocean,
        # material-palenight, solarized-dark, solarized-light
        #
        # Override individual colors (supports color names, hex codes like "#50FA7B",
        # or rgb values like "rgb(80,250,123)"):
        # prompt_color = "green"
        # prompt_session_color = "cyan"
        # prompt_context_color = "yellow"
        # prompt_cosmos_alias_color = "orange1"
        # prompt_blob_alias_color = "magenta"
        # table_header_color = "blue"
        # table_border_color = "grey"
        # error_color = "red"
        # warning_color = "yellow"
        # success_color = "green"
        # dim_color = "grey"
        # json_key_color = "cyan"
        # json_string_color = "green"
        # json_number_color = "magenta"
        # json_boolean_color = "yellow"
        # json_null_color = "grey"
        # banner_color = "cyan1"

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

        var includeResolver = new IncludeResolver();
        await LoadConfigurationRecursiveAsync(_configPath, config, includeResolver, cancellationToken);

        _cachedConfiguration = config;
        return config;
    }

    private async Task LoadConfigurationRecursiveAsync(
        string filePath,
        NimbusConfiguration config,
        IncludeResolver includeResolver,
        CancellationToken cancellationToken)
    {
        var resolvedPath = Path.GetFullPath(filePath);

        if (!includeResolver.TryMarkVisited(resolvedPath))
        {
            _logger.LogWarning("Circular include detected for {FilePath}, skipping", filePath);
            return;
        }

        if (!File.Exists(resolvedPath))
        {
            _logger.LogWarning("Include file not found: {FilePath}", filePath);
            return;
        }

        try
        {
            var tomlContent = await File.ReadAllTextAsync(resolvedPath, cancellationToken);
            var tomlTable = Toml.ToModel(tomlContent);

            // Parse includes first and load them (they become the base that this file overrides)
            var includeFiles = ParseIncludes(tomlTable);
            foreach (var includeFile in includeFiles)
            {
                var includePath = includeResolver.ResolvePath(includeFile, resolvedPath);
                await LoadConfigurationRecursiveAsync(includePath, config, includeResolver, cancellationToken);
            }

            // Parse current file's configuration (overrides included files)
            var fileConfig = new NimbusConfiguration();
            ParseTheme(tomlTable, fileConfig);
            ParseCosmosAliases(tomlTable, fileConfig);
            ParseBlobAliases(tomlTable, fileConfig);
            ParseStorageAliases(tomlTable, fileConfig);

            // Parse generators and generate aliases
            var generators = ParseGenerators(tomlTable);
            if (generators is not null)
            {
                var aliasGenerator = new AliasGenerator(_logger);
                var generatedConfig = aliasGenerator.GenerateAliases(generators);

                // Merge generated aliases first (lowest priority)
                ConfigurationMerger.Merge(config, generatedConfig);
            }

            // Merge this file's explicit config (overrides generated and included)
            ConfigurationMerger.Merge(config, fileConfig);
        }
        catch (TomlException ex)
        {
            _logger.LogError(ex, "Failed to parse TOML configuration at {ConfigPath}", filePath);
        }
    }

    private List<string> ParseIncludes(TomlTable tomlTable)
    {
        var result = new List<string>();

        if (!tomlTable.TryGetValue("include", out var includeObj) || includeObj is not TomlTable includeTable)
            return result;

        if (includeTable.TryGetValue("files", out var filesObj) && filesObj is TomlArray filesArray)
        {
            foreach (var file in filesArray)
            {
                if (file is string filePath)
                    result.Add(filePath);
            }
        }

        return result;
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

        // Start with default theme, or a preset if specified
        var baseTheme = ThemeConfig.Default;
        var presetName = GetStringValue(themeTable, "preset");
        if (!string.IsNullOrEmpty(presetName))
        {
            var preset = ThemePresets.GetPreset(presetName);
            if (preset is not null)
            {
                baseTheme = preset;
            }
            else
            {
                _logger.LogWarning(
                    "Unknown theme preset '{PresetName}'. Using default theme. Available presets: {Presets}",
                    presetName,
                    string.Join(", ", ThemePresets.GetPresetNames()));
            }
        }

        // Parse each color property with validation, falling back to base theme values
        var promptColor = ValidateColor(themeTable, "prompt_color", baseTheme.PromptColor);
        var promptSessionColor = ValidateColor(themeTable, "prompt_session_color", baseTheme.PromptSessionColor);
        var promptContextColor = ValidateColor(themeTable, "prompt_context_color", baseTheme.PromptContextColor);
        var promptCosmosAliasColor = ValidateColor(themeTable, "prompt_cosmos_alias_color", baseTheme.PromptCosmosAliasColor);
        var promptBlobAliasColor = ValidateColor(themeTable, "prompt_blob_alias_color", baseTheme.PromptBlobAliasColor);
        var tableHeaderColor = ValidateColor(themeTable, "table_header_color", baseTheme.TableHeaderColor);
        var tableBorderColor = ValidateColor(themeTable, "table_border_color", baseTheme.TableBorderColor);
        var errorColor = ValidateColor(themeTable, "error_color", baseTheme.ErrorColor);
        var warningColor = ValidateColor(themeTable, "warning_color", baseTheme.WarningColor);
        var successColor = ValidateColor(themeTable, "success_color", baseTheme.SuccessColor);
        var dimColor = ValidateColor(themeTable, "dim_color", baseTheme.DimColor);
        var jsonKeyColor = ValidateColor(themeTable, "json_key_color", baseTheme.JsonKeyColor);
        var jsonStringColor = ValidateColor(themeTable, "json_string_color", baseTheme.JsonStringColor);
        var jsonNumberColor = ValidateColor(themeTable, "json_number_color", baseTheme.JsonNumberColor);
        var jsonBooleanColor = ValidateColor(themeTable, "json_boolean_color", baseTheme.JsonBooleanColor);
        var jsonNullColor = ValidateColor(themeTable, "json_null_color", baseTheme.JsonNullColor);
        var bannerColor = ValidateColor(themeTable, "banner_color", baseTheme.BannerColor);

        config.Theme = new ThemeConfig(
            PromptColor: promptColor,
            PromptSessionColor: promptSessionColor,
            PromptContextColor: promptContextColor,
            PromptCosmosAliasColor: promptCosmosAliasColor,
            PromptBlobAliasColor: promptBlobAliasColor,
            TableHeaderColor: tableHeaderColor,
            TableBorderColor: tableBorderColor,
            ErrorColor: errorColor,
            WarningColor: warningColor,
            SuccessColor: successColor,
            DimColor: dimColor,
            JsonKeyColor: jsonKeyColor,
            JsonStringColor: jsonStringColor,
            JsonNumberColor: jsonNumberColor,
            JsonBooleanColor: jsonBooleanColor,
            JsonNullColor: jsonNullColor,
            BannerColor: bannerColor);
    }

    private string ValidateColor(TomlTable table, string key, string defaultValue)
    {
        var value = GetStringValue(table, key);
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        if (ColorFormatValidator.IsValid(value))
            return value;

        _logger.LogWarning(
            "Invalid color value '{ColorValue}' for theme property '{PropertyName}'. Using default: {DefaultValue}",
            value,
            key,
            defaultValue);
        return defaultValue;
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

    private GeneratorsConfig? ParseGenerators(TomlTable tomlTable)
    {
        if (!tomlTable.TryGetValue("generators", out var generatorsObj) || generatorsObj is not TomlTable generatorsTable)
            return null;

        var config = new GeneratorsConfig();

        // Parse dimensions (kingdoms, backends, etc.)
        foreach (var (key, value) in generatorsTable)
        {
            if (value is not TomlTable dimensionTable)
                continue;

            // Check if this is a generator config (cosmos, blob, storage) or a dimension
            if (key == "cosmos")
            {
                config.Cosmos = ParseCosmosGenerator(dimensionTable);
            }
            else if (key == "blob")
            {
                config.Blob = ParseBlobGenerator(dimensionTable);
            }
            else if (key == "storage")
            {
                config.Storage = ParseStorageGenerator(dimensionTable);
            }
            else
            {
                // It's a dimension
                config.Dimensions[key] = ParseDimension(dimensionTable);
            }
        }

        return config;
    }

    private Dictionary<string, GeneratorDimensionEntry> ParseDimension(TomlTable dimensionTable)
    {
        var entries = new Dictionary<string, GeneratorDimensionEntry>();

        foreach (var (entryName, entryValue) in dimensionTable)
        {
            if (entryValue is not TomlTable entryTable)
                continue;

            var entry = new GeneratorDimensionEntry { Name = entryName };

            foreach (var (propKey, propValue) in entryTable)
            {
                if (propValue is string strValue)
                    entry.Properties[propKey] = strValue;
            }

            entries[entryName] = entry;
        }

        return entries;
    }

    private CosmosGeneratorConfig ParseCosmosGenerator(TomlTable table)
    {
        var config = new CosmosGeneratorConfig
        {
            Enabled = GetBoolValue(table, "enabled") ?? false,
            AliasNameTemplate = GetStringValue(table, "alias_name_template") ?? string.Empty,
            EndpointTemplate = GetStringValue(table, "endpoint_template") ?? string.Empty,
            DatabaseTemplate = GetStringValue(table, "database_template") ?? string.Empty,
            KeyEnvTemplate = GetStringValue(table, "key_env_template")
        };

        if (table.TryGetValue("types", out var typesObj) && typesObj is TomlTable typesTable)
        {
            foreach (var (typeName, containerValue) in typesTable)
            {
                if (containerValue is string containerName)
                    config.Types[typeName] = containerName;
            }
        }

        return config;
    }

    private BlobGeneratorConfig ParseBlobGenerator(TomlTable table) =>
        new()
        {
            Enabled = GetBoolValue(table, "enabled") ?? false,
            AliasNameTemplate = GetStringValue(table, "alias_name_template") ?? string.Empty,
            AccountTemplate = GetStringValue(table, "account_template") ?? string.Empty,
            ContainerTemplate = GetStringValue(table, "container_template") ?? string.Empty
        };

    private StorageGeneratorConfig ParseStorageGenerator(TomlTable table) =>
        new()
        {
            Enabled = GetBoolValue(table, "enabled") ?? false,
            AliasNameTemplate = GetStringValue(table, "alias_name_template") ?? string.Empty,
            AccountTemplate = GetStringValue(table, "account_template") ?? string.Empty
        };

    private static string? GetStringValue(TomlTable table, string key) =>
        table.TryGetValue(key, out var value) && value is string str ? str : null;

    private static bool? GetBoolValue(TomlTable table, string key) =>
        table.TryGetValue(key, out var value) && value is bool b ? b : null;
}
