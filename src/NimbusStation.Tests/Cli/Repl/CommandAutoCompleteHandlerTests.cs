using NimbusStation.Cli.Commands;
using NimbusStation.Cli.Repl;
using NimbusStation.Core.Commands;

namespace NimbusStation.Tests.Cli.Repl;

public class CommandAutoCompleteHandlerTests
{
    private readonly CommandRegistry _registry;
    private readonly CommandAutoCompleteHandler _handler;

    public CommandAutoCompleteHandlerTests()
    {
        _registry = new CommandRegistry();
        _registry.Register(new TestCommand("session", ["start", "list", "ls", "leave", "resume", "delete", "rm", "status"]));
        _registry.Register(new TestCommand("alias", ["list", "ls", "show", "add", "remove", "rm", "test"]));
        _registry.Register(new TestCommand("cosmos", []));
        _registry.Register(new TestCommand("help", []));
        _registry.RegisterAlias("?", "help");
        _registry.Register(new TestCommand("exit", []));
        _registry.RegisterAlias("quit", "exit");
        _registry.RegisterAlias("q", "exit");
        _handler = new CommandAutoCompleteHandler(_registry);
    }

    #region Command Name Completion

    [Fact]
    public void GetSuggestions_EmptyInput_ReturnsAllCommands()
    {
        var suggestions = _handler.GetSuggestions("", index: 0);

        Assert.Contains("session", suggestions);
        Assert.Contains("alias", suggestions);
        Assert.Contains("cosmos", suggestions);
        Assert.Contains("help", suggestions);
        Assert.Contains("exit", suggestions);
        Assert.Contains("quit", suggestions);
    }

    [Fact]
    public void GetSuggestions_PartialCommandName_ReturnsMatchingCommands()
    {
        var suggestions = _handler.GetSuggestions("se", index: 0);

        Assert.Contains("session", suggestions);
        Assert.DoesNotContain("alias", suggestions);
        Assert.DoesNotContain("cosmos", suggestions);
    }

    [Fact]
    public void GetSuggestions_PartialCommandName_IsCaseInsensitive()
    {
        var suggestions = _handler.GetSuggestions("SE", index: 0);

        Assert.Contains("session", suggestions);
    }

    [Fact]
    public void GetSuggestions_FullCommandName_ReturnsExactMatch()
    {
        var suggestions = _handler.GetSuggestions("session", index: 0);

        Assert.Contains("session", suggestions);
        Assert.Single(suggestions);
    }

    [Fact]
    public void GetSuggestions_NoMatch_ReturnsEmptyArray()
    {
        var suggestions = _handler.GetSuggestions("xyz", index: 0);

        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSuggestions_CommandsAndAliases_AreIncluded()
    {
        var suggestions = _handler.GetSuggestions("", index: 0);

        Assert.Contains("help", suggestions);
        Assert.Contains("exit", suggestions);
        Assert.Contains("quit", suggestions);
        Assert.Contains("q", suggestions);
        Assert.Contains("?", suggestions);
    }

    [Fact]
    public void GetSuggestions_PartialCommand_ReturnsMatch()
    {
        var suggestions = _handler.GetSuggestions("he", index: 0);

        Assert.Contains("help", suggestions);
        Assert.Single(suggestions);
    }

    #endregion

    #region Subcommand Completion

    [Fact]
    public void GetSuggestions_CommandWithSpace_ReturnsSubcommands()
    {
        var suggestions = _handler.GetSuggestions("session ", index: 8);

        Assert.Contains("start", suggestions);
        Assert.Contains("list", suggestions);
        Assert.Contains("leave", suggestions);
        Assert.Contains("resume", suggestions);
        Assert.Contains("delete", suggestions);
        Assert.Contains("status", suggestions);
    }

    [Fact]
    public void GetSuggestions_PartialSubcommand_ReturnsMatchingSubcommands()
    {
        var suggestions = _handler.GetSuggestions("session st", index: 10);

        Assert.Contains("start", suggestions);
        Assert.Contains("status", suggestions);
        Assert.DoesNotContain("list", suggestions);
        Assert.DoesNotContain("leave", suggestions);
    }

    [Fact]
    public void GetSuggestions_PartialSubcommand_IsCaseInsensitive()
    {
        var suggestions = _handler.GetSuggestions("session ST", index: 10);

        Assert.Contains("start", suggestions);
        Assert.Contains("status", suggestions);
    }

    [Fact]
    public void GetSuggestions_CommandWithNoSubcommands_ReturnsEmptyArray()
    {
        var suggestions = _handler.GetSuggestions("cosmos ", index: 7);

        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSuggestions_UnknownCommand_ReturnsEmptyArray()
    {
        var suggestions = _handler.GetSuggestions("unknown ", index: 8);

        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSuggestions_HelpCommandWithSpace_ReturnsEmptyArray()
    {
        var suggestions = _handler.GetSuggestions("help ", index: 5);

        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSuggestions_AliasSubcommands_ReturnsCorrectSubcommands()
    {
        var suggestions = _handler.GetSuggestions("alias ", index: 6);

        Assert.Contains("list", suggestions);
        Assert.Contains("show", suggestions);
        Assert.Contains("add", suggestions);
        Assert.Contains("remove", suggestions);
        Assert.Contains("test", suggestions);
    }

    [Fact]
    public void GetSuggestions_SubcommandNoMatch_ReturnsEmptyArray()
    {
        var suggestions = _handler.GetSuggestions("session xyz", index: 11);

        Assert.Empty(suggestions);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetSuggestions_AfterSubcommand_ReturnsSubcommandsWithEmptyPrefix()
    {
        // After "session start ", typing at end still suggests subcommands (for editing)
        var suggestions = _handler.GetSuggestions("session start ", index: 14);

        // Current behavior: returns all subcommands since empty prefix matches all
        Assert.Contains("start", suggestions);
        Assert.Contains("list", suggestions);
    }

    [Fact]
    public void GetSuggestions_ResultsAreSorted()
    {
        var suggestions = _handler.GetSuggestions("", index: 0);

        var sortedSuggestions = suggestions.OrderBy(s => s).ToArray();
        Assert.Equal(sortedSuggestions, suggestions);
    }

    [Fact]
    public void GetSuggestions_SubcommandResultsAreSorted()
    {
        var suggestions = _handler.GetSuggestions("session ", index: 8);

        var sortedSuggestions = suggestions.OrderBy(s => s).ToArray();
        Assert.Equal(sortedSuggestions, suggestions);
    }

    #endregion

    /// <summary>
    /// Test implementation of ICommand for unit testing.
    /// </summary>
    private sealed class TestCommand : ICommand
    {
        private readonly HashSet<string> _subcommands;

        public string Name { get; }
        public string Description => $"Test command: {Name}";
        public string Usage => $"{Name} <subcommand>";
        public IReadOnlySet<string> Subcommands => _subcommands;

        public TestCommand(string name, IEnumerable<string> subcommands)
        {
            Name = name;
            _subcommands = new HashSet<string>(subcommands, StringComparer.OrdinalIgnoreCase);
        }

        public Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(CommandResult.Ok());
    }
}
