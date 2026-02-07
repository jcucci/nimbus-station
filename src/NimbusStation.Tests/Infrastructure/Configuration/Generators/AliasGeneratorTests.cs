using NimbusStation.Infrastructure.Configuration.Generators;

namespace NimbusStation.Tests.Infrastructure.Configuration.Generators;

public sealed class AliasGeneratorTests
{
    private readonly AliasGenerator _generator = new();

    [Fact]
    public void GenerateAliases_CosmosGenerator_GeneratesCartesianProduct()
    {
        var config = new GeneratorsConfig
        {
            Dimensions = new Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>
            {
                ["kingdoms"] = new()
                {
                    ["ninja"] = new GeneratorDimensionEntry
                    {
                        Name = "ninja",
                        Properties = new Dictionary<string, string> { ["abbrev"] = "nja" }
                    },
                    ["exchange"] = new GeneratorDimensionEntry
                    {
                        Name = "exchange",
                        Properties = new Dictionary<string, string> { ["abbrev"] = "exc" }
                    }
                },
                ["backends"] = new()
                {
                    ["activities"] = new GeneratorDimensionEntry
                    {
                        Name = "activities",
                        Properties = new Dictionary<string, string> { ["database"] = "activities-db" }
                    }
                }
            },
            Cosmos = new CosmosGeneratorConfig
            {
                Enabled = true,
                AliasNameTemplate = "{kingdoms}-{backends}-{type}",
                EndpointTemplate = "https://king-{abbrev}-be.documents.azure.com:443/",
                DatabaseTemplate = "{database}",
                Types = new Dictionary<string, string>
                {
                    ["event"] = "event-store",
                    ["view"] = "view-store"
                }
            }
        };

        var result = _generator.GenerateAliases(config);

        // 2 kingdoms × 1 backend × 2 types = 4 aliases
        Assert.Equal(4, result.CosmosAliases.Count);
        Assert.True(result.CosmosAliases.TryGetValue("ninja-activities-event", out var alias));
        Assert.True(result.CosmosAliases.ContainsKey("ninja-activities-view"));
        Assert.True(result.CosmosAliases.ContainsKey("exchange-activities-event"));
        Assert.True(result.CosmosAliases.ContainsKey("exchange-activities-view"));
        Assert.Equal("https://king-nja-be.documents.azure.com:443/", alias.Endpoint);
        Assert.Equal("activities-db", alias.Database);
        Assert.Equal("event-store", alias.Container);
    }

    [Fact]
    public void GenerateAliases_BlobGenerator_GeneratesAliases()
    {
        var config = new GeneratorsConfig
        {
            Dimensions = new Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>
            {
                ["kingdoms"] = new()
                {
                    ["ninja"] = new GeneratorDimensionEntry
                    {
                        Name = "ninja",
                        Properties = new Dictionary<string, string> { ["abbrev"] = "nja" }
                    }
                },
                ["backends"] = new()
                {
                    ["activities"] = new GeneratorDimensionEntry
                    {
                        Name = "activities",
                        Properties = new Dictionary<string, string>()
                    }
                }
            },
            Blob = new BlobGeneratorConfig
            {
                Enabled = true,
                AliasNameTemplate = "{kingdoms}-{backends}-blob",
                AccountTemplate = "king{abbrev}be",
                ContainerTemplate = "{backends}-blobs"
            }
        };

        var result = _generator.GenerateAliases(config);

        Assert.Single(result.BlobAliases);
        Assert.True(result.BlobAliases.TryGetValue("ninja-activities-blob", out var alias));
        Assert.Equal("kingnjabe", alias.Account);
        Assert.Equal("activities-blobs", alias.Container);
    }

    [Fact]
    public void GenerateAliases_StorageGenerator_DeduplicatesAliases()
    {
        var config = new GeneratorsConfig
        {
            Dimensions = new Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>
            {
                ["kingdoms"] = new()
                {
                    ["ninja"] = new GeneratorDimensionEntry
                    {
                        Name = "ninja",
                        Properties = new Dictionary<string, string> { ["abbrev"] = "nja" }
                    }
                },
                ["backends"] = new()
                {
                    ["activities"] = new GeneratorDimensionEntry { Name = "activities", Properties = [] },
                    ["invoices"] = new GeneratorDimensionEntry { Name = "invoices", Properties = [] }
                }
            },
            Storage = new StorageGeneratorConfig
            {
                Enabled = true,
                AliasNameTemplate = "{kingdoms}-storage",
                AccountTemplate = "king{abbrev}be"
            }
        };

        var result = _generator.GenerateAliases(config);

        // Should only have 1 storage alias (deduplicated by name)
        Assert.Single(result.StorageAliases);
        Assert.True(result.StorageAliases.TryGetValue("ninja-storage", out var alias));
        Assert.Equal("kingnjabe", alias.Account);
    }

