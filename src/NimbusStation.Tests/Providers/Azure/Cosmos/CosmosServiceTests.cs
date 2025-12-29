using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Providers.Azure.Cosmos;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Providers.Azure.Cosmos;

public sealed class CosmosServiceTests : IDisposable
{
    private readonly StubConfigurationService _configurationService;
    private readonly CosmosService _service;
    private readonly string _testEnvVar = $"COSMOS_TEST_KEY_{Guid.NewGuid():N}";

    public CosmosServiceTests()
    {
        _configurationService = new StubConfigurationService();
        _service = new CosmosService(_configurationService);
    }

    public void Dispose()
    {
        _service.Dispose();
        Environment.SetEnvironmentVariable(_testEnvVar, null);
    }

    [Fact]
    public async Task ExecuteQueryAsync_AliasNotFound_ThrowsInvalidOperation()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecuteQueryAsync("missing-alias", "SELECT * FROM c"));

        Assert.Contains("not found in config", exception.Message);
        Assert.Contains("missing-alias", exception.Message);
    }

    [Fact]
    public async Task ExecuteQueryAsync_NoKeyEnvConfigured_ThrowsInvalidOperation()
    {
        _configurationService.AddCosmosAlias("test-alias", new CosmosAliasConfig(
            "https://test.documents.azure.com:443/",
            "TestDb",
            "TestContainer",
            KeyEnv: null));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecuteQueryAsync("test-alias", "SELECT * FROM c"));

        Assert.Contains("No key_env configured", exception.Message);
    }

    [Fact]
    public async Task ExecuteQueryAsync_EnvVarNotSet_ThrowsInvalidOperation()
    {
        _configurationService.AddCosmosAlias("test-alias", new CosmosAliasConfig(
            "https://test.documents.azure.com:443/",
            "TestDb",
            "TestContainer",
            KeyEnv: _testEnvVar));

        // Ensure env var is not set
        Environment.SetEnvironmentVariable(_testEnvVar, null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecuteQueryAsync("test-alias", "SELECT * FROM c"));

        Assert.Contains($"Set environment variable {_testEnvVar}", exception.Message);
    }

    [Fact]
    public async Task ExecuteQueryAsync_EmptyEnvVar_ThrowsInvalidOperation()
    {
        _configurationService.AddCosmosAlias("test-alias", new CosmosAliasConfig(
            "https://test.documents.azure.com:443/",
            "TestDb",
            "TestContainer",
            KeyEnv: _testEnvVar));

        Environment.SetEnvironmentVariable(_testEnvVar, "   ");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecuteQueryAsync("test-alias", "SELECT * FROM c"));

        Assert.Contains($"Set environment variable {_testEnvVar}", exception.Message);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var service = new CosmosService(_configurationService);

        // Should not throw
        service.Dispose();
        service.Dispose();
    }
}
