using NimbusStation.Infrastructure.Browser;
using NimbusStation.Infrastructure.Configuration.Generators;

namespace NimbusStation.Tests.Infrastructure.Browser;

public sealed class AliasHierarchyBuilderTests
{
    [Fact]
    public void BuildCosmosHierarchy_NullGenerators_ReturnsNull()
    {
        var result = AliasHierarchyBuilder.BuildCosmosHierarchy(null, ["alias1", "alias2"]);

        Assert.Null(result);
    }

    [Fact]
    public void BuildCosmosHierarchy_DisabledCosmos_ReturnsNull()
    {
        var generators = CreateGeneratorsConfig(cosmosEnabled: false);

        var result = AliasHierarchyBuilder.BuildCosmosHierarchy(generators, ["alias1"]);

        Assert.Null(result);
    }

    [Fact]
    public void BuildCosmosHierarchy_EmptyAliases_ReturnsNull()
    {
        var generators = CreateGeneratorsConfig();

        var result = AliasHierarchyBuilder.BuildCosmosHierarchy(generators, []);

        Assert.Null(result);
    }

    [Fact]
    public void BuildCosmosHierarchy_WithDimensions_BuildsCorrectTree()
    {
        var generators = CreateGeneratorsConfig();
        var aliases = new List<string>
        {
            "ninja-activities-event",
            "ninja-activities-data",
            "ninja-invoices-event",
            "ninja-invoices-data",
            "exchange-activities-event",
            "exchange-activities-data",
            "exchange-invoices-event",
            "exchange-invoices-data"
        };

        AliasHierarchyNode? root = AliasHierarchyBuilder.BuildCosmosHierarchy(generators, aliases);

        Assert.NotNull(root);
        // Root should have 2 kingdoms
        Assert.Equal(2, root.Children.Count);
        Assert.True(root.Children.TryGetValue("ninja", out AliasHierarchyNode? ninja));
        Assert.True(root.Children.ContainsKey("exchange"));

        // Each kingdom should have 2 backends
        Assert.Equal(2, ninja!.Children.Count);
        Assert.True(ninja.Children.TryGetValue("activities", out AliasHierarchyNode? ninjaActivities));
        Assert.True(ninja.Children.ContainsKey("invoices"));

        // Each backend should have 2 types (leaf nodes)
        Assert.Equal(2, ninjaActivities!.Children.Count);
        Assert.True(ninjaActivities.Children.TryGetValue("event", out AliasHierarchyNode? eventNode));
        Assert.True(ninjaActivities.Children.TryGetValue("data", out AliasHierarchyNode? dataNode));

        // Leaf nodes should have correct alias names
        Assert.True(eventNode!.IsLeaf);
        Assert.Equal("ninja-activities-event", eventNode.AliasName);
        Assert.Equal("ninja-activities-data", dataNode!.AliasName);
    }

    [Fact]
    public void BuildCosmosHierarchy_WithCustomAliases_GroupsUnderCustom()
    {
        var generators = CreateGeneratorsConfig();
        var aliases = new List<string>
        {
            "ninja-activities-event",
            "ninja-activities-data",
            "my-custom-alias",
            "another-manual-one"
        };

        AliasHierarchyNode? root = AliasHierarchyBuilder.BuildCosmosHierarchy(generators, aliases);

        Assert.NotNull(root);
        Assert.True(root.Children.ContainsKey("ninja"));
        Assert.True(root.Children.TryGetValue("Custom", out AliasHierarchyNode? custom));

        Assert.Equal(2, custom!.Children.Count);
        Assert.True(custom.Children.ContainsKey("another-manual-one"));
        Assert.True(custom.Children.TryGetValue("my-custom-alias", out AliasHierarchyNode? myCustom));

        // Custom leaves should reference themselves
        Assert.True(myCustom!.IsLeaf);
        Assert.Equal("my-custom-alias", myCustom.AliasName);
    }

    [Fact]
    public void BuildCosmosHierarchy_AllCustomAliases_ReturnsNull()
    {
        var generators = CreateGeneratorsConfig();
        var aliases = new List<string> { "custom-only", "another-custom" };

        AliasHierarchyNode? result = AliasHierarchyBuilder.BuildCosmosHierarchy(generators, aliases);

        // No generated aliases matched, so hierarchy is not useful
        Assert.Null(result);
    }