    [Fact]
    public void GenerateAliases_DisabledGenerator_DoesNotGenerate()
    {
        var config = new GeneratorsConfig
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
                Enabled = false,
                AliasNameTemplate = "{kingdoms}-test",
                EndpointTemplate = "https://test.com",
                DatabaseTemplate = "db",
                Types = new Dictionary<string, string> { ["event"] = "events" }
            }
        };

        var result = _generator.GenerateAliases(config);

        Assert.Empty(result.CosmosAliases);
    }

    [Fact]
    public void GenerateAliases_EmptyDimensions_WithUnresolvedVars_SkipsAliases()
    {
        var config = new GeneratorsConfig
        {
            Dimensions = [],
            Cosmos = new CosmosGeneratorConfig
            {
                Enabled = true,
                AliasNameTemplate = "{kingdoms}-{type}",
                EndpointTemplate = "https://test.com",
                DatabaseTemplate = "db",
                Types = new Dictionary<string, string> { ["event"] = "events" }
            }
        };

        var result = _generator.GenerateAliases(config);

        // With unresolved variables in template, aliases are skipped
        Assert.Empty(result.CosmosAliases);
    }

    [Fact]
    public void GenerateAliases_EmptyDimensions_WithResolvableTemplate_GeneratesAliases()
    {
        var config = new GeneratorsConfig
        {
            Dimensions = [],
            Cosmos = new CosmosGeneratorConfig
            {
                Enabled = true,
                AliasNameTemplate = "static-{type}",
                EndpointTemplate = "https://test.com",
                DatabaseTemplate = "db",
                Types = new Dictionary<string, string> { ["event"] = "events" }
            }
        };

        var result = _generator.GenerateAliases(config);

        // With resolvable template (type is provided by types map), aliases are generated
        Assert.Single(result.CosmosAliases);
        Assert.Contains("static-event", result.CosmosAliases.Keys);
    }

    [Fact]
    public void GenerateAliases_PrefixedProperties_DisambiguateCollisions()
    {
        // When multiple dimensions have the same property name, prefixed keys allow disambiguation
        var config = new GeneratorsConfig
        {
            Dimensions = new Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>
            {
                ["kingdoms"] = new()
                {
                    ["ninja"] = new GeneratorDimensionEntry
                    {
                        Name = "ninja",
                        Properties = new Dictionary<string, string> { ["abbrev"] = "nja" }
                    }
                },
                ["backends"] = new()
                {
                    ["activities"] = new GeneratorDimensionEntry
                    {
                        Name = "activities",
                        // Same property name as kingdoms, would collide if unprefixed
                        Properties = new Dictionary<string, string> { ["abbrev"] = "act" }
                    }
                }
            },
            Cosmos = new CosmosGeneratorConfig
            {
                Enabled = true,
                AliasNameTemplate = "{kingdoms}-{backends}-{type}",
                // Use prefixed keys to disambiguate
                EndpointTemplate = "https://{kingdoms_abbrev}-{backends_abbrev}.documents.azure.com:443/",
                DatabaseTemplate = "db",
                Types = new Dictionary<string, string> { ["event"] = "event-store" }
            }
        };

        var result = _generator.GenerateAliases(config);

        Assert.Single(result.CosmosAliases);
        Assert.True(result.CosmosAliases.TryGetValue("ninja-activities-event", out var alias));
        // Prefixed keys allow using both abbrev values
        Assert.Equal("https://nja-act.documents.azure.com:443/", alias.Endpoint);
    }

    [Fact]
    public void GenerateAliases_WithKeyEnvTemplate_SetsKeyEnv()
    {
        var config = new GeneratorsConfig
        {
            Dimensions = new Dictionary<string, Dictionary<string, GeneratorDimensionEntry>>
            {
                ["kingdoms"] = new()
                {
                    ["ninja"] = new GeneratorDimensionEntry
                    {
                        Name = "ninja",
                        Properties = new Dictionary<string, string> { ["key_env"] = "NINJA_KEY" }
                    }
                }
            },
            Cosmos = new CosmosGeneratorConfig
            {
                Enabled = true,
                AliasNameTemplate = "{kingdoms}-{type}",
                EndpointTemplate = "https://test.com",
                DatabaseTemplate = "db",
                KeyEnvTemplate = "{key_env}",
                Types = new Dictionary<string, string> { ["event"] = "events" }
            }
        };

        var result = _generator.GenerateAliases(config);

        Assert.Single(result.CosmosAliases);
        Assert.Equal("NINJA_KEY", result.CosmosAliases["ninja-event"].KeyEnv);
    }
}
