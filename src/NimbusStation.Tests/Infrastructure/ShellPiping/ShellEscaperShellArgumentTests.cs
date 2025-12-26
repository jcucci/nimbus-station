using NimbusStation.Infrastructure.ShellPiping;

namespace NimbusStation.Tests.Infrastructure.ShellPiping;

/// <summary>
/// Security-critical tests for ShellEscaper.EscapeForShellArgument.
/// This method is used by ShellDelegator for multi-pipe execution.
/// </summary>
public class ShellEscaperShellArgumentTests
{
    [Fact]
    public void SimpleCommand_WrapsInDoubleQuotes()
    {
        var result = ShellEscaper.EscapeForShellArgument("echo hello");
        Assert.StartsWith("\"", result);
        Assert.EndsWith("\"", result);
    }

    [Fact]
    public void PipelineCommand_PreservesPipes()
    {
        var result = ShellEscaper.EscapeForShellArgument("cat | head -5");
        Assert.Contains("|", result);
    }

    [Fact]
    public void NewlineCharacter_IsEscaped()
    {
        var result = ShellEscaper.EscapeForShellArgument("echo hello\necho world");

        if (PlatformHelper.IsWindows)
            Assert.Contains("`n", result);
        else
            Assert.Contains("\\n", result);

        Assert.DoesNotContain("\n", result);
    }

    [Fact]
    public void CarriageReturn_IsEscaped()
    {
        var result = ShellEscaper.EscapeForShellArgument("echo hello\recho world");

        if (PlatformHelper.IsWindows)
            Assert.Contains("`r", result);
        else
            Assert.Contains("\\r", result);

        Assert.DoesNotContain("\r", result);
    }

    [Fact]
    public void DoubleQuote_IsEscaped()
    {
        var result = ShellEscaper.EscapeForShellArgument("echo \"hello\"");

        if (PlatformHelper.IsWindows)
            Assert.Contains("`\"", result);
        else
            Assert.Contains("\\\"", result);
    }

    [Fact]
    public void Backtick_IsEscaped()
    {
        var result = ShellEscaper.EscapeForShellArgument("echo `whoami`");

        if (PlatformHelper.IsWindows)
            Assert.Contains("``", result);
        else
            Assert.Contains("\\`", result);
    }

    [Fact]
    public void DollarSign_IsEscapedOnUnix()
    {
        var result = ShellEscaper.EscapeForShellArgument("echo $HOME");

        if (!PlatformHelper.IsWindows)
            Assert.Contains("\\$", result);
    }

    [Fact]
    public void Backslash_IsEscapedOnUnix()
    {
        var result = ShellEscaper.EscapeForShellArgument("echo \\n");

        if (!PlatformHelper.IsWindows)
            Assert.Contains("\\\\", result);
    }

    [Fact]
    public void Tab_IsEscapedOnWindows()
    {
        var result = ShellEscaper.EscapeForShellArgument("echo hello\tworld");

        if (PlatformHelper.IsWindows)
        {
            Assert.Contains("`t", result);
            Assert.DoesNotContain("\t", result);
        }
    }

    [Fact]
    public void CommandInjectionAttempt_Semicolon_IsPreservedButQuoted()
    {
        var result = ShellEscaper.EscapeForShellArgument("cat file; rm -rf /");
        Assert.StartsWith("\"", result);
        Assert.EndsWith("\"", result);
        Assert.Contains(";", result);
    }

    [Fact]
    public void CommandInjectionAttempt_NewlineWithCommand_IsEscaped()
    {
        var result = ShellEscaper.EscapeForShellArgument("echo test\n; rm -rf /");
        Assert.DoesNotContain("\n", result);
    }

    [Fact]
    public void ComplexPipeline_IsProperlyEscaped()
    {
        var result = ShellEscaper.EscapeForShellArgument("jq -r '.name' | grep \"test\" | head -5");
        Assert.StartsWith("\"", result);
        Assert.EndsWith("\"", result);
        Assert.Contains("|", result);
    }
}
