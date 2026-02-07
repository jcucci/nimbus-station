using Microsoft.Extensions.Logging;

namespace NimbusStation.Infrastructure.Configuration.Generators;

/// <summary>
/// Generates aliases from dimension combinations and templates.
/// </summary>
public sealed class AliasGenerator
{
    private readonly ILogger? _logger;

    public AliasGenerator(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates all aliases based on the generator configuration.
    /// </summary>
    /// <param name="config">The generators configuration.</param>
    /// <returns>A configuration containing all generated aliases.</returns>
    public NimbusConfiguration GenerateAliases(GeneratorsConfig config)
    {
        var result = new NimbusConfiguration();

        var combinations = GenerateCombinations(config.Dimensions);

        if (config.Cosmos?.Enabled == true)
            GenerateCosmosAliases(config.Cosmos, combinations, result);

        if (config.Blob?.Enabled == true)
            GenerateBlobAliases(config.Blob, combinations, result);

        if (config.Storage?.Enabled == true)
            GenerateStorageAliases(config.Storage, combinations, result);

        return result;
    }

    private void GenerateCosmosAliases(
        CosmosGeneratorConfig cosmos,
        List<Dictionary<string, string>> combinations,
        NimbusConfiguration result)
    {
        foreach (var combo in combinations)
        {
            foreach (var typeEntry in cosmos.Types)
            {
                var variables = new Dictionary<string, string>(combo)
                {
                    ["type"] = typeEntry.Key,
                    ["type_suffix"] = typeEntry.Value
                };

                var aliasName = TemplateSubstitutor.Substitute(cosmos.AliasNameTemplate, variables);
                var endpoint = TemplateSubstitutor.Substitute(cosmos.EndpointTemplate, variables);
                var database = TemplateSubstitutor.Substitute(cosmos.DatabaseTemplate, variables);
                var container = !string.IsNullOrEmpty(cosmos.ContainerTemplate)
                    ? TemplateSubstitutor.Substitute(cosmos.ContainerTemplate, variables)
                    : typeEntry.Value;
                var keyEnv = cosmos.KeyEnvTemplate is not null
                    ? TemplateSubstitutor.Substitute(cosmos.KeyEnvTemplate, variables)
                    : null;

                if (TemplateSubstitutor.HasUnresolvedVariables(aliasName) ||
                    TemplateSubstitutor.HasUnresolvedVariables(endpoint) ||
                    TemplateSubstitutor.HasUnresolvedVariables(database) ||
                    TemplateSubstitutor.HasUnresolvedVariables(container) ||
                    (keyEnv is not null && TemplateSubstitutor.HasUnresolvedVariables(keyEnv)))
                {
                    _logger?.LogWarning(
                        "Skipping Cosmos alias '{AliasName}': unresolved variables in template",
                        aliasName);
                    continue;
                }

                result.CosmosAliases[aliasName] = new CosmosAliasConfig(endpoint, database, container, keyEnv);
            }
        }
    }

    private void GenerateBlobAliases(
        BlobGeneratorConfig blob,
        List<Dictionary<string, string>> combinations,
        NimbusConfiguration result)
    {
        foreach (var combo in combinations)
        {
            var aliasName = TemplateSubstitutor.Substitute(blob.AliasNameTemplate, combo);
            var account = TemplateSubstitutor.Substitute(blob.AccountTemplate, combo);
            var container = TemplateSubstitutor.Substitute(blob.ContainerTemplate, combo);

            if (TemplateSubstitutor.HasUnresolvedVariables(aliasName) ||
                TemplateSubstitutor.HasUnresolvedVariables(account) ||
                TemplateSubstitutor.HasUnresolvedVariables(container))
            {
                _logger?.LogWarning(
                    "Skipping Blob alias '{AliasName}': unresolved variables in template",
                    aliasName);
                continue;
            }

            result.BlobAliases[aliasName] = new BlobAliasConfig(account, container);
        }
    }

    private void GenerateStorageAliases(
        StorageGeneratorConfig storage,
        List<Dictionary<string, string>> combinations,
        NimbusConfiguration result)
    {
        // Storage aliases typically only need kingdom-level (not backend combinations)
        // Generate unique aliases by using only the first dimension's entries
        var generatedNames = new HashSet<string>();

        foreach (var combo in combinations)
        {
            var aliasName = TemplateSubstitutor.Substitute(storage.AliasNameTemplate, combo);
            var account = TemplateSubstitutor.Substitute(storage.AccountTemplate, combo);

            if (TemplateSubstitutor.HasUnresolvedVariables(aliasName) ||
                TemplateSubstitutor.HasUnresolvedVariables(account))
            {
                _logger?.LogWarning(
                    "Skipping Storage alias '{AliasName}': unresolved variables in template",
                    aliasName);
                continue;
            }

            // Avoid duplicate storage aliases (same name)
            if (!generatedNames.Add(aliasName))
                continue;

            result.StorageAliases[aliasName] = new StorageAliasConfig(account);
        }
    }

    /// <summary>
    /// Generates the Cartesian product of all dimension entries.
    /// Each combination is a dictionary of variable names to their values.
    /// </summary>
    /// <remarks>
    /// Limited to 10,000 combinations to prevent resource exhaustion from misconfiguration.
    /// </remarks>
    private List<Dictionary<string, string>> GenerateCombinations(
        Dictionary<string, Dictionary<string, GeneratorDimensionEntry>> dimensions)
    {
        const int MaxCombinations = 10_000;
        var result = new List<Dictionary<string, string>> { new() };

        foreach (var dimension in dimensions)
        {
            var dimensionName = dimension.Key;
            var newResult = new List<Dictionary<string, string>>();

            foreach (var existing in result)
            {
                foreach (var entry in dimension.Value)
                {
                    if (newResult.Count >= MaxCombinations)
                    {
                        _logger?.LogWarning(
                            "Generator combination limit ({MaxCombinations}) exceeded, truncating alias generation",
                            MaxCombinations);
                        return newResult;
                    }

                    var combined = new Dictionary<string, string>(existing)
                    {
                        [dimensionName] = entry.Key
                    };

                    // Add properties with both unprefixed and prefixed keys.
                    // Prefixed keys (e.g., "kingdoms_abbrev") are always unambiguous.
                    // Unprefixed keys (e.g., "abbrev") are convenient but may collide
                    // if multiple dimensions have the same property name - later dimensions
                    // will override earlier ones for unprefixed keys.
                    foreach (var prop in entry.Value.Properties)
                    {
                        var prefixedKey = $"{dimensionName}_{prop.Key}";
                        combined[prefixedKey] = prop.Value;
                        combined[prop.Key] = prop.Value;
                    }

                    newResult.Add(combined);
                }
            }

            result = newResult;
        }

        return result;
    }
}
