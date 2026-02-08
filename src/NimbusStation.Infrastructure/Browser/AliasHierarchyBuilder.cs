using NimbusStation.Infrastructure.Configuration.Generators;

namespace NimbusStation.Infrastructure.Browser;

/// <summary>
/// Builds hierarchical trees of alias names from generator dimensions and alias dictionaries.
/// Used by the interactive browser to present drill-down navigation.
/// </summary>
public static class AliasHierarchyBuilder
{
    /// <summary>
    /// Builds a hierarchy for Cosmos aliases using generator dimensions.
    /// </summary>
    /// <param name="generators">The generators configuration (may be null).</param>
    /// <param name="allAliasNames">All known Cosmos alias names.</param>
    /// <returns>The root hierarchy node, or null if no generators configured (flat-list fallback).</returns>
    public static AliasHierarchyNode? BuildCosmosHierarchy(
        GeneratorsConfig? generators,
        IReadOnlyCollection<string> allAliasNames)
    {
        if (generators?.Cosmos is not { Enabled: true })
            return null;

        var cosmos = generators.Cosmos;
        var dimensionOrder = ResolveDimensionOrder(
            cosmos.AliasNameTemplate,
            generators.Dimensions,
            SynthesizeTypeDimension(cosmos.Types));

        return BuildHierarchy(dimensionOrder, cosmos.AliasNameTemplate, generators.Dimensions, allAliasNames);
    }

    /// <summary>
    /// Builds a hierarchy for Blob aliases using generator dimensions.
    /// </summary>
    /// <param name="generators">The generators configuration (may be null).</param>
    /// <param name="allAliasNames">All known Blob alias names.</param>
    /// <returns>The root hierarchy node, or null if no generators configured (flat-list fallback).</returns>
    public static AliasHierarchyNode? BuildBlobHierarchy(
        GeneratorsConfig? generators,
        IReadOnlyCollection<string> allAliasNames)
    {
        if (generators?.Blob is not { Enabled: true })
            return null;

        var blob = generators.Blob;
        var dimensionOrder = ResolveDimensionOrder(
            blob.AliasNameTemplate,
            generators.Dimensions,
            syntheticDimensions: null);

        return BuildHierarchy(dimensionOrder, blob.AliasNameTemplate, generators.Dimensions, allAliasNames);
    }

    /// <summary>
    /// Builds a hierarchy for Storage aliases using generator dimensions.
    /// </summary>
    /// <param name="generators">The generators configuration (may be null).</param>
    /// <param name="allAliasNames">All known Storage alias names.</param>
    /// <returns>The root hierarchy node, or null if no generators configured (flat-list fallback).</returns>
    public static AliasHierarchyNode? BuildStorageHierarchy(
        GeneratorsConfig? generators,
        IReadOnlyCollection<string> allAliasNames)
    {
        if (generators?.Storage is not { Enabled: true })
            return null;

        var storage = generators.Storage;
        var dimensionOrder = ResolveDimensionOrder(
            storage.AliasNameTemplate,
            generators.Dimensions,
            syntheticDimensions: null);

        return BuildHierarchy(dimensionOrder, storage.AliasNameTemplate, generators.Dimensions, allAliasNames);
    }

    private static AliasHierarchyNode? BuildHierarchy(
        List<(string Name, Dictionary<string, GeneratorDimensionEntry> Entries)> dimensionOrder,
        string aliasNameTemplate,
        Dictionary<string, Dictionary<string, GeneratorDimensionEntry>> allDimensions,
        IReadOnlyCollection<string> allAliasNames)
    {
        if (dimensionOrder.Count == 0 || allAliasNames.Count == 0)
            return null;

        var allAliasSet = new HashSet<string>(allAliasNames);
        var claimedAliases = new HashSet<string>();

        var root = new AliasHierarchyNode { Label = "root" };
        BuildLevel(root, dimensionOrder, depth: 0, new Dictionary<string, string>(),
            aliasNameTemplate, allAliasSet, claimedAliases);

        // If no generated aliases matched, hierarchy is not useful
        if (claimedAliases.Count == 0)
            return null;

        // Add custom aliases (not claimed by any generator pattern)
        var customAliases = allAliasSet.Except(claimedAliases).OrderBy(a => a).ToList();
        if (customAliases.Count > 0)
        {
            var customNode = new AliasHierarchyNode { Label = "Custom" };
            foreach (string alias in customAliases)
            {
                customNode.Children[alias] = new AliasHierarchyNode
                {
                    Label = alias,
                    AliasName = alias
                };
            }
            root.Children["Custom"] = customNode;
        }

        return root;
    }

