using Microsoft.Extensions.Logging;
using Tomlyn;
using Tomlyn.Model;

namespace NimbusStation.Infrastructure.Aliases;

/// <summary>
/// Service for managing command aliases stored in aliases.toml.
/// </summary>
public sealed class AliasService : IAliasService
{
    private const string DefaultAliasesTemplate =
        """
        # Nimbus Station - Command Aliases
        # Documentation: https://github.com/jcucci/nimbus-station
        #
        # Command aliases let you create shortcuts for frequently used commands.
        # Note: Resource aliases (prefixed with @) are defined in config.toml
        #       and resolve to cloud resource connection details.
        #
        # Simple aliases (prefix expansion):
        #   cq = "azure cosmos query"
        #   bd = "azure blob download"
        #
        # Parameterized aliases (use {0}, {1}, etc.):
        #   user = "azure cosmos query @prod-users \"SELECT * FROM c WHERE c.userId = '{0}'\""
        #
        # Built-in variables:
        #   {ticket}      - Current session ticket ID (requires active session)
        #   {session-dir} - Session directory path (requires active session)
        #   {today}       - Today's date (YYYY-MM-DD)
        #   {now}         - Current UTC timestamp (ISO 8601)
        #   {user}        - OS username

        [aliases]
        # Add your aliases below:

        """;

    private readonly ILogger<AliasService> _logger;
    private readonly string _aliasesPath;
    private Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);
    private bool _isLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="AliasService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public AliasService(ILogger<AliasService> logger)
        : this(logger, GetDefaultAliasesPath())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AliasService"/> class with a custom path.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="aliasesPath">The path to the aliases.toml file.</param>
    public AliasService(ILogger<AliasService> logger, string aliasesPath)
    {
        _logger = logger;
        _aliasesPath = aliasesPath;
    }

    /// <inheritdoc/>
    public async Task<AliasesConfiguration> LoadAliasesAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoaded)
        {
            return new AliasesConfiguration(_aliases);
        }

        return await ReloadAliasesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AliasesConfiguration> ReloadAliasesAsync(CancellationToken cancellationToken = default)
    {
        _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(_aliasesPath))
        {
            await CreateDefaultAliasesFileAsync(cancellationToken);
            _isLoaded = true;
            return AliasesConfiguration.Empty;
        }

        try
        {
            var tomlContent = await File.ReadAllTextAsync(_aliasesPath, cancellationToken);
            var tomlTable = Toml.ToModel(tomlContent);

            ParseAliases(tomlTable);
        }
        catch (TomlException ex)
        {
            _logger.LogError(ex, "Failed to parse TOML aliases at {AliasesPath}", _aliasesPath);
        }

        _isLoaded = true;
        return new AliasesConfiguration(_aliases);
    }

    /// <inheritdoc/>
    public async Task<string?> GetAliasAsync(string name, CancellationToken cancellationToken = default)
    {
        await LoadAliasesAsync(cancellationToken);
        return _aliases.GetValueOrDefault(name);
    }

    /// <inheritdoc/>
    public async Task AddAliasAsync(string name, string expansion, CancellationToken cancellationToken = default)
    {
        var validation = AliasNameValidator.Validate(name);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.ErrorMessage, nameof(name));
        }

        await LoadAliasesAsync(cancellationToken);
        _aliases[name] = expansion;
        await PersistAliasesAsync(cancellationToken);

        _logger.LogInformation("Added alias '{Name}' = \"{Expansion}\"", name, expansion);
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveAliasAsync(string name, CancellationToken cancellationToken = default)
    {
        await LoadAliasesAsync(cancellationToken);

        if (!_aliases.Remove(name))
        {
            return false;
        }

        await PersistAliasesAsync(cancellationToken);
        _logger.LogInformation("Removed alias '{Name}'", name);
        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> GetAllAliases()
    {
        return _aliases;
    }

    private static string GetDefaultAliasesPath()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrEmpty(configHome))
        {
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }

        return Path.Combine(configHome, "nimbus", "aliases.toml");
    }

    private async Task CreateDefaultAliasesFileAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_aliasesPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(_aliasesPath, DefaultAliasesTemplate, cancellationToken);
        _logger.LogInformation("Created default aliases file at {AliasesPath}", _aliasesPath);
    }

    private void ParseAliases(TomlTable tomlTable)
    {
        if (!tomlTable.TryGetValue("aliases", out var aliasesObj) || aliasesObj is not TomlTable aliasesTable)
        {
            _logger.LogDebug("No [aliases] section found in aliases.toml");
            return;
        }

        foreach (var (aliasName, aliasValue) in aliasesTable)
        {
            if (aliasValue is not string expansion)
            {
                _logger.LogWarning("Skipping alias '{AliasName}': expected a string value", aliasName);
                continue;
            }

            var validation = AliasNameValidator.Validate(aliasName);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Skipping invalid alias '{AliasName}': {Error}", aliasName, validation.ErrorMessage);
                continue;
            }

            _aliases[aliasName] = expansion;
        }

        _logger.LogDebug("Loaded {Count} aliases from aliases.toml", _aliases.Count);
    }

    private async Task PersistAliasesAsync(CancellationToken cancellationToken)
    {
        var tomlContent = GenerateTomlContent();
        await File.WriteAllTextAsync(_aliasesPath, tomlContent, cancellationToken);
        _logger.LogDebug("Persisted {Count} aliases to aliases.toml", _aliases.Count);
    }

    private string GenerateTomlContent()
    {
        var builder = new System.Text.StringBuilder();

        builder.AppendLine("# Nimbus Station - Command Aliases");
        builder.AppendLine("# Documentation: https://github.com/jcucci/nimbus-station");
        builder.AppendLine();

        builder.AppendLine("[aliases]");

        foreach (var (name, expansion) in _aliases.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            // Escape the expansion string for TOML
            var escapedExpansion = EscapeTomlString(expansion);
            builder.AppendLine($"{name} = \"{escapedExpansion}\"");
        }

        return builder.ToString();
    }

    private static string EscapeTomlString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