    [Fact]
    public void BuildCosmosHierarchy_HyphenatedDimensionEntries_ResolvesCorrectly()
    {
        var generators = new GeneratorsConfig
        {
            Dimensions = new Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>
            {
                ["kingdoms"] = new()
                {
                    ["ninja"] = new GeneratorDimensionEntry { Name = "ninja", Properties = [] }
                },
                ["backends"] = new()
                {
                    ["charge-assemblies"] = new GeneratorDimensionEntry { Name = "charge-assemblies", Properties = [] }
                }
            },
            Cosmos = new CosmosGeneratorConfig
            {
                Enabled = true,
                AliasNameTemplate = "{kingdoms}-{backends}-{type}",
                EndpointTemplate = "https://test.documents.azure.com:443/",
                DatabaseTemplate = "db",
                ContainerTemplate = "container",
                Types = new Dictionary<string, string>
                {
                    ["event"] = "events",
                    ["data"] = "data"
                }
            }
        };
        var aliases = new List<string>
        {
            "ninja-charge-assemblies-event",
            "ninja-charge-assemblies-data"
        };

        AliasHierarchyNode? root = AliasHierarchyBuilder.BuildCosmosHierarchy(generators, aliases);

        Assert.NotNull(root);
        Assert.True(root.Children.TryGetValue("ninja", out AliasHierarchyNode? ninja));

        Assert.True(ninja!.Children.TryGetValue("charge-assemblies", out AliasHierarchyNode? chargeAssemblies));
        Assert.Equal(2, chargeAssemblies!.Children.Count);
        Assert.Equal("ninja-charge-assemblies-event", chargeAssemblies.Children["event"].AliasName);
    }

    [Fact]
    public void BuildBlobHierarchy_NullGenerators_ReturnsNull()
    {
        var result = AliasHierarchyBuilder.BuildBlobHierarchy(null, ["alias1"]);

        Assert.Null(result);
    }

    [Fact]
    public void BuildBlobHierarchy_WithDimensions_BuildsCorrectTree()
    {
        var generators = new GeneratorsConfig
        {
            Dimensions = new Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>
            {
                ["kingdoms"] = new()
                {
                    ["ninja"] = new GeneratorDimensionEntry { Name = "ninja", Properties = [] },
                    ["exchange"] = new GeneratorDimensionEntry { Name = "exchange", Properties = [] }
                },
                ["backends"] = new()
                {
                    ["activities"] = new GeneratorDimensionEntry { Name = "activities", Properties = [] }
                }
            },
            Blob = new BlobGeneratorConfig
            {
                Enabled = true,
                AliasNameTemplate = "{kingdoms}-{backends}-blob",
                AccountTemplate = "account",
                ContainerTemplate = "container"
            }
        };
        var aliases = new List<string>
        {
            "ninja-activities-blob",
            "exchange-activities-blob"
        };

        AliasHierarchyNode? root = AliasHierarchyBuilder.BuildBlobHierarchy(generators, aliases);

        Assert.NotNull(root);
        Assert.Equal(2, root.Children.Count);
        Assert.True(root.Children.TryGetValue("ninja", out AliasHierarchyNode? ninja));
        Assert.True(root.Children.ContainsKey("exchange"));

        // Each kingdom has one backend (leaf level for blob since no type dimension)
        Assert.Single(ninja!.Children);
        Assert.True(ninja.Children.TryGetValue("activities", out AliasHierarchyNode? activities));
        Assert.True(activities!.IsLeaf);
        Assert.Equal("ninja-activities-blob", activities.AliasName);
    }

