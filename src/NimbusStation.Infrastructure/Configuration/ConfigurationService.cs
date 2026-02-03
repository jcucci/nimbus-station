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
    private readonly GeneratorEngine _generatorEngine;
    private readonly string _configPath;
    private readonly string _configDirectory;
    private NimbusConfiguration? _cachedConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="generatorEngine">The generator engine for alias generation.</param>
    public ConfigurationService(ILogger<ConfigurationService> logger, GeneratorEngine generatorEngine)
        : this(logger, generatorEngine, GetDefaultConfigPath())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class with a custom config path.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="generatorEngine">The generator engine for alias generation.</param>
    /// <param name="configPath">The path to the configuration file.</param>
    public ConfigurationService(ILogger<ConfigurationService> logger, GeneratorEngine generatorEngine, string configPath)
    {
        _logger = logger;
        _generatorEngine = generatorEngine;
        _configPath = configPath;
        _configDirectory = Path.GetDirectoryName(configPath) ?? string.Empty;
    }

    /// <inheritdoc/>
    public async Task<NimbusConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedConfiguration is not null)
            return _cachedConfiguration;

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

            // Step 1: Load and merge include files
            var mergedTable = await LoadAndMergeIncludesAsync(tomlTable, cancellationToken);

            // Step 2: Parse theme from merged config
            ParseTheme(mergedTable, config);

            // Step 3: Parse generators and generate aliases
            var dimensions = ParseDimensions(mergedTable);
            var cosmosGenerator = ParseCosmosGenerator(mergedTable);
            var blobGenerator = ParseBlobGenerator(mergedTable);
            var storageGenerator = ParseStorageGenerator(mergedTable);

            // Step 4: Generate aliases (these form the base)
            var generatedCosmosAliases = _generatorEngine.GenerateCosmosAliases(cosmosGenerator, dimensions);
            var generatedBlobAliases = _generatorEngine.GenerateBlobAliases(blobGenerator, dimensions);
            var generatedStorageAliases = _generatorEngine.GenerateStorageAliases(storageGenerator, dimensions);

            // Add generated aliases to config
            foreach (var (name, alias) in generatedCosmosAliases)
                config.CosmosAliases[name] = alias;
            foreach (var (name, alias) in generatedBlobAliases)
                config.BlobAliases[name] = alias;
            foreach (var (name, alias) in generatedStorageAliases)
                config.StorageAliases[name] = alias;

            // Step 5: Parse explicit aliases (these override generated)
            ParseCosmosAliases(mergedTable, config);
            ParseBlobAliases(mergedTable, config);
            ParseStorageAliases(mergedTable, config);

            _logger.LogInformation(
                "Loaded configuration with {CosmosCount} cosmos, {BlobCount} blob, {StorageCount} storage aliases",
                config.CosmosAliases.Count,
                config.BlobAliases.Count,
                config.StorageAliases.Count);
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

    private static string? GetStringValue(TomlTable table, string key) =>
        table.TryGetValue(key, out var value) && value is string str ? str : null;

    private static bool GetBoolValue(TomlTable table, string key, bool defaultValue = false) =>
        table.TryGetValue(key, out var value) && value is bool b ? b : defaultValue;

    private async Task<TomlTable> LoadAndMergeIncludesAsync(TomlTable mainTable, CancellationToken cancellationToken)
    {
        if (!mainTable.TryGetValue("include", out var includeObj) || includeObj is not TomlTable includeTable)
            return mainTable;

        if (!includeTable.TryGetValue("files", out var filesObj) || filesObj is not TomlArray filesArray)
            return mainTable;

        foreach (var fileObj in filesArray)
        {
            if (fileObj is not string relativePath)
                continue;

            // Security: Reject rooted paths and path traversal attempts
            if (Path.IsPathRooted(relativePath) || relativePath.Contains(".."))
            {
                _logger.LogWarning(
                    "Include path rejected (rooted or traversal attempt): {IncludePath}",
                    relativePath);
                continue;
            }

            var fullPath = Path.Combine(_configDirectory, relativePath);

            // Ensure resolved path stays within config directory
            var normalizedPath = Path.GetFullPath(fullPath);
            var normalizedConfigDir = Path.GetFullPath(_configDirectory);
            if (!normalizedPath.StartsWith(normalizedConfigDir, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Include path rejected (escapes config directory): {IncludePath}",
                    relativePath);
                continue;
            }

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Include file not found: {IncludePath}", fullPath);
                continue;
            }

            try
            {
                var includeContent = await File.ReadAllTextAsync(fullPath, cancellationToken);
                var includeTable2 = Toml.ToModel(includeContent);
                // NOTE: Include files can overwrite existing keys in mainTable.
                // This allows include files to provide environment-specific overrides.
                MergeTomlTables(mainTable, includeTable2);
                _logger.LogDebug("Merged include file: {IncludePath}", fullPath);
            }
            catch (TomlException ex)
            {
                _logger.LogWarning(ex, "Failed to parse include file: {IncludePath}", fullPath);
            }
        }

        return mainTable;
    }

    private static void MergeTomlTables(TomlTable target, TomlTable source)
    {
        foreach (var (key, value) in source)
        {
            if (value is TomlTable sourceTable && target.TryGetValue(key, out var existingValue) && existingValue is TomlTable targetTable)
            {
                // Recursively merge nested tables
                MergeTomlTables(targetTable, sourceTable);
            }
            else
            {
                // Overwrite or add the value
                target[key] = value;
            }
        }
    }

    private Dictionary<string, IReadOnlyList<GeneratorDimension>> ParseDimensions(TomlTable tomlTable)
    {
        var dimensions = new Dictionary<string, IReadOnlyList<GeneratorDimension>>(StringComparer.OrdinalIgnoreCase);

        if (!tomlTable.TryGetValue("generators", out var generatorsObj) || generatorsObj is not TomlTable generatorsTable)
            return dimensions;

        // Parse each dimension (kingdoms, backends, etc.)
        foreach (var (dimName, dimValue) in generatorsTable)
        {
            // Skip generator config sections (cosmos, blob, storage)
            if (dimName is "cosmos" or "blob" or "storage")
                continue;

            if (dimValue is not TomlTable dimTable)
                continue;

            var entries = new List<GeneratorDimension>();
            foreach (var (entryKey, entryValue) in dimTable)
            {
                if (entryValue is not TomlTable entryTable)
                    continue;

                var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (propName, propValue) in entryTable)
                {
                    if (propValue is string strValue)
                        properties[propName] = strValue;
                }

                entries.Add(new GeneratorDimension
                {
                    Key = entryKey,
                    Properties = properties
                });
            }

            dimensions[dimName] = entries;
            _logger.LogDebug("Parsed dimension '{DimensionName}' with {EntryCount} entries", dimName, entries.Count);
        }

        return dimensions;
    }

    private CosmosGeneratorConfig? ParseCosmosGenerator(TomlTable tomlTable)
    {
        if (!tomlTable.TryGetValue("generators", out var generatorsObj) || generatorsObj is not TomlTable generatorsTable)
            return null;

        if (!generatorsTable.TryGetValue("cosmos", out var cosmosObj) || cosmosObj is not TomlTable cosmosTable)
            return null;

        var aliasNameTemplate = GetStringValue(cosmosTable, "alias_name_template");
        var endpointTemplate = GetStringValue(cosmosTable, "endpoint_template");
        var databaseTemplate = GetStringValue(cosmosTable, "database_template");

        if (aliasNameTemplate is null || endpointTemplate is null || databaseTemplate is null)
        {
            _logger.LogWarning("Cosmos generator missing required templates");
            return null;
        }

        var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (cosmosTable.TryGetValue("types", out var typesObj) && typesObj is TomlTable typesTable)
        {
            foreach (var (typeName, typeValue) in typesTable)
            {
                if (typeValue is string suffix)
                    types[typeName] = suffix;
            }
        }

        return new CosmosGeneratorConfig
        {
            Enabled = GetBoolValue(cosmosTable, "enabled"),
            AliasNameTemplate = aliasNameTemplate,
            EndpointTemplate = endpointTemplate,
            DatabaseTemplate = databaseTemplate,
            ContainerTemplate = GetStringValue(cosmosTable, "container_template"),
            Types = types
        };
    }

    private BlobGeneratorConfig? ParseBlobGenerator(TomlTable tomlTable)
    {
        if (!tomlTable.TryGetValue("generators", out var generatorsObj) || generatorsObj is not TomlTable generatorsTable)
            return null;

        if (!generatorsTable.TryGetValue("blob", out var blobObj) || blobObj is not TomlTable blobTable)
            return null;

        var aliasNameTemplate = GetStringValue(blobTable, "alias_name_template");
        var accountTemplate = GetStringValue(blobTable, "account_template");
        var containerTemplate = GetStringValue(blobTable, "container_template");

        if (aliasNameTemplate is null || accountTemplate is null || containerTemplate is null)
        {
            _logger.LogWarning("Blob generator missing required templates");
            return null;
        }

        return new BlobGeneratorConfig
        {
            Enabled = GetBoolValue(blobTable, "enabled"),
            AliasNameTemplate = aliasNameTemplate,
            AccountTemplate = accountTemplate,
            ContainerTemplate = containerTemplate
        };
    }

    private StorageGeneratorConfig? ParseStorageGenerator(TomlTable tomlTable)
    {
        if (!tomlTable.TryGetValue("generators", out var generatorsObj) || generatorsObj is not TomlTable generatorsTable)
            return null;

        if (!generatorsTable.TryGetValue("storage", out var storageObj) || storageObj is not TomlTable storageTable)
            return null;

        var aliasNameTemplate = GetStringValue(storageTable, "alias_name_template");
        var accountTemplate = GetStringValue(storageTable, "account_template");

        if (aliasNameTemplate is null || accountTemplate is null)
        {
            _logger.LogWarning("Storage generator missing required templates");
            return null;
        }

        return new StorageGeneratorConfig
        {
            Enabled = GetBoolValue(storageTable, "enabled"),
            AliasNameTemplate = aliasNameTemplate,
            AccountTemplate = accountTemplate
        };
    }
}
