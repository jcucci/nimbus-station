using NimbusStation.Infrastructure.Configuration;

namespace NimbusStation.Tests.Infrastructure.Configuration;

public sealed class ColorFormatValidatorTests
{
    [Theory]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#50FA7B")]
    [InlineData("#abcdef")]
    [InlineData("#ABC123")]
    public void IsValid_ValidSixDigitHex_ReturnsTrue(string color)
    {
        Assert.True(ColorFormatValidator.IsValid(color));
    }

    [Theory]
    [InlineData("#000")]
    [InlineData("#FFF")]
    [InlineData("#abc")]
    [InlineData("#F0A")]
    public void IsValid_ValidThreeDigitHex_ReturnsTrue(string color)
    {
        Assert.True(ColorFormatValidator.IsValid(color));
    }

    [Theory]
    [InlineData("#")]
    [InlineData("#1")]
    [InlineData("#12")]
    [InlineData("#1234")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#GGGGGG")]
    [InlineData("#XYZ")]
    [InlineData("# 000000")]
    public void IsValid_InvalidHex_ReturnsFalse(string color)
    {
        Assert.False(ColorFormatValidator.IsValid(color));
    }

    [Theory]
    [InlineData("rgb(0,0,0)")]
    [InlineData("rgb(255,255,255)")]
    [InlineData("rgb(80,250,123)")]
    [InlineData("RGB(100,150,200)")]
    [InlineData("Rgb(0, 0, 0)")]
    [InlineData("rgb( 100 , 150 , 200 )")]
    public void IsValid_ValidRgb_ReturnsTrue(string color)
    {
        Assert.True(ColorFormatValidator.IsValid(color));
    }

    [Theory]
    [InlineData("rgb(256,0,0)")]
    [InlineData("rgb(-1,0,0)")]
    [InlineData("rgb(0,0)")]
    [InlineData("rgb(0,0,0,0)")]
    [InlineData("rgb()")]
    [InlineData("rgb(a,b,c)")]
    [InlineData("rgb(0,0,0")]
    [InlineData("rgb0,0,0)")]
    public void IsValid_InvalidRgb_ReturnsFalse(string color)
    {
        Assert.False(ColorFormatValidator.IsValid(color));
    }

    [Theory]
    [InlineData("green")]
    [InlineData("red")]
    [InlineData("blue")]
    [InlineData("cyan")]
    [InlineData("magenta")]
    [InlineData("yellow")]
    [InlineData("white")]
    [InlineData("black")]
    [InlineData("grey")]
    [InlineData("orange1")]
    [InlineData("cyan1")]
    [InlineData("Green")]
    [InlineData("GREEN")]
    public void IsValid_ValidNamedColor_ReturnsTrue(string color)
    {
        Assert.True(ColorFormatValidator.IsValid(color));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValid_EmptyOrWhitespace_ReturnsFalse(string? color)
    {
        Assert.False(ColorFormatValidator.IsValid(color!));
    }

    [Theory]
    [InlineData("green-dark")]
    [InlineData("light_blue")]
    [InlineData("red!")]
    [InlineData("color with space")]
    [InlineData("[red]")]
    [InlineData("red[/]")]
    [InlineData("[[red]]")]
    public void IsValid_InvalidNamedColor_ReturnsFalse(string color)
    {
        Assert.False(ColorFormatValidator.IsValid(color));
    }

    [Fact]
    public void IsValid_MarkupInjectionAttempts_ReturnsFalse()
    {
        // Verify that characters that could cause markup injection are rejected
        Assert.False(ColorFormatValidator.IsValid("[red]"));
        Assert.False(ColorFormatValidator.IsValid("red]"));
        Assert.False(ColorFormatValidator.IsValid("["));
        Assert.False(ColorFormatValidator.IsValid("]"));
        Assert.False(ColorFormatValidator.IsValid("color[bold]"));
    }
}
