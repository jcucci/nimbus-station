using Microsoft.Extensions.Logging;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Infrastructure.Configuration.Generators;

namespace NimbusStation.Tests.Infrastructure.Configuration.Generators;

public sealed class GeneratorEngineTests : IDisposable
{
    private readonly LoggerFactory _loggerFactory;
    private readonly GeneratorEngine _engine;

    public GeneratorEngineTests()
    {
        _loggerFactory = new LoggerFactory();
        var logger = _loggerFactory.CreateLogger<GeneratorEngine>();
        _engine = new GeneratorEngine(logger);
    }

    public void Dispose() => _loggerFactory.Dispose();

    [Fact]
    public void GenerateCosmosAliases_DisabledGenerator_ReturnsEmpty()
    {
        var config = new CosmosGeneratorConfig
        {
            Enabled = false,
            AliasNameTemplate = "{kingdoms}-{backends}",
            EndpointTemplate = "https://{abbrev}.documents.azure.com:443/",
            DatabaseTemplate = "{database}"
        };

        var result = _engine.GenerateCosmosAliases(config, new Dictionary<string, IReadOnlyList<GeneratorDimension>>());

        Assert.Empty(result);
    }

    [Fact]
    public void GenerateCosmosAliases_SingleDimensionNoTypes_GeneratesAliases()
    {
        var config = new CosmosGeneratorConfig
        {
            Enabled = true,
            AliasNameTemplate = "{kingdoms}-cosmos",
            EndpointTemplate = "https://{abbrev}.documents.azure.com:443/",
            DatabaseTemplate = "mydb",
            ContainerTemplate = "mycontainer"
        };

        var dimensions = new Dictionary<string, IReadOnlyList<GeneratorDimension>>
        {
            ["kingdoms"] = new List<GeneratorDimension>
            {
                new() { Key = "ninja", Properties = new Dictionary<string, string> { ["abbrev"] = "ninja" } },
                new() { Key = "team", Properties = new Dictionary<string, string> { ["abbrev"] = "team" } }
            }
        };

        var result = _engine.GenerateCosmosAliases(config, dimensions);

        Assert.Equal(2, result.Count);
        Assert.True(result.TryGetValue("ninja-cosmos", out var ninjaAlias));
        Assert.True(result.TryGetValue("team-cosmos", out var teamAlias));
        Assert.Equal("https://ninja.documents.azure.com:443/", ninjaAlias.Endpoint);
        Assert.Equal("https://team.documents.azure.com:443/", teamAlias.Endpoint);
    }

    [Fact]
    public void GenerateCosmosAliases_WithTypes_GeneratesMultipleAliasesPerCombination()
    {
        var config = new CosmosGeneratorConfig
        {
            Enabled = true,
            AliasNameTemplate = "{kingdoms}-{type}",
            EndpointTemplate = "https://example.documents.azure.com:443/",
            DatabaseTemplate = "db",
            ContainerTemplate = "container-{type_suffix}",
            Types = new Dictionary<string, string>
            {
                ["event"] = "events",
                ["data"] = "data"
            }
        };

        var dimensions = new Dictionary<string, IReadOnlyList<GeneratorDimension>>
        {
            ["kingdoms"] = new List<GeneratorDimension>
            {
                new() { Key = "ninja", Properties = new Dictionary<string, string>() }
            }
        };

        var result = _engine.GenerateCosmosAliases(config, dimensions);

        Assert.Equal(2, result.Count);
        Assert.True(result.TryGetValue("ninja-event", out var eventAlias));
        Assert.True(result.TryGetValue("ninja-data", out var dataAlias));
        Assert.Equal("container-events", eventAlias.Container);
        Assert.Equal("container-data", dataAlias.Container);
    }

