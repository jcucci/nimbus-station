using NimbusStation.Cli.Output;
using Spectre.Console;

namespace NimbusStation.Tests.Cli.Output;

public sealed class SpectreColorHelperTests
{
    [Theory]
    [InlineData("#000000", 0, 0, 0)]
    [InlineData("#FFFFFF", 255, 255, 255)]
    [InlineData("#FF0000", 255, 0, 0)]
    [InlineData("#00FF00", 0, 255, 0)]
    [InlineData("#0000FF", 0, 0, 255)]
    [InlineData("#50FA7B", 80, 250, 123)]
    [InlineData("#abcdef", 171, 205, 239)]
    public void TryParseColor_ValidSixDigitHex_ReturnsCorrectColor(string input, byte r, byte g, byte b)
    {
        var result = SpectreColorHelper.TryParseColor(input);

        Assert.NotNull(result);
        Assert.Equal(r, result.Value.R);
        Assert.Equal(g, result.Value.G);
        Assert.Equal(b, result.Value.B);
    }

    [Theory]
    [InlineData("#000", 0, 0, 0)]
    [InlineData("#FFF", 255, 255, 255)]
    [InlineData("#F00", 255, 0, 0)]
    [InlineData("#0F0", 0, 255, 0)]
    [InlineData("#00F", 0, 0, 255)]
    [InlineData("#abc", 170, 187, 204)]
    public void TryParseColor_ValidThreeDigitHex_ExpandsAndReturnsCorrectColor(string input, byte r, byte g, byte b)
    {
        var result = SpectreColorHelper.TryParseColor(input);

        Assert.NotNull(result);
        Assert.Equal(r, result.Value.R);
        Assert.Equal(g, result.Value.G);
        Assert.Equal(b, result.Value.B);
    }

    [Theory]
    [InlineData("rgb(0,0,0)", 0, 0, 0)]
    [InlineData("rgb(255,255,255)", 255, 255, 255)]
    [InlineData("rgb(80,250,123)", 80, 250, 123)]
    [InlineData("RGB(100,150,200)", 100, 150, 200)]
    [InlineData("rgb( 50 , 100 , 150 )", 50, 100, 150)]
    public void TryParseColor_ValidRgb_ReturnsCorrectColor(string input, byte r, byte g, byte b)
    {
        var result = SpectreColorHelper.TryParseColor(input);

        Assert.NotNull(result);
        Assert.Equal(r, result.Value.R);
        Assert.Equal(g, result.Value.G);
        Assert.Equal(b, result.Value.B);
    }

    [Fact]
    public void TryParseColor_NamedColorGreen_ReturnsGreen()
    {
        var result = SpectreColorHelper.TryParseColor("green");

        Assert.NotNull(result);
        Assert.Equal(Color.Green, result.Value);
    }

    [Fact]
    public void TryParseColor_NamedColorRed_ReturnsRed()
    {
        var result = SpectreColorHelper.TryParseColor("red");

        Assert.NotNull(result);
        Assert.Equal(Color.Red, result.Value);
    }

    [Fact]
    public void TryParseColor_NamedColorCaseInsensitive_ReturnsColor()
    {
        var lower = SpectreColorHelper.TryParseColor("blue");
        var upper = SpectreColorHelper.TryParseColor("BLUE");
        var mixed = SpectreColorHelper.TryParseColor("Blue");

        Assert.NotNull(lower);
        Assert.NotNull(upper);
        Assert.NotNull(mixed);
        Assert.Equal(lower, upper);
        Assert.Equal(lower, mixed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParseColor_EmptyOrWhitespace_ReturnsNull(string? input)
    {
        var result = SpectreColorHelper.TryParseColor(input!);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("#")]
    [InlineData("#12")]
    [InlineData("#1234")]
    [InlineData("#GGGGGG")]
    [InlineData("rgb(256,0,0)")]
    [InlineData("rgb(0,0)")]
    [InlineData("notacolor")]
    [InlineData("invalid123")]
    public void TryParseColor_InvalidInput_ReturnsNull(string input)
    {
        var result = SpectreColorHelper.TryParseColor(input);

        Assert.Null(result);
    }

    [Fact]
    public void ParseColorOrDefault_ValidColor_ReturnsColor()
    {
        var result = SpectreColorHelper.ParseColorOrDefault("#FF0000", Color.White);

        Assert.Equal(255, result.R);
        Assert.Equal(0, result.G);
        Assert.Equal(0, result.B);
    }

    [Fact]
    public void ParseColorOrDefault_InvalidColor_ReturnsFallback()
    {
        var fallback = Color.Yellow;
        var result = SpectreColorHelper.ParseColorOrDefault("invalid", fallback);

        Assert.Equal(fallback, result);
    }

    [Fact]
    public void ParseColorOrDefault_EmptyString_ReturnsFallback()
    {
        var fallback = Color.Cyan;
        var result = SpectreColorHelper.ParseColorOrDefault("", fallback);

        Assert.Equal(fallback, result);
    }

    [Fact]
    public void IsValidColor_DelegatesToColorFormatValidator()
    {
        // Valid colors
        Assert.True(SpectreColorHelper.IsValidColor("#FF0000"));
        Assert.True(SpectreColorHelper.IsValidColor("rgb(255,0,0)"));
        Assert.True(SpectreColorHelper.IsValidColor("green"));

        // Invalid colors
        Assert.False(SpectreColorHelper.IsValidColor(""));
        Assert.False(SpectreColorHelper.IsValidColor("#GGG"));
        Assert.False(SpectreColorHelper.IsValidColor("[red]"));
    }
}
