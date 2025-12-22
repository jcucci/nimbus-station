using NimbusStation.Core.Parsing;

namespace NimbusStation.Tests.Core.Parsing;

public sealed class InputTokenizerTests
{
    #region Basic Tokenization

    [Fact]
    public void Tokenize_EmptyString_ReturnsEmpty() =>
        Assert.Empty(InputTokenizer.Tokenize(""));

    [Fact]
    public void Tokenize_Null_ReturnsEmpty() =>
        Assert.Empty(InputTokenizer.Tokenize(null));

    [Fact]
    public void Tokenize_WhitespaceOnly_ReturnsEmpty() =>
        Assert.Empty(InputTokenizer.Tokenize("   \t  "));

    [Fact]
    public void Tokenize_SingleWord_ReturnsSingleToken()
    {
        var tokens = InputTokenizer.Tokenize("hello");

        Assert.Single(tokens);
        Assert.Equal("hello", tokens[0]);
    }

    [Fact]
    public void Tokenize_MultipleWords_ReturnsMultipleTokens()
    {
        var tokens = InputTokenizer.Tokenize("one two three");

        Assert.Equal(3, tokens.Length);
        Assert.Equal(["one", "two", "three"], tokens);
    }

    [Fact]
    public void Tokenize_MultipleConsecutiveSpaces_TreatsAsSingleDelimiter()
    {
        var tokens = InputTokenizer.Tokenize("one    two     three");

        Assert.Equal(3, tokens.Length);
        Assert.Equal(["one", "two", "three"], tokens);
    }

    #endregion

    #region Quoted Strings - Quotes Stripped (default)

    [Fact]
    public void Tokenize_DoubleQuotedString_StripsQuotes()
    {
        var tokens = InputTokenizer.Tokenize("hello \"world test\" end");

        Assert.Equal(3, tokens.Length);
        Assert.Equal("world test", tokens[1]);
    }

    [Fact]
    public void Tokenize_SingleQuotedString_StripsQuotes()
    {
        var tokens = InputTokenizer.Tokenize("hello 'world test' end");

        Assert.Equal(3, tokens.Length);
        Assert.Equal("world test", tokens[1]);
    }

    [Fact]
    public void Tokenize_EmptyQuotedString_OmitsEmptyToken()
    {
        // Empty quoted strings don't produce a token (no content)
        var tokens = InputTokenizer.Tokenize("before \"\" after");

        Assert.Equal(2, tokens.Length);
        Assert.Equal("before", tokens[0]);
        Assert.Equal("after", tokens[1]);
    }

    [Fact]
    public void Tokenize_MixedQuoteTypes_HandlesEachSeparately()
    {
        var tokens = InputTokenizer.Tokenize("\"double quoted\" 'single quoted'");

        Assert.Equal(2, tokens.Length);
        Assert.Equal("double quoted", tokens[0]);
        Assert.Equal("single quoted", tokens[1]);
    }

    [Fact]
    public void Tokenize_SingleQuoteInsideDoubleQuotes_Preserved()
    {
        var tokens = InputTokenizer.Tokenize("\"it's a test\"");

        Assert.Single(tokens);
        Assert.Equal("it's a test", tokens[0]);
    }

    [Fact]
    public void Tokenize_DoubleQuoteInsideSingleQuotes_Preserved()
    {
        var tokens = InputTokenizer.Tokenize("'say \"hello\"'");

        Assert.Single(tokens);
        Assert.Equal("say \"hello\"", tokens[0]);
    }

    #endregion

    #region Quoted Strings - Quotes Preserved

    [Fact]
    public void Tokenize_PreserveQuotes_KeepsDoubleQuotes()
    {
        var tokens = InputTokenizer.Tokenize("hello \"world test\" end", preserveQuotes: true);

        Assert.Equal(3, tokens.Length);
        Assert.Equal("\"world test\"", tokens[1]);
    }

    [Fact]
    public void Tokenize_PreserveQuotes_KeepsSingleQuotes()
    {
        var tokens = InputTokenizer.Tokenize("hello 'world test' end", preserveQuotes: true);

        Assert.Equal(3, tokens.Length);
        Assert.Equal("'world test'", tokens[1]);
    }

    #endregion

    #region Escape Sequences