    [Fact]
    public void GenerateCosmosAliases_TwoDimensions_GeneratesCrossProduct()
    {
        var config = new CosmosGeneratorConfig
        {
            Enabled = true,
            AliasNameTemplate = "{kingdoms}-{backends}",
            EndpointTemplate = "https://{abbrev}.documents.azure.com:443/",
            DatabaseTemplate = "{database}",
            ContainerTemplate = "{container_base}"
        };

        var dimensions = new Dictionary<string, IReadOnlyList<GeneratorDimension>>
        {
            ["kingdoms"] = new List<GeneratorDimension>
            {
                new() { Key = "ninja", Properties = new Dictionary<string, string> { ["abbrev"] = "ninja" } },
                new() { Key = "team", Properties = new Dictionary<string, string> { ["abbrev"] = "team" } }
            },
            ["backends"] = new List<GeneratorDimension>
            {
                new() { Key = "invoices", Properties = new Dictionary<string, string> { ["database"] = "snowdrop", ["container_base"] = "invoices" } },
                new() { Key = "orders", Properties = new Dictionary<string, string> { ["database"] = "orders-db", ["container_base"] = "orders" } }
            }
        };

        var result = _engine.GenerateCosmosAliases(config, dimensions);

        Assert.Equal(4, result.Count); // 2 kingdoms x 2 backends
        Assert.True(result.ContainsKey("ninja-invoices"));
        Assert.True(result.ContainsKey("ninja-orders"));
        Assert.True(result.ContainsKey("team-invoices"));
        Assert.True(result.ContainsKey("team-orders"));
    }

    [Fact]
    public void GenerateBlobAliases_ValidConfig_GeneratesAliases()
    {
        var config = new BlobGeneratorConfig
        {
            Enabled = true,
            AliasNameTemplate = "{kingdoms}-{backends}-blob",
            AccountTemplate = "king{abbrev}stg",
            ContainerTemplate = "{container_base}"
        };

        var dimensions = new Dictionary<string, IReadOnlyList<GeneratorDimension>>
        {
            ["kingdoms"] = new List<GeneratorDimension>
            {
                new() { Key = "ninja", Properties = new Dictionary<string, string> { ["abbrev"] = "ninja" } }
            },
            ["backends"] = new List<GeneratorDimension>
            {
                new() { Key = "invoices", Properties = new Dictionary<string, string> { ["container_base"] = "snowdrop-invoices" } }
            }
        };

        var result = _engine.GenerateBlobAliases(config, dimensions);

        Assert.Single(result);
        Assert.True(result.TryGetValue("ninja-invoices-blob", out var blobAlias));
        Assert.Equal("kingninjastg", blobAlias.Account);
        Assert.Equal("snowdrop-invoices", blobAlias.Container);
    }

    [Fact]
    public void GenerateStorageAliases_ValidConfig_GeneratesAliases()
    {
        var config = new StorageGeneratorConfig
        {
            Enabled = true,
            AliasNameTemplate = "{kingdoms}-storage",
            AccountTemplate = "king{abbrev}stg"
        };

        var dimensions = new Dictionary<string, IReadOnlyList<GeneratorDimension>>
        {
            ["kingdoms"] = new List<GeneratorDimension>
            {
                new() { Key = "ninja", Properties = new Dictionary<string, string> { ["abbrev"] = "ninja" } },
                new() { Key = "team", Properties = new Dictionary<string, string> { ["abbrev"] = "team" } }
            }
        };

        var result = _engine.GenerateStorageAliases(config, dimensions);

        Assert.Equal(2, result.Count);
        Assert.True(result.TryGetValue("ninja-storage", out var ninjaStorage));
        Assert.True(result.TryGetValue("team-storage", out var teamStorage));
        Assert.Equal("kingninjastg", ninjaStorage.Account);
        Assert.Equal("kingteamstg", teamStorage.Account);
    }

    [Fact]
    public void GenerateCosmosAliases_MissingDimension_ReturnsEmpty()
    {
        var config = new CosmosGeneratorConfig
        {
            Enabled = true,
            AliasNameTemplate = "{kingdoms}-{backends}",
            EndpointTemplate = "https://example.documents.azure.com:443/",
            DatabaseTemplate = "db",
            ContainerTemplate = "container"
        };

        // Only provide kingdoms, not backends
        var dimensions = new Dictionary<string, IReadOnlyList<GeneratorDimension>>
        {
            ["kingdoms"] = new List<GeneratorDimension>
            {
                new() { Key = "ninja", Properties = new Dictionary<string, string>() }
            }
        };

        var result = _engine.GenerateCosmosAliases(config, dimensions);

        Assert.Empty(result);
    }
}
