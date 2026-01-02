using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Output;
using NimbusStation.Tests.Fixtures;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class ExitCommandTests
{
    private readonly StubSessionStateManager _sessionStateManager;
    private readonly ExitCommand _command;
    private readonly CaptureOutputWriter _outputWriter;

    public ExitCommandTests()
    {
        _sessionStateManager = new StubSessionStateManager();
        _command = new ExitCommand();
        _outputWriter = new CaptureOutputWriter();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsExitSignal()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.IsExitSignal);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessTrue()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsGoodbyeMessage()
    {
        var context = new CommandContext(_sessionStateManager, _outputWriter);

        var result = await _command.ExecuteAsync([], context);

        Assert.Equal("Goodbye!", result.Message);
    }

    [Fact]
    public void Name_ReturnsExit()
    {
        Assert.Equal("exit", _command.Name);
    }

    [Fact]
    public void Description_ReturnsExpected()
    {
        Assert.Equal("Exit the REPL", _command.Description);
    }

    [Fact]
    public void Usage_ReturnsExpected()
    {
        Assert.Equal("exit", _command.Usage);
    }

    [Fact]
    public void Subcommands_IsEmpty()
    {
        Assert.Empty(_command.Subcommands);
    }
}
