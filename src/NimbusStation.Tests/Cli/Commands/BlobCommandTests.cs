using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Infrastructure.Output;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class BlobCommandTests
{
    private readonly MockBlobService _blobService;
    private readonly StubConfigurationService _configurationService;
    private readonly StubSessionService _sessionService;
    private readonly StubSessionStateManager _sessionStateManager;
    private readonly BlobCommand _command;
    private readonly CaptureOutputWriter _outputWriter;

    public BlobCommandTests()
    {
        _blobService = new MockBlobService();
        _configurationService = new StubConfigurationService();
        _sessionService = new StubSessionService();
        _sessionStateManager = new StubSessionStateManager();
        _command = new BlobCommand(_blobService, _configurationService, _sessionService);
        _outputWriter = new CaptureOutputWriter();
    }

    [Fact]
    public async Task ExecuteAsync_NoSession_ReturnsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["list"], context);

        Assert.False(result.Success);
        Assert.Contains("No active session", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_NoArgs_ReturnsUsageError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync([], context);

        Assert.False(result.Success);
        Assert.Contains("Usage", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownSubcommand_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["unknown"], context);

        Assert.False(result.Success);
        Assert.Contains("Unknown subcommand", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ContainersWithoutStorageContext_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["containers"], context);

        Assert.False(result.Success);
        Assert.Contains("No active storage context", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ContainersWithMissingAlias_ReturnsError()
    {
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext(null, null, "missing-alias")));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["containers"], context);

        Assert.False(result.Success);
        Assert.Contains("not found in config", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ContainersSuccess_CallsService()
    {
        SetupValidStorageContext();
        _blobService.SetupContainerListResult(MockBlobService.CreateContainerResult("container1", "container2"));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["containers"], context);

        Assert.True(result.Success);
        var call = Assert.Single(_blobService.ListContainersCalls);
        Assert.Equal("prod-storage", call);
    }

    [Fact]
    public async Task ExecuteAsync_ListWithoutBlobContext_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["list"], context);

        Assert.False(result.Success);
        Assert.Contains("No active blob context", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ListWithMissingAlias_ReturnsError()
    {
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext(null, "missing-alias", null)));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["list"], context);

        Assert.False(result.Success);
        Assert.Contains("not found in config", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ListSuccess_CallsService()
    {
        SetupValidBlobContext();
        _blobService.SetupBlobListResult(MockBlobService.CreateBlobResult(("file1.json", 1024), ("file2.json", 2048)));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["list"], context);

        Assert.True(result.Success);
        var call = Assert.Single(_blobService.ListBlobsCalls);
        Assert.Equal("prod-exports", call.AliasName);
        Assert.Null(call.Prefix);
    }

    [Fact]
    public async Task ExecuteAsync_ListWithPrefix_PassesPrefixToService()
    {
        SetupValidBlobContext();
        _blobService.SetupBlobListResult(MockBlobService.CreateEmptyBlobResult());
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["list", "exports/2024/"], context);

        Assert.True(result.Success);
        var call = Assert.Single(_blobService.ListBlobsCalls);
        Assert.Equal("exports/2024/", call.Prefix);
    }

    [Fact]
    public async Task ExecuteAsync_GetWithoutPath_ReturnsUsageError()
    {
        SetupValidBlobContext();
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["get"], context);

        Assert.False(result.Success);
        Assert.Contains("Usage", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_GetWithoutBlobContext_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["get", "some/file.json"], context);

        Assert.False(result.Success);
        Assert.Contains("No active blob context", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_GetSuccess_CallsService()
    {
        SetupValidBlobContext();
        _blobService.SetupContentResult(MockBlobService.CreateTextContent("{\"test\": true}"));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["get", "data/file.json"], context);

        Assert.True(result.Success);
        var call = Assert.Single(_blobService.GetContentCalls);
        Assert.Equal("prod-exports", call.AliasName);
        Assert.Equal("data/file.json", call.BlobName);
    }

    [Fact]
    public async Task ExecuteAsync_GetSuccess_OutputsRawContent()
    {
        SetupValidBlobContext();
        _blobService.SetupContentResult(MockBlobService.CreateTextContent("{\"test\": true}"));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["get", "data/file.json"], context);

        var output = _outputWriter.GetOutput();
        Assert.Contains("{\"test\": true}", output);
    }

    [Fact]
    public async Task ExecuteAsync_DownloadWithoutPath_ReturnsUsageError()
    {
        SetupValidBlobContext();
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["download"], context);

        Assert.False(result.Success);
        Assert.Contains("Usage", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_DownloadWithoutBlobContext_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["download", "some/file.json"], context);

        Assert.False(result.Success);
        Assert.Contains("No active blob context", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_DownloadSuccess_CallsService()
    {
        SetupValidBlobContext();
        _blobService.SetupDownloadPath("/tmp/downloads/data/file.json");
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["download", "data/file.json"], context);

        Assert.True(result.Success);
        var call = Assert.Single(_blobService.DownloadCalls);
        Assert.Equal("prod-exports", call.AliasName);
        Assert.Equal("data/file.json", call.BlobName);
    }

    [Fact]
    public async Task ExecuteAsync_DownloadSuccess_OutputsPath()
    {
        SetupValidBlobContext();
        _blobService.SetupDownloadPath("/home/user/.nimbus/sessions/TEST-123/downloads/data/file.json");
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        await _command.ExecuteAsync(["download", "data/file.json"], context);

        var output = _outputWriter.GetOutput();
        Assert.Contains("Downloaded to:", output);
        Assert.Contains("file.json", output);
    }

    [Fact]
    public async Task ExecuteAsync_ServiceThrowsException_ReturnsError()
    {
        SetupValidBlobContext();
        _blobService.SetupException(new InvalidOperationException("Azure CLI not authenticated"));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["list"], context);

        Assert.False(result.Success);
        Assert.Contains("Azure CLI not authenticated", result.Message);
    }

    [Fact]
    public void Name_ReturnsBlob() => Assert.Equal("blob", _command.Name);

    [Fact]
    public void Subcommands_ContainsExpectedCommands()
    {
        Assert.Contains("containers", _command.Subcommands);
        Assert.Contains("list", _command.Subcommands);
        Assert.Contains("get", _command.Subcommands);
        Assert.Contains("download", _command.Subcommands);
        Assert.Contains("search", _command.Subcommands);
    }

    [Fact]
    public async Task ExecuteAsync_SearchWithoutBlobContext_ReturnsError()
    {
        var context = CreateContextWithSession();

        var result = await _command.ExecuteAsync(["search"], context);

        Assert.False(result.Success);
        Assert.Contains("No active blob context", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_SearchWithMissingAlias_ReturnsError()
    {
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext(null, "missing-alias", null)));
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["search"], context);

        Assert.False(result.Success);
        Assert.Contains("not found in config", result.Message);
    }

    private void SetupValidStorageContext()
    {
        _configurationService.AddStorageAlias("prod-storage", new StorageAliasConfig("prodstorageaccount"));
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext(null, null, "prod-storage")));
    }

    private void SetupValidBlobContext()
    {
        _configurationService.AddBlobAlias("prod-exports", new BlobAliasConfig("prodstorageaccount", "exports"));
        _sessionStateManager.ActivateSession(Session.Create("TEST-123")
            .WithContext(new SessionContext(null, "prod-exports", null)));
    }

    private CommandContext CreateContextWithSession()
    {
        _sessionStateManager.ActivateSession(Session.Create("TEST-123"));
        return new CommandContext(_sessionStateManager, _outputWriter);
    }
}