    private static void BuildLevel(
        AliasHierarchyNode parent,
        List<(string Name, Dictionary<string, GeneratorDimensionEntry> Entries)> dimensionOrder,
        int depth,
        Dictionary<string, string> context,
        string aliasNameTemplate,
        HashSet<string> allAliasSet,
        HashSet<string> claimedAliases)
    {
        if (depth >= dimensionOrder.Count)
            return;

        var (dimName, entries) = dimensionOrder[depth];
        bool isLastLevel = depth == dimensionOrder.Count - 1;

        foreach (var (entryKey, entry) in entries.OrderBy(e => e.Key))
        {
            var childContext = new Dictionary<string, string>(context)
            {
                [dimName] = entryKey
            };

            // Add entry properties with both prefixed and unprefixed keys
            foreach (var prop in entry.Properties)
            {
                var prefixedKey = $"{dimName}_{prop.Key}";
                childContext[prefixedKey] = prop.Value;
                childContext[prop.Key] = prop.Value;
            }

            if (isLastLevel)
            {
                string aliasName = TemplateSubstitutor.Substitute(aliasNameTemplate, childContext);
                if (!TemplateSubstitutor.HasUnresolvedVariables(aliasName) && allAliasSet.Contains(aliasName))
                {
                    parent.Children[entryKey] = new AliasHierarchyNode
                    {
                        Label = entryKey,
                        DimensionName = dimName,
                        AliasName = aliasName
                    };
                    claimedAliases.Add(aliasName);
                }
            }
            else
            {
                var child = new AliasHierarchyNode
                {
                    Label = entryKey,
                    DimensionName = dimName
                };

                BuildLevel(child, dimensionOrder, depth + 1, childContext,
                    aliasNameTemplate, allAliasSet, claimedAliases);

                if (child.Children.Count > 0)
                    parent.Children[entryKey] = child;
            }
        }
    }

    /// <summary>
    /// Resolves the ordered list of dimensions from a template's variable references.
    /// Only includes variables that map to actual dimensions or synthetic dimensions.
    /// </summary>
    private static List<(string Name, Dictionary<string, GeneratorDimensionEntry> Entries)> ResolveDimensionOrder(
        string aliasNameTemplate,
        Dictionary<string, Dictionary<string, GeneratorDimensionEntry>> dimensions,
        Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>? syntheticDimensions)
    {
        var templateVars = TemplateSubstitutor.GetVariables(aliasNameTemplate);
        var result = new List<(string Name, Dictionary<string, GeneratorDimensionEntry> Entries)>();

        foreach (string varName in templateVars)
        {
            if (dimensions.TryGetValue(varName, out var entries))
            {
                result.Add((varName, entries));
            }
            else if (syntheticDimensions is not null &&
                     syntheticDimensions.TryGetValue(varName, out var syntheticEntries))
            {
                result.Add((varName, syntheticEntries));
            }
        }

        return result;
    }

    /// <summary>
    /// Synthesizes a dimension from Cosmos type mappings so "type" can be a browsable level.
    /// </summary>
    private static Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>? SynthesizeTypeDimension(
        Dictionary<string, string> types)
    {
        if (types.Count == 0)
            return null;

        var typeEntries = new Dictionary<string, GeneratorDimensionEntry>();
        foreach (var (typeName, typeSuffix) in types)
        {
            typeEntries[typeName] = new GeneratorDimensionEntry
            {
                Name = typeName,
                Properties = new Dictionary<string, string> { ["type_suffix"] = typeSuffix }
            };
        }

        return new Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>
        {
            ["type"] = typeEntries
        };
    }
}
