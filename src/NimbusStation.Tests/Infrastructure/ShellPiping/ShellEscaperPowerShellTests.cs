using NimbusStation.Infrastructure.ShellPiping;

namespace NimbusStation.Tests.Infrastructure.ShellPiping;

/// <summary>
/// Tests for PowerShell escaping via pwsh -Command.
/// </summary>
public class ShellEscaperPowerShellTests
{
    [Fact]
    public void SimpleCommand_WrapsInSingleQuotes() =>
        Assert.Equal("'echo hello'", ShellEscaper.EscapeForPowerShell("echo hello"));

    [Fact]
    public void EmptyString_ReturnsEmptyQuotes() =>
        Assert.Equal("''", ShellEscaper.EscapeForPowerShell(""));

    [Fact]
    public void NullString_ReturnsEmptyQuotes() =>
        Assert.Equal("''", ShellEscaper.EscapeForPowerShell(null!));

    [Fact]
    public void EmbeddedSingleQuote_DoublesQuote() =>
        Assert.Equal("'echo ''hello'''", ShellEscaper.EscapeForPowerShell("echo 'hello'"));

    [Fact]
    public void MultipleSingleQuotes_DoublesAll() =>
        Assert.Equal("'it''s a ''test'''", ShellEscaper.EscapeForPowerShell("it's a 'test'"));

    [Fact]
    public void DoubleQuotes_PreservedLiterally() =>
        Assert.Equal("'echo \"hello\"'", ShellEscaper.EscapeForPowerShell("echo \"hello\""));

    [Fact]
    public void Semicolon_PreservedLiterally() =>
        Assert.Equal("'echo hello; Remove-Item /'", ShellEscaper.EscapeForPowerShell("echo hello; Remove-Item /"));

    [Fact]
    public void VariableExpansion_PreservedLiterally() =>
        Assert.Equal("'echo $env:HOME'", ShellEscaper.EscapeForPowerShell("echo $env:HOME"));

    [Fact]
    public void Pipe_PreservedLiterally() =>
        Assert.Equal("'Get-Content file | Select-String test'", ShellEscaper.EscapeForPowerShell("Get-Content file | Select-String test"));
}
