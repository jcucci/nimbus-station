using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Output;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class BrowseCommandTests
{
    private readonly StubConfigurationService _configurationService;
    private readonly StubSessionStateManager _sessionStateManager;
    private readonly BrowseCommand _command;
    private readonly CaptureOutputWriter _outputWriter;

    public BrowseCommandTests()
    {
        _configurationService = new StubConfigurationService();
        _sessionStateManager = new StubSessionStateManager();
        _command = new BrowseCommand(_configurationService);
        _outputWriter = new CaptureOutputWriter();
    }

    [Fact]
    public async Task ExecuteAsync_NoArgs_ReturnsUsageError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.False(result.Success);
        Assert.Contains("Usage", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownSubcommand_ReturnsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["queue"], context);

        Assert.False(result.Success);
        Assert.Contains("Unknown type", result.Message);
        Assert.Contains("queue", result.Message);
    }

    [Fact]
    public void Name_ReturnsBrowse()
    {
        Assert.Equal("browse", _command.Name);
    }

    [Fact]
    public void Subcommands_ContainsExpectedValues()
    {
        Assert.Contains("cosmos", _command.Subcommands);
        Assert.Contains("blob", _command.Subcommands);
        Assert.Contains("storage", _command.Subcommands);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(_command.Description));
    }

    [Fact]
    public async Task ExecuteAsync_CosmosNonInteractive_ReturnsError()
    {
        // In test environment, AnsiConsole.Profile.Capabilities.Interactive is false
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["cosmos"], context);

        Assert.False(result.Success);
        Assert.Contains("interactive terminal", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_BlobNonInteractive_ReturnsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["blob"], context);

        Assert.False(result.Success);
        Assert.Contains("interactive terminal", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_StorageNonInteractive_ReturnsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["storage"], context);

        Assert.False(result.Success);
        Assert.Contains("interactive terminal", result.Message);
    }
}
