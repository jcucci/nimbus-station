using NimbusStation.Core.Suggestions;

namespace NimbusStation.Tests.Core.Suggestions;

public class LevenshteinDistanceTests
{
    [Theory]
    [InlineData("", "", 0)]
    [InlineData("abc", "abc", 0)]
    [InlineData("abc", "ABC", 0)] // Case insensitive by default
    [InlineData("", "abc", 3)]
    [InlineData("abc", "", 3)]
    public void Compute_ExactMatches_ReturnsZero(string source, string target, int expected)
    {
        var result = LevenshteinDistance.Compute(source, target);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("saturday", "sunday", 3)]
    [InlineData("cosmos", "cosmo", 1)]
    [InlineData("blob", "blobs", 1)]
    [InlineData("session", "sesssion", 1)]
    public void Compute_KnownDistances_ReturnsCorrectValue(string source, string target, int expected)
    {
        var result = LevenshteinDistance.Compute(source, target);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("abc", "ABC", true, 0)]
    [InlineData("abc", "ABC", false, 3)]
    [InlineData("Cosmos", "cosmos", true, 0)]
    [InlineData("Cosmos", "cosmos", false, 1)]
    public void Compute_CaseSensitivity_RespectsFlag(string source, string target, bool ignoreCase, int expected)
    {
        var result = LevenshteinDistance.Compute(source, target, ignoreCase);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Compute_SingleCharacterDifference_ReturnsOne()
    {
        // Insertion
        Assert.Equal(1, LevenshteinDistance.Compute("cat", "cart"));
        
        // Deletion
        Assert.Equal(1, LevenshteinDistance.Compute("cart", "cat"));
        
        // Substitution
        Assert.Equal(1, LevenshteinDistance.Compute("cat", "cut"));
    }

    [Fact]
    public void Compute_NullOrEmpty_HandlesGracefully()
    {
        Assert.Equal(0, LevenshteinDistance.Compute(null!, null!));
        Assert.Equal(0, LevenshteinDistance.Compute("", ""));
        Assert.Equal(3, LevenshteinDistance.Compute(null!, "abc"));
        Assert.Equal(3, LevenshteinDistance.Compute("abc", null!));
    }
}
