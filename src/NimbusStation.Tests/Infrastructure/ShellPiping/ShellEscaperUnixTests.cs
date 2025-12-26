using NimbusStation.Infrastructure.ShellPiping;

namespace NimbusStation.Tests.Infrastructure.ShellPiping;

/// <summary>
/// Tests for Unix shell escaping via /bin/sh -c.
/// </summary>
public class ShellEscaperUnixTests
{
    [Fact]
    public void SimpleCommand_WrapsInSingleQuotes() =>
        Assert.Equal("'echo hello'", ShellEscaper.EscapeForUnixShell("echo hello"));

    [Fact]
    public void EmptyString_ReturnsEmptyQuotes() =>
        Assert.Equal("''", ShellEscaper.EscapeForUnixShell(""));

    [Fact]
    public void NullString_ReturnsEmptyQuotes() =>
        Assert.Equal("''", ShellEscaper.EscapeForUnixShell(null!));

    [Fact]
    public void EmbeddedSingleQuote_EscapesCorrectly() =>
        Assert.Equal(@"'echo '\''hello'\'''", ShellEscaper.EscapeForUnixShell("echo 'hello'"));

    [Fact]
    public void MultipleSingleQuotes_EscapesAll() =>
        Assert.Equal(@"'it'\''s a '\''test'\'''", ShellEscaper.EscapeForUnixShell("it's a 'test'"));

    [Fact]
    public void DoubleQuotes_PreservedLiterally() =>
        Assert.Equal("'echo \"hello\"'", ShellEscaper.EscapeForUnixShell("echo \"hello\""));

    [Fact]
    public void Semicolon_PreservedLiterally() =>
        Assert.Equal("'echo hello; rm -rf /'", ShellEscaper.EscapeForUnixShell("echo hello; rm -rf /"));

    [Fact]
    public void CommandSubstitution_PreservedLiterally() =>
        Assert.Equal("'echo $(whoami)'", ShellEscaper.EscapeForUnixShell("echo $(whoami)"));

    [Fact]
    public void BacktickSubstitution_PreservedLiterally() =>
        Assert.Equal("'echo `whoami`'", ShellEscaper.EscapeForUnixShell("echo `whoami`"));

    [Fact]
    public void VariableExpansion_PreservedLiterally() =>
        Assert.Equal("'echo $HOME'", ShellEscaper.EscapeForUnixShell("echo $HOME"));

    [Fact]
    public void Pipe_PreservedLiterally() =>
        Assert.Equal("'cat file | grep test'", ShellEscaper.EscapeForUnixShell("cat file | grep test"));

    [Fact]
    public void Ampersand_PreservedLiterally() =>
        Assert.Equal("'echo hello && echo world'", ShellEscaper.EscapeForUnixShell("echo hello && echo world"));

    [Fact]
    public void Newline_PreservedLiterally() =>
        Assert.Equal("'line1\nline2'", ShellEscaper.EscapeForUnixShell("line1\nline2"));

    [Fact]
    public void Backslash_PreservedLiterally() =>
        Assert.Equal("'echo \\n'", ShellEscaper.EscapeForUnixShell("echo \\n"));

    [Fact]
    public void Glob_PreservedLiterally() =>
        Assert.Equal("'ls *.txt'", ShellEscaper.EscapeForUnixShell("ls *.txt"));
}
