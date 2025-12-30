using NimbusStation.Core.Suggestions;

namespace NimbusStation.Tests.Core.Suggestions;

public class CommandSuggesterTests
{
    private readonly string[] _commands = ["cosmos", "blob", "session", "auth", "alias", "use", "info", "theme", "help", "exit"];

    [Theory]
    [InlineData("cosmo", "cosmos")]
    [InlineData("blop", "blob")]
    [InlineData("sesion", "session")]
    [InlineData("aliass", "alias")]
    public void GetSuggestions_TyposWithinThreshold_ReturnsSuggestion(string input, string expected)
    {
        var suggestions = CommandSuggester.GetSuggestions(input, _commands);
        
        Assert.NotEmpty(suggestions);
        Assert.Contains(expected, suggestions);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetSuggestions_EmptyOrWhitespace_ReturnsEmpty(string? input)
    {
        var suggestions = CommandSuggester.GetSuggestions(input!, _commands);
        
        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSuggestions_ExactMatch_ReturnsEmpty()
    {
        // Exact matches should not be suggested
        var suggestions = CommandSuggester.GetSuggestions("cosmos", _commands);
        
        Assert.DoesNotContain("cosmos", suggestions);
    }

    [Fact]
    public void GetSuggestions_TooFarFromAnyCommand_ReturnsEmpty()
    {
        var suggestions = CommandSuggester.GetSuggestions("xyz123", _commands);
        
        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSuggestions_MultipleSuggestions_SortedByDistance()
    {
        // "blo" is distance 1 from "blob" and distance 3 from "auth"
        var suggestions = CommandSuggester.GetSuggestions("blo", _commands);
        
        Assert.NotEmpty(suggestions);
        Assert.Equal("blob", suggestions[0]); // Closest match first
    }

    [Fact]
    public void GetBestSuggestion_ReturnsClosestMatch()
    {
        var suggestion = CommandSuggester.GetBestSuggestion("cosmo", _commands);
        
        Assert.Equal("cosmos", suggestion);
    }

    [Fact]
    public void GetBestSuggestion_NoMatch_ReturnsNull()
    {
        var suggestion = CommandSuggester.GetBestSuggestion("xyz123", _commands);
        
        Assert.Null(suggestion);
    }

    [Theory]
    [InlineData("cosmo", 1)]
    [InlineData("cosmo", 2)]
    [InlineData("cosmo", 3)]
    public void GetSuggestions_RespectsMaxDistance(string input, int maxDistance)
    {
        var suggestions = CommandSuggester.GetSuggestions(input, _commands, maxDistance);
        
        // "cosmo" to "cosmos" is distance 1, so should be included for all maxDistance >= 1
        Assert.Contains("cosmos", suggestions);
    }

    [Fact]
    public void GetSuggestions_MaxDistanceZero_ReturnsEmpty()
    {
        var suggestions = CommandSuggester.GetSuggestions("cosmo", _commands, maxDistance: 0);
        
        // Distance 0 means exact match only, but we exclude exact matches
        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSuggestions_CaseInsensitive_FindsMatches()
    {
        var suggestions = CommandSuggester.GetSuggestions("COSMO", _commands);
        
        Assert.Contains("cosmos", suggestions);
    }
}