    [Fact]
    public void Tokenize_EscapedSpace_IncludedInToken()
    {
        var tokens = InputTokenizer.Tokenize("hello\\ world");

        Assert.Single(tokens);
        Assert.Equal("hello world", tokens[0]);
    }

    [Fact]
    public void Tokenize_EscapedQuote_TreatedAsQuoteStart()
    {
        // Escaped quotes at start of word become quote delimiters after stripping backslash
        var tokens = InputTokenizer.Tokenize("say \\\"hello\\\"");

        Assert.Equal(2, tokens.Length);
        Assert.Equal("say", tokens[0]);
        Assert.Equal("\"hello\"", tokens[1]);
    }

    [Fact]
    public void Tokenize_EscapedBackslash_IncludedInToken()
    {
        var tokens = InputTokenizer.Tokenize("path\\\\to\\\\file");

        Assert.Single(tokens);
        Assert.Equal("path\\to\\file", tokens[0]);
    }

    [Fact]
    public void Tokenize_TrailingBackslash_PreservedAsBackslash()
    {
        var tokens = InputTokenizer.Tokenize("test\\");

        Assert.Single(tokens);
        Assert.Equal("test\\", tokens[0]);
    }

    [Fact]
    public void Tokenize_EscapeInsideQuotes_Handled()
    {
        var tokens = InputTokenizer.Tokenize("\"hello\\\"world\"");

        Assert.Single(tokens);
        Assert.Equal("hello\"world", tokens[0]);
    }

    #endregion

    #region Unclosed Quotes

    [Fact]
    public void Tokenize_UnclosedDoubleQuote_TreatedAsQuotedToEnd()
    {
        var tokens = InputTokenizer.Tokenize("hello \"unclosed");

        Assert.Equal(2, tokens.Length);
        Assert.Equal("hello", tokens[0]);
        Assert.Equal("unclosed", tokens[1]);
    }

    [Fact]
    public void Tokenize_UnclosedSingleQuote_TreatedAsQuotedToEnd()
    {
        var tokens = InputTokenizer.Tokenize("hello 'unclosed");

        Assert.Equal(2, tokens.Length);
        Assert.Equal("hello", tokens[0]);
        Assert.Equal("unclosed", tokens[1]);
    }

    #endregion

    #region Helper Methods

    [Fact]
    public void GetCommandName_EmptyTokens_ReturnsNull() =>
        Assert.Null(InputTokenizer.GetCommandName([]));

    [Fact]
    public void GetCommandName_SingleToken_ReturnsToken() =>
        Assert.Equal("command", InputTokenizer.GetCommandName(["command"]));

    [Fact]
    public void GetCommandName_MultipleTokens_ReturnsFirst() =>
        Assert.Equal("command", InputTokenizer.GetCommandName(["command", "arg1", "arg2"]));

    [Fact]
    public void GetArguments_EmptyTokens_ReturnsEmpty() =>
        Assert.Empty(InputTokenizer.GetArguments([]));

    [Fact]
    public void GetArguments_SingleToken_ReturnsEmpty() =>
        Assert.Empty(InputTokenizer.GetArguments(["command"]));

    [Fact]
    public void GetArguments_MultipleTokens_ReturnsAllButFirst() =>
        Assert.Equal(["arg1", "arg2"], InputTokenizer.GetArguments(["command", "arg1", "arg2"]));

    #endregion

    #region Real-World Examples

    [Fact]
    public void Tokenize_SqlQuery_ParsesCorrectly()
    {
        var tokens = InputTokenizer.Tokenize("azure cosmos query @prod \"SELECT * FROM c WHERE c.id = 'abc'\"");

        Assert.Equal(5, tokens.Length);
        Assert.Equal("azure", tokens[0]);
        Assert.Equal("cosmos", tokens[1]);
        Assert.Equal("query", tokens[2]);
        Assert.Equal("@prod", tokens[3]);
        Assert.Equal("SELECT * FROM c WHERE c.id = 'abc'", tokens[4]);
    }

    [Fact]
    public void Tokenize_CommandWithResourceAlias_PreservesAtSymbol()
    {
        var tokens = InputTokenizer.Tokenize("cq @prod-users @staging-data");

        Assert.Equal(3, tokens.Length);
        Assert.Equal("cq", tokens[0]);
        Assert.Equal("@prod-users", tokens[1]);
        Assert.Equal("@staging-data", tokens[2]);
    }

    #endregion
}
