using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Providers.Azure.Blob;
using NimbusStation.Providers.Azure.Cli;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Providers.Azure.Blob;

public sealed class BlobServiceTests
{
    private readonly MockAzureCliExecutor _cliExecutor;
    private readonly StubConfigurationService _configurationService;
    private readonly BlobService _service;

    public BlobServiceTests()
    {
        _cliExecutor = new MockAzureCliExecutor();
        _configurationService = new StubConfigurationService();
        _service = new BlobService(_cliExecutor, _configurationService);
    }

    [Fact]
    public async Task ListContainersAsync_StorageAliasNotFound_ThrowsInvalidOperation()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ListContainersAsync("missing-alias"));

        Assert.Contains("not found in config", exception.Message);
        Assert.Contains("missing-alias", exception.Message);
    }

    [Fact]
    public async Task ListContainersAsync_CliError_ThrowsInvalidOperation()
    {
        _configurationService.AddStorageAlias("test-storage", new StorageAliasConfig("teststorage"));
        _cliExecutor.SetupFailure("Auth failed");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ListContainersAsync("test-storage"));

        Assert.Contains("Auth failed", exception.Message);
    }

    [Fact]
    public async Task ListContainersAsync_PassesCorrectArguments()
    {
        _configurationService.AddStorageAlias("test-storage", new StorageAliasConfig("mystorageaccount"));
        _cliExecutor.SetupJsonResult("[]");

        await _service.ListContainersAsync("test-storage");

        var call = Assert.Single(_cliExecutor.ExecuteCalls);
        Assert.Contains("storage container list", call);
        Assert.Contains("--account-name \"mystorageaccount\"", call);
        Assert.Contains("--auth-mode login", call);
    }

    [Fact]
    public async Task ListBlobsAsync_BlobAliasNotFound_ThrowsInvalidOperation()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ListBlobsAsync("missing-alias"));

        Assert.Contains("not found in config", exception.Message);
        Assert.Contains("missing-alias", exception.Message);
    }

    [Fact]
    public async Task ListBlobsAsync_PassesCorrectArguments()
    {
        _configurationService.AddBlobAlias("test-blob", new BlobAliasConfig("mystorageaccount", "mycontainer"));
        _cliExecutor.SetupJsonResult("[]");

        await _service.ListBlobsAsync("test-blob");

        var call = Assert.Single(_cliExecutor.ExecuteCalls);
        Assert.Contains("storage blob list", call);
        Assert.Contains("--account-name \"mystorageaccount\"", call);
        Assert.Contains("--container-name \"mycontainer\"", call);
        Assert.Contains("--auth-mode login", call);
    }

    [Fact]
    public async Task ListBlobsAsync_WithPrefix_IncludesPrefixInArguments()
    {
        _configurationService.AddBlobAlias("test-blob", new BlobAliasConfig("mystorageaccount", "mycontainer"));
        _cliExecutor.SetupJsonResult("[]");

        await _service.ListBlobsAsync("test-blob", prefix: "exports/2024/");

        var call = Assert.Single(_cliExecutor.ExecuteCalls);
        Assert.Contains("--prefix \"exports/2024/\"", call);
    }

    [Fact]
    public async Task ListBlobsAsync_WithoutPrefix_DoesNotIncludePrefixArgument()
    {
        _configurationService.AddBlobAlias("test-blob", new BlobAliasConfig("mystorageaccount", "mycontainer"));
        _cliExecutor.SetupJsonResult("[]");

        await _service.ListBlobsAsync("test-blob");

        var call = Assert.Single(_cliExecutor.ExecuteCalls);
        Assert.DoesNotContain("--prefix", call);
    }

    [Fact]
    public async Task GetBlobContentAsync_BlobAliasNotFound_ThrowsInvalidOperation()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetBlobContentAsync("missing-alias", "some/blob.json"));

        Assert.Contains("not found in config", exception.Message);
    }

    [Fact]
    public async Task GetBlobContentAsync_PassesCorrectArgumentsForBlobShow()
    {
        _configurationService.AddBlobAlias("test-blob", new BlobAliasConfig("mystorageaccount", "mycontainer"));
        _cliExecutor.SetupJsonResult("{\"name\":\"data.json\",\"properties\":{\"contentType\":\"application/json\"}}");

        // This will fail on the download step, but we can verify the first call
        try { await _service.GetBlobContentAsync("test-blob", "path/to/data.json"); } catch { }

        var showCall = _cliExecutor.ExecuteCalls.FirstOrDefault(c => c.Contains("blob show"));
        Assert.NotNull(showCall);
        Assert.Contains("--account-name", showCall);
        Assert.Contains("--container-name", showCall);
        Assert.Contains("--name", showCall);
        Assert.Contains("--auth-mode login", showCall);
    }

    [Fact]
    public async Task DownloadBlobAsync_BlobAliasNotFound_ThrowsInvalidOperation()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DownloadBlobAsync("missing-alias", "some/blob.json", "/tmp"));

        Assert.Contains("not found in config", exception.Message);
    }

    [Fact]
    public async Task DownloadBlobAsync_PassesCorrectArguments()
    {
        _configurationService.AddBlobAlias("test-blob", new BlobAliasConfig("mystorageaccount", "mycontainer"));
        _cliExecutor.SetupJsonResult(""); // Empty result for download

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            await _service.DownloadBlobAsync("test-blob", "exports/data.json", tempDir);

            var call = Assert.Single(_cliExecutor.ExecuteCalls);
            Assert.Contains("storage blob download", call);
            Assert.Contains("--account-name", call);
            Assert.Contains("--container-name", call);
            Assert.Contains("--name", call);
            Assert.Contains("--file", call);
            Assert.Contains("--auth-mode login", call);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadBlobAsync_CreatesDirectoryStructure()
    {
        _configurationService.AddBlobAlias("test-blob", new BlobAliasConfig("mystorageaccount", "mycontainer"));
        _cliExecutor.SetupJsonResult("");

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = await _service.DownloadBlobAsync("test-blob", "nested/path/file.json", tempDir);

            var expectedDir = Path.Combine(tempDir, "nested", "path");
            Assert.True(Directory.Exists(expectedDir), $"Expected directory {expectedDir} to be created");
            Assert.Contains("nested", result);
            Assert.Contains("file.json", result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ListContainersAsync_InvalidAccountName_ThrowsArgumentException()
    {
        _configurationService.AddStorageAlias("bad-alias", new StorageAliasConfig("INVALID_UPPERCASE"));

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ListContainersAsync("bad-alias"));

        Assert.Contains("Invalid Azure resource name", exception.Message);
    }

    [Fact]
    public async Task ListBlobsAsync_QuotesAccountAndContainerNames()
    {
        _configurationService.AddBlobAlias("test-blob", new BlobAliasConfig("mystorageaccount", "mycontainer"));
        _cliExecutor.SetupJsonResult("[]");

        await _service.ListBlobsAsync("test-blob");

        var call = Assert.Single(_cliExecutor.ExecuteCalls);
        Assert.Contains("\"mystorageaccount\"", call);
        Assert.Contains("\"mycontainer\"", call);
    }

    [Fact]
    public void IsBinaryContentType_TextTypes_ReturnsFalse()
    {
        Assert.False(BlobContentResult.IsBinaryContentType("text/plain"));
        Assert.False(BlobContentResult.IsBinaryContentType("text/html"));
        Assert.False(BlobContentResult.IsBinaryContentType("application/json"));
        Assert.False(BlobContentResult.IsBinaryContentType("application/xml"));
    }

    [Fact]
    public void IsBinaryContentType_BinaryTypes_ReturnsTrue()
    {
        Assert.True(BlobContentResult.IsBinaryContentType("application/octet-stream"));
        Assert.True(BlobContentResult.IsBinaryContentType("application/zip"));
        Assert.True(BlobContentResult.IsBinaryContentType("application/gzip"));
        Assert.True(BlobContentResult.IsBinaryContentType("image/png"));
        Assert.True(BlobContentResult.IsBinaryContentType("image/jpeg"));
        Assert.True(BlobContentResult.IsBinaryContentType("audio/mpeg"));
        Assert.True(BlobContentResult.IsBinaryContentType("video/mp4"));
    }

    [Fact]
    public void IsBinaryContentType_EmptyOrNull_ReturnsFalse()
    {
        Assert.False(BlobContentResult.IsBinaryContentType(""));
        Assert.False(BlobContentResult.IsBinaryContentType(null!));
        Assert.False(BlobContentResult.IsBinaryContentType("   "));
    }
}
