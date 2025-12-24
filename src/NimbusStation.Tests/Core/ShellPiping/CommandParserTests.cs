using NimbusStation.Core.ShellPiping;

namespace NimbusStation.Tests.Core.ShellPiping;

public class CommandParserTests
{
    [Fact]
    public void Parse_SimpleCommand_ReturnsCommandOnly()
    {
        var (command, arguments) = CommandParser.Parse("jq");

        Assert.Equal("jq", command);
        Assert.Null(arguments);
    }

    [Fact]
    public void Parse_CommandWithArguments_SplitsCorrectly()
    {
        var (command, arguments) = CommandParser.Parse("jq .");

        Assert.Equal("jq", command);
        Assert.Equal(".", arguments);
    }

    [Fact]
    public void Parse_CommandWithQuotedArgument_PreservesQuotes()
    {
        var (command, arguments) = CommandParser.Parse("jq '.name'");

        Assert.Equal("jq", command);
        Assert.Equal("'.name'", arguments);
    }

    [Fact]
    public void Parse_CommandWithDoubleQuotedArgument_PreservesQuotes()
    {
        var (command, arguments) = CommandParser.Parse("grep \"hello world\"");

        Assert.Equal("grep", command);
        Assert.Equal("\"hello world\"", arguments);
    }

    [Fact]
    public void Parse_CommandWithMultipleArguments_ReturnsAllArguments()
    {
        var (command, arguments) = CommandParser.Parse("grep -i \"hello world\"");

        Assert.Equal("grep", command);
        Assert.Equal("-i \"hello world\"", arguments);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmptyCommand()
    {
        var (command, arguments) = CommandParser.Parse("");

        Assert.Equal("", command);
        Assert.Null(arguments);
    }

    [Fact]
    public void Parse_NullString_ReturnsEmptyCommand()
    {
        var (command, arguments) = CommandParser.Parse(null);

        Assert.Equal("", command);
        Assert.Null(arguments);
    }

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsEmptyCommand()
    {
        var (command, arguments) = CommandParser.Parse("   ");

        Assert.Equal("", command);
        Assert.Null(arguments);
    }

    [Fact]
    public void Parse_LeadingWhitespace_TrimsCorrectly()
    {
        var (command, arguments) = CommandParser.Parse("  cmd arg");

        Assert.Equal("cmd", command);
        Assert.Equal("arg", arguments);
    }

    [Fact]
    public void Parse_TrailingWhitespace_TrimsCorrectly()
    {
        var (command, arguments) = CommandParser.Parse("cmd arg  ");

        Assert.Equal("cmd", command);
        Assert.Equal("arg", arguments);
    }

    [Fact]
    public void Parse_MultipleSpacesBetweenArgs_PreservesInnerSpaces()
    {
        var (command, arguments) = CommandParser.Parse("cmd   arg1   arg2");

        Assert.Equal("cmd", command);
        Assert.Equal("arg1   arg2", arguments);
    }

    [Fact]
    public void Parse_CommandWithComplexJqFilter_PreservesFilter()
    {
        var (command, arguments) = CommandParser.Parse("jq '.[] | .id'");

        Assert.Equal("jq", command);
        Assert.Equal("'.[] | .id'", arguments);
    }

    [Fact]
    public void Parse_HeadWithNumericArg_SplitsCorrectly()
    {
        var (command, arguments) = CommandParser.Parse("head -5");

        Assert.Equal("head", command);
        Assert.Equal("-5", arguments);
    }

    [Fact]
    public void Parse_WcWithFlag_SplitsCorrectly()
    {
        var (command, arguments) = CommandParser.Parse("wc -l");

        Assert.Equal("wc", command);
        Assert.Equal("-l", arguments);
    }

    [Fact]
    public void Parse_UnclosedDoubleQuote_SplitsAtSpaceBeforeQuote()
    {
        // The space before the quote is found first, so it splits there
        var (command, arguments) = CommandParser.Parse("echo \"unclosed");

        Assert.Equal("echo", command);
        Assert.Equal("\"unclosed", arguments);
    }

    [Fact]
    public void Parse_UnclosedSingleQuote_SplitsAtSpaceBeforeQuote()
    {
        var (command, arguments) = CommandParser.Parse("jq '.name");

        Assert.Equal("jq", command);
        Assert.Equal("'.name", arguments);
    }

    [Fact]
    public void Parse_UnclosedQuoteWithNoSpaceBeforeQuote_TreatsEntireStringAsCommand()
    {
        // Quote starts in command name, no space found, so entire string is command
        var (command, arguments) = CommandParser.Parse("\"quoted-command arg");

        Assert.Equal("\"quoted-command arg", command);
        Assert.Null(arguments);
    }
}
