using NimbusStation.Cli.Commands;
using NimbusStation.Core.Commands;

namespace NimbusStation.Tests.Cli.Commands;

public sealed class CommandRegistryTests
{
    private readonly CommandRegistry _registry;

    public CommandRegistryTests()
    {
        _registry = new CommandRegistry();
    }

    [Fact]
    public void Register_Command_CanBeRetrieved()
    {
        var command = new TestCommand("test");

        _registry.Register(command);

        Assert.Same(command, _registry.GetCommand("test"));
    }

    [Fact]
    public void GetCommand_UnregisteredName_ReturnsNull()
    {
        Assert.Null(_registry.GetCommand("nonexistent"));
    }

    [Fact]
    public void GetCommand_IsCaseSensitive()
    {
        var command = new TestCommand("test");
        _registry.Register(command);

        Assert.Same(command, _registry.GetCommand("test"));
        Assert.Null(_registry.GetCommand("TEST"));
        Assert.Null(_registry.GetCommand("Test"));
    }

    [Fact]
    public void RegisterAlias_AliasResolvesToCommand()
    {
        var command = new TestCommand("help");
        _registry.Register(command);
        _registry.RegisterAlias("?", "help");

        Assert.Same(command, _registry.GetCommand("?"));
    }

    [Fact]
    public void RegisterAlias_IsCaseSensitive()
    {
        var command = new TestCommand("help");
        _registry.Register(command);
        _registry.RegisterAlias("h", "help");

        Assert.Same(command, _registry.GetCommand("h"));
        Assert.Null(_registry.GetCommand("H"));
    }

    [Fact]
    public void GetCommand_AliasToNonexistentCommand_ReturnsNull()
    {
        _registry.RegisterAlias("?", "nonexistent");

        Assert.Null(_registry.GetCommand("?"));
    }

    [Fact]
    public void HasCommand_ReturnsTrueForRegisteredCommand()
    {
        _registry.Register(new TestCommand("test"));

        Assert.True(_registry.HasCommand("test"));
    }

    [Fact]
    public void HasCommand_ReturnsTrueForAlias()
    {
        _registry.Register(new TestCommand("help"));
        _registry.RegisterAlias("?", "help");

        Assert.True(_registry.HasCommand("?"));
    }

    [Fact]
    public void HasCommand_ReturnsFalseForUnknown()
    {
        Assert.False(_registry.HasCommand("nonexistent"));
    }

    [Fact]
    public void GetAllCommands_ReturnsRegisteredCommands()
    {
        var cmd1 = new TestCommand("foo");
        var cmd2 = new TestCommand("bar");
        _registry.Register(cmd1);
        _registry.Register(cmd2);

        var commands = _registry.GetAllCommands().ToList();

        Assert.Contains(cmd1, commands);
        Assert.Contains(cmd2, commands);
        Assert.Equal(2, commands.Count);
    }

    [Fact]
    public void GetAllCommandNames_IncludesCommandsAndAliases()
    {
        _registry.Register(new TestCommand("help"));
        _registry.Register(new TestCommand("session"));
        _registry.RegisterAlias("?", "help");

        var names = _registry.GetAllCommandNames().ToList();

        Assert.Contains("help", names);
        Assert.Contains("session", names);
        Assert.Contains("?", names);
        Assert.Equal(3, names.Count);
    }

    [Fact]
    public void Register_AutoRegistersCommandAliases()
    {
        var command = new TestCommandWithAliases("foo", ["f", "fo"]);
        _registry.Register(command);

        Assert.Same(command, _registry.GetCommand("foo"));
        Assert.Same(command, _registry.GetCommand("f"));
        Assert.Same(command, _registry.GetCommand("fo"));
    }

    [Fact]
    public void Register_ThrowsOnDuplicateCommandName()
    {
        _registry.Register(new TestCommand("test"));

        var ex = Assert.Throws<InvalidOperationException>(() => _registry.Register(new TestCommand("test")));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void Register_ThrowsWhenAliasConflictsWithCommandName()
    {
        _registry.Register(new TestCommand("help"));

        var ex = Assert.Throws<InvalidOperationException>(
            () => _registry.Register(new TestCommandWithAliases("foo", ["help"])));
        Assert.Contains("conflicts with existing command", ex.Message);
    }

    [Fact]
    public void Register_ThrowsWhenCommandNameConflictsWithExistingAlias()
    {
        _registry.Register(new TestCommandWithAliases("foo", ["bar"]));

        var ex = Assert.Throws<InvalidOperationException>(() => _registry.Register(new TestCommand("bar")));
        Assert.Contains("conflicts with existing alias", ex.Message);
    }

    [Fact]
    public void Register_ThrowsOnDuplicateAlias()
    {
        _registry.Register(new TestCommandWithAliases("foo", ["x"]));

        var ex = Assert.Throws<InvalidOperationException>(
            () => _registry.Register(new TestCommandWithAliases("bar", ["x"])));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void RegisterAlias_ThrowsWhenAliasConflictsWithCommandName()
    {
        _registry.Register(new TestCommand("help"));

        var ex = Assert.Throws<InvalidOperationException>(() => _registry.RegisterAlias("help", "other"));
        Assert.Contains("conflicts with existing command", ex.Message);
    }

    [Fact]
    public void RegisterAlias_ThrowsOnDuplicateAlias()
    {
        _registry.Register(new TestCommand("help"));
        _registry.RegisterAlias("h", "help");

        var ex = Assert.Throws<InvalidOperationException>(() => _registry.RegisterAlias("h", "other"));
        Assert.Contains("already registered", ex.Message);
    }

    private sealed class TestCommand : ICommand
    {
        public string Name { get; }
        public string Description => "Test command";
        public string Usage => $"{Name} [args]";
        public IReadOnlySet<string> Subcommands { get; } = new HashSet<string>();

        public TestCommand(string name) => Name = name;

        public Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult.Ok());
    }

    private sealed class TestCommandWithAliases : ICommand
    {
        public string Name { get; }
        public string Description => "Test command with aliases";
        public string Usage => $"{Name} [args]";
        public IReadOnlySet<string> Subcommands { get; } = new HashSet<string>();
        public IReadOnlySet<string> Aliases { get; }

        public TestCommandWithAliases(string name, IEnumerable<string> aliases)
        {
            Name = name;
            Aliases = aliases.ToHashSet();
        }

        public Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult.Ok());
    }
}