    [Fact]
    public void BuildStorageHierarchy_SingleDimension_SingleLevelTree()
    {
        var generators = new GeneratorsConfig
        {
            Dimensions = new Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>
            {
                ["kingdoms"] = new()
                {
                    ["ninja"] = new GeneratorDimensionEntry { Name = "ninja", Properties = [] },
                    ["exchange"] = new GeneratorDimensionEntry { Name = "exchange", Properties = [] }
                }
            },
            Storage = new StorageGeneratorConfig
            {
                Enabled = true,
                AliasNameTemplate = "{kingdoms}-storage",
                AccountTemplate = "account"
            }
        };
        var aliases = new List<string>
        {
            "ninja-storage",
            "exchange-storage"
        };

        AliasHierarchyNode? root = AliasHierarchyBuilder.BuildStorageHierarchy(generators, aliases);

        Assert.NotNull(root);
        Assert.Equal(2, root.Children.Count);

        // Single-level tree: root children are leaf nodes
        Assert.True(root.Children["ninja"].IsLeaf);
        Assert.Equal("ninja-storage", root.Children["ninja"].AliasName);
        Assert.True(root.Children["exchange"].IsLeaf);
        Assert.Equal("exchange-storage", root.Children["exchange"].AliasName);
    }

    [Fact]
    public void BuildStorageHierarchy_DisabledStorage_ReturnsNull()
    {
        var generators = new GeneratorsConfig
        {
            Storage = new StorageGeneratorConfig { Enabled = false }
        };

        var result = AliasHierarchyBuilder.BuildStorageHierarchy(generators, ["alias1"]);

        Assert.Null(result);
    }

    [Fact]
    public void BuildCosmosHierarchy_NoMatchingDimensionInTemplate_ReturnsNull()
    {
        var generators = new GeneratorsConfig
        {
            Dimensions = new Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>
            {
                ["kingdoms"] = new()
                {
                    ["ninja"] = new GeneratorDimensionEntry { Name = "ninja", Properties = [] }
                }
            },
            Cosmos = new CosmosGeneratorConfig
            {
                Enabled = true,
                AliasNameTemplate = "{nonexistent}-{type}",
                EndpointTemplate = "endpoint",
                DatabaseTemplate = "db",
                ContainerTemplate = "container",
                Types = new Dictionary<string, string> { ["event"] = "events" }
            }
        };

        // Template references "nonexistent" dimension which doesn't exist,
        // but "type" is synthetic. With only "type", it's still a valid single-level tree.
        AliasHierarchyNode? result = AliasHierarchyBuilder.BuildCosmosHierarchy(generators, ["test-event"]);

        // The alias won't match because {nonexistent} won't be resolved
        Assert.Null(result);
    }

    [Fact]
    public void BuildCosmosHierarchy_LeavesHaveCorrectDimensionName()
    {
        var generators = CreateGeneratorsConfig();
        var aliases = new List<string> { "ninja-activities-event" };

        AliasHierarchyNode? root = AliasHierarchyBuilder.BuildCosmosHierarchy(generators, aliases);

        Assert.NotNull(root);
        AliasHierarchyNode ninja = root.Children["ninja"];
        Assert.Equal("kingdoms", ninja.DimensionName);

        AliasHierarchyNode activities = ninja.Children["activities"];
        Assert.Equal("backends", activities.DimensionName);

        AliasHierarchyNode eventNode = activities.Children["event"];
        Assert.Equal("type", eventNode.DimensionName);
    }

    /// <summary>
    /// Creates a standard generators config with kingdoms (ninja, exchange), backends (activities, invoices),
    /// and Cosmos types (event, data). Alias name template: "{kingdoms}-{backends}-{type}".
    /// </summary>
    private static GeneratorsConfig CreateGeneratorsConfig(bool cosmosEnabled = true) =>
        new()
        {
            Dimensions = new Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>
            {
                ["kingdoms"] = new()
                {
                    ["ninja"] = new GeneratorDimensionEntry { Name = "ninja", Properties = [] },
                    ["exchange"] = new GeneratorDimensionEntry { Name = "exchange", Properties = [] }
                },
                ["backends"] = new()
                {
                    ["activities"] = new GeneratorDimensionEntry { Name = "activities", Properties = [] },
                    ["invoices"] = new GeneratorDimensionEntry { Name = "invoices", Properties = [] }
                }
            },
            Cosmos = new CosmosGeneratorConfig
            {
                Enabled = cosmosEnabled,
                AliasNameTemplate = "{kingdoms}-{backends}-{type}",
                EndpointTemplate = "https://test.documents.azure.com:443/",
                DatabaseTemplate = "testdb",
                ContainerTemplate = "container",
                Types = new Dictionary<string, string>
                {
                    ["event"] = "events",
                    ["data"] = "data"
                }
            }
        };
}
