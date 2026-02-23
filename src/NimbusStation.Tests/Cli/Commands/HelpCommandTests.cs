using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Output;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class HelpCommandTests
{
    private readonly StubSessionStateManager _sessionStateManager;
    private readonly StubConfigurationService _configurationService;
    private readonly CommandRegistry _registry;
    private readonly HelpCommand _command;
    private readonly CaptureOutputWriter _outputWriter;

    public HelpCommandTests()
    {
        _sessionStateManager = new StubSessionStateManager();
        _configurationService = new StubConfigurationService();
        _registry = new CommandRegistry();
        _outputWriter = new CaptureOutputWriter();

        // Register HelpCommand and a test command
        // Use Func<CommandRegistry> to avoid circular dependency during construction
        _command = new HelpCommand(() => _registry, _configurationService);
        _registry.Register(_command);
        _registry.Register(new TestCommand());
    }

    [Fact]
    public async Task ExecuteAsync_NoArgs_ShowsCommandTable()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
        Assert.Contains("Available Commands", _outputWriter.GetOutput());
    }

    [Fact]
    public async Task ExecuteAsync_ValidCommand_ShowsSpecificHelp()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["test"], context);

        Assert.True(result.Success);
        Assert.Contains("test", _outputWriter.GetOutput());
        Assert.Contains("A test command", _outputWriter.GetOutput());
        Assert.Contains("Usage", _outputWriter.GetOutput());
    }

    [Fact]
    public async Task ExecuteAsync_UnknownCommand_ShowsError()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["nonexistent"], context);

        Assert.True(result.Success);
        Assert.Contains("Unknown command", _outputWriter.GetOutput());
        Assert.Contains("nonexistent", _outputWriter.GetOutput());
    }

    [Fact]
    public async Task ExecuteAsync_HelpCommand_ShowsHelpForItself()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync(["help"], context);

        Assert.True(result.Success);
        Assert.Contains("help", _outputWriter.GetOutput());
        Assert.Contains("Display help information", _outputWriter.GetOutput());
    }

    [Fact]
    public void Name_ReturnsHelp()
    {
        Assert.Equal("help", _command.Name);
    }

    [Fact]
    public void Description_ReturnsExpected()
    {
        Assert.Equal("Display help information for commands", _command.Description);
    }

    [Fact]
    public void Usage_ReturnsExpected()
    {
        Assert.Equal("help [command]", _command.Usage);
    }

    [Fact]
    public void Subcommands_ContainsRegisteredCommandNames()
    {
        var subcommands = _command.Subcommands;

        Assert.Contains("help", subcommands);
        Assert.Contains("test", subcommands);
    }

    [Fact]
    public void Aliases_ContainsQuestionMark()
    {
        Assert.Contains("?", _command.Aliases);
        Assert.Single(_command.Aliases);
    }

    [Fact]
    public void CanBePiped_ReturnsFalse() => Assert.False(_command.CanBePiped);

    [Fact]
    public void HelpMetadata_IsNotNull() => Assert.NotNull(_command.HelpMetadata);

    private sealed class TestCommand : ICommand
    {
        public string Name => "test";
        public string Description => "A test command";
        public string Usage => "test [args]";
        public IReadOnlySet<string> Subcommands { get; } = new HashSet<string>();

        public Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult.Ok());
    }
}
