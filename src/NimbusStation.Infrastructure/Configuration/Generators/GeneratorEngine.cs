using Microsoft.Extensions.Logging;

namespace NimbusStation.Infrastructure.Configuration.Generators;

/// <summary>
/// Generates aliases from dimension combinations and templates.
/// </summary>
public sealed class GeneratorEngine
{
    private readonly ILogger<GeneratorEngine> _logger;

    public GeneratorEngine(ILogger<GeneratorEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates Cosmos aliases from configuration.
    /// </summary>
    public Dictionary<string, CosmosAliasConfig> GenerateCosmosAliases(
        CosmosGeneratorConfig? config,
        IReadOnlyDictionary<string, IReadOnlyList<GeneratorDimension>> dimensions)
    {
        var result = new Dictionary<string, CosmosAliasConfig>(StringComparer.OrdinalIgnoreCase);

        if (config is null || !config.Enabled)
            return result;

        var requiredDimensions = ExtractRequiredDimensions(config.AliasNameTemplate, dimensions);
        var crossProduct = ComputeCrossProduct(requiredDimensions, dimensions);
        var skipped = 0;

        foreach (var context in crossProduct)
        {
            if (config.Types.Count > 0)
            {
                // Generate one alias per type
                foreach (var (typeName, typeSuffix) in config.Types)
                {
                    var typeContext = new Dictionary<string, string>(context, StringComparer.OrdinalIgnoreCase)
                    {
                        ["type"] = typeName,
                        ["type_suffix"] = typeSuffix
                    };

                    if (!TryGenerateCosmosAlias(config, typeContext, result))
                        skipped++;
                }
            }
            else
            {
                if (!TryGenerateCosmosAlias(config, context, result))
                    skipped++;
            }
        }

        if (skipped > 0)
            _logger.LogWarning("Skipped {Count} cosmos alias(es) due to missing template variables", skipped);

        _logger.LogDebug("Generated {Count} Cosmos aliases", result.Count);
        return result;
    }

    /// <summary>
    /// Generates Blob aliases from configuration.
    /// </summary>
    public Dictionary<string, BlobAliasConfig> GenerateBlobAliases(
        BlobGeneratorConfig? config,
        IReadOnlyDictionary<string, IReadOnlyList<GeneratorDimension>> dimensions)
    {
        var result = new Dictionary<string, BlobAliasConfig>(StringComparer.OrdinalIgnoreCase);

        if (config is null || !config.Enabled)
            return result;

        var requiredDimensions = ExtractRequiredDimensions(config.AliasNameTemplate, dimensions);
        var crossProduct = ComputeCrossProduct(requiredDimensions, dimensions);
        var skipped = 0;

        foreach (var context in crossProduct)
        {
            var aliasName = TemplateSubstitutor.TrySubstitute(config.AliasNameTemplate, context);
            var account = TemplateSubstitutor.TrySubstitute(config.AccountTemplate, context);
            var container = TemplateSubstitutor.TrySubstitute(config.ContainerTemplate, context);

            if (aliasName is null || account is null || container is null)
            {
                _logger.LogDebug(
                    "Skipping blob alias generation: missing variable in context for templates");
                skipped++;
                continue;
            }

            result[aliasName] = new BlobAliasConfig(account, container);
        }

        if (skipped > 0)
            _logger.LogWarning("Skipped {Count} blob alias(es) due to missing template variables", skipped);

        _logger.LogDebug("Generated {Count} Blob aliases", result.Count);
        return result;
    }

    /// <summary>
    /// Generates Storage aliases from configuration.
    /// </summary>
    public Dictionary<string, StorageAliasConfig> GenerateStorageAliases(
        StorageGeneratorConfig? config,
        IReadOnlyDictionary<string, IReadOnlyList<GeneratorDimension>> dimensions)
    {
        var result = new Dictionary<string, StorageAliasConfig>(StringComparer.OrdinalIgnoreCase);

        if (config is null || !config.Enabled)
            return result;

        var requiredDimensions = ExtractRequiredDimensions(config.AliasNameTemplate, dimensions);
        var crossProduct = ComputeCrossProduct(requiredDimensions, dimensions);
        var skipped = 0;

        foreach (var context in crossProduct)
        {
            var aliasName = TemplateSubstitutor.TrySubstitute(config.AliasNameTemplate, context);
            var account = TemplateSubstitutor.TrySubstitute(config.AccountTemplate, context);

            if (aliasName is null || account is null)
            {
                _logger.LogDebug(
                    "Skipping storage alias generation: missing variable in context for templates");
                skipped++;
                continue;
            }

            result[aliasName] = new StorageAliasConfig(account);
        }

        if (skipped > 0)
            _logger.LogWarning("Skipped {Count} storage alias(es) due to missing template variables", skipped);

        _logger.LogDebug("Generated {Count} Storage aliases", result.Count);
        return result;
    }

    private bool TryGenerateCosmosAlias(
        CosmosGeneratorConfig config,
        IReadOnlyDictionary<string, string> context,
        Dictionary<string, CosmosAliasConfig> result)
    {
        var aliasName = TemplateSubstitutor.TrySubstitute(config.AliasNameTemplate, context);
        var endpoint = TemplateSubstitutor.TrySubstitute(config.EndpointTemplate, context);
        var database = TemplateSubstitutor.TrySubstitute(config.DatabaseTemplate, context);
        var container = config.ContainerTemplate is not null
            ? TemplateSubstitutor.TrySubstitute(config.ContainerTemplate, context)
            : null;

        if (aliasName is null || endpoint is null || database is null)
        {
            _logger.LogDebug(
                "Skipping cosmos alias generation: missing variable in context for templates");
            return false;
        }

        // Container is required for a valid CosmosAliasConfig
        if (string.IsNullOrEmpty(container))
        {
            _logger.LogDebug(
                "Skipping cosmos alias '{AliasName}': container is required but not provided or expansion failed",
                aliasName);
            return false;
        }

        result[aliasName] = new CosmosAliasConfig(endpoint, database, container);
        return true;
    }

    private static IReadOnlyList<string> ExtractRequiredDimensions(
        string aliasNameTemplate,
        IReadOnlyDictionary<string, IReadOnlyList<GeneratorDimension>> dimensions)
    {
        var variables = TemplateSubstitutor.ExtractVariables(aliasNameTemplate);
        return variables
            .Where(v => dimensions.ContainsKey(v))
            .ToList();
    }

    private IEnumerable<Dictionary<string, string>> ComputeCrossProduct(
        IReadOnlyList<string> dimensionNames,
        IReadOnlyDictionary<string, IReadOnlyList<GeneratorDimension>> dimensions)
    {
        if (dimensionNames.Count == 0)
        {
            yield return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            yield break;
        }

        // Check all required dimensions exist
        foreach (var dimName in dimensionNames)
        {
            if (!dimensions.ContainsKey(dimName))
            {
                _logger.LogWarning("Dimension '{DimensionName}' not found, skipping generation", dimName);
                yield break;
            }
        }

        // Compute cross-product recursively
        foreach (var combo in ComputeCrossProductRecursive(dimensionNames, dimensions, 0))
        {
            yield return combo;
        }
    }

    private static IEnumerable<Dictionary<string, string>> ComputeCrossProductRecursive(
        IReadOnlyList<string> dimensionNames,
        IReadOnlyDictionary<string, IReadOnlyList<GeneratorDimension>> dimensions,
        int index)
    {
        if (index >= dimensionNames.Count)
        {
            yield return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            yield break;
        }

        var dimensionName = dimensionNames[index];
        var entries = dimensions[dimensionName];

        foreach (var entry in entries)
        {
            foreach (var restCombo in ComputeCrossProductRecursive(dimensionNames, dimensions, index + 1))
            {
                // Add this dimension's key and properties to the context
                restCombo[dimensionName] = entry.Key;
                foreach (var (propName, propValue) in entry.Properties)
                {
                    restCombo[propName] = propValue;
                }

                yield return restCombo;
            }
        }
    }
}
