using NimbusStation.Core.Output;

namespace NimbusStation.Tests.Core.Output;

public sealed class MarkupStripperTests
{
    #region Basic Markup Stripping

    [Fact]
    public void Strip_NullInput_ReturnsNull()
    {
        var result = MarkupStripper.Strip(null!);

        Assert.Null(result);
    }

    [Fact]
    public void Strip_EmptyString_ReturnsEmpty()
    {
        var result = MarkupStripper.Strip(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Strip_NoMarkup_ReturnsOriginal()
    {
        var result = MarkupStripper.Strip("Hello World");

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Strip_SimpleColorTag_RemovesTag()
    {
        var result = MarkupStripper.Strip("[red]Error[/]");

        Assert.Equal("Error", result);
    }

    [Fact]
    public void Strip_BoldTag_RemovesTag()
    {
        var result = MarkupStripper.Strip("[bold]Important[/]");

        Assert.Equal("Important", result);
    }

    [Fact]
    public void Strip_DimTag_RemovesTag()
    {
        var result = MarkupStripper.Strip("[dim]Subtle text[/]");

        Assert.Equal("Subtle text", result);
    }

    #endregion

    #region Nested Tags

    [Fact]
    public void Strip_NestedTags_RemovesAllTags()
    {
        var result = MarkupStripper.Strip("[bold][red]Error message[/][/]");

        Assert.Equal("Error message", result);
    }

    [Fact]
    public void Strip_MultipleTagsInLine_RemovesAll()
    {
        var result = MarkupStripper.Strip("[green]Success:[/] [cyan]Operation completed[/]");

        Assert.Equal("Success: Operation completed", result);
    }

    #endregion

    #region Complex Markup

    [Fact]
    public void Strip_ColorWithModifier_RemovesTag()
    {
        var result = MarkupStripper.Strip("[orange1 bold]Warning[/]");

        Assert.Equal("Warning", result);
    }

    [Fact]
    public void Strip_HexColor_RemovesTag()
    {
        var result = MarkupStripper.Strip("[#ff5500]Custom color[/]");

        Assert.Equal("Custom color", result);
    }

    [Fact]
    public void Strip_LinkStyle_RemovesTag()
    {
        var result = MarkupStripper.Strip("[link=https://example.com]Click here[/]");

        Assert.Equal("Click here", result);
    }

    #endregion

    #region Escaped Brackets

    [Fact]
    public void Strip_EscapedOpenBracket_PreservesSingleBracket()
    {
        var result = MarkupStripper.Strip("Array[[0]]");

        Assert.Equal("Array[0]", result);
    }

    [Fact]
    public void Strip_EscapedCloseBracket_PreservesSingleBracket()
    {
        var result = MarkupStripper.Strip("Value is ]]100");

        Assert.Equal("Value is ]100", result);
    }

    [Fact]
    public void Strip_EscapedBracketsWithMarkup_HandlesCorrectly()
    {
        var result = MarkupStripper.Strip("[cyan]Array[[0]][/] = [green]42[/]");

        Assert.Equal("Array[0] = 42", result);
    }

    [Fact]
    public void Strip_MultipleEscapedBrackets_PreservesAll()
    {
        var result = MarkupStripper.Strip("[[test]] and [[another]]");

        Assert.Equal("[test] and [another]", result);
    }

    #endregion

    #region Real-World Examples

    [Fact]
    public void Strip_SessionPrompt_StripsCorrectly()
    {
        var result = MarkupStripper.Strip("[green]ns[/][[[cyan]TICKET-123[/]]]› ");

        Assert.Equal("ns[TICKET-123]› ", result);
    }

    [Fact]
    public void Strip_ErrorMessage_StripsCorrectly()
    {
        var result = MarkupStripper.Strip("[red]Error:[/] No active session. Use '[cyan]session start <ticket>[/]' first.");

        Assert.Equal("Error: No active session. Use 'session start <ticket>' first.", result);
    }

    [Fact]
    public void Strip_TableHeader_StripsCorrectly()
    {
        var result = MarkupStripper.Strip("[bold]Property[/]");

        Assert.Equal("Property", result);
    }

    [Fact]
    public void Strip_ContextOutput_StripsCorrectly()
    {
        var result = MarkupStripper.Strip("[green]Context set:[/] [orange1]cosmos/prod-main[/]");

        Assert.Equal("Context set: cosmos/prod-main", result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Strip_UnclosedTag_HandlesGracefully()
    {
        var result = MarkupStripper.Strip("[red]Unclosed tag");

        Assert.Equal("Unclosed tag", result);
    }

    [Fact]
    public void Strip_EmptyTag_HandlesGracefully()
    {
        var result = MarkupStripper.Strip("[]Empty[]");

        Assert.Equal("Empty", result);
    }

    [Fact]
    public void Strip_OnlyTags_ReturnsEmpty()
    {
        var result = MarkupStripper.Strip("[bold][red][/][/]");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Strip_ConsecutiveTags_RemovesAll()
    {
        var result = MarkupStripper.Strip("[/][bold][/][red][/]text[/]");

        Assert.Equal("text", result);
    }

    #endregion
}
