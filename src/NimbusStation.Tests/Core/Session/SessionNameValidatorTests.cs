using NimbusStation.Core.Session;

namespace NimbusStation.Tests.Core.Session;

public sealed class SessionNameValidatorTests
{
    [Theory]
    [InlineData("SUP-123")]
    [InlineData("my-ticket")]
    [InlineData("2025-01-15")]
    [InlineData("ticket_with_underscore")]
    [InlineData("ALLCAPS")]
    [InlineData("123456")]
    [InlineData("a")]
    public void IsValid_ValidNames_ReturnsTrue(string name)
    {
        var result = SessionNameValidator.IsValid(name, out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Theory]
    [InlineData(null, "empty")]
    [InlineData("", "empty")]
    [InlineData("   ", "empty")]
    public void IsValid_EmptyOrNull_ReturnsFalse(string? name, string expectedContains)
    {
        var result = SessionNameValidator.IsValid(name, out var errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains(expectedContains, errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    [InlineData(" both ")]
    public void IsValid_LeadingOrTrailingWhitespace_ReturnsFalse(string name)
    {
        var result = SessionNameValidator.IsValid(name, out var errorMessage);

        Assert.False(result);
        Assert.Contains("whitespace", errorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(".hidden")]
    [InlineData("trailing.")]
    public void IsValid_LeadingOrTrailingDot_ReturnsFalse(string name)
    {
        var result = SessionNameValidator.IsValid(name, out var errorMessage);

        Assert.False(result);
        Assert.Contains("dot", errorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("path/separator")]
    [InlineData("path\\separator")]
    [InlineData("has<angle")]
    [InlineData("has>angle")]
    [InlineData("has:colon")]
    [InlineData("has\"quote")]
    [InlineData("has|pipe")]
    [InlineData("has?question")]
    [InlineData("has*star")]
    public void IsValid_InvalidCharacters_ReturnsFalse(string name)
    {
        var result = SessionNameValidator.IsValid(name, out var errorMessage);

        Assert.False(result);
        Assert.Contains("cannot contain", errorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("COM9")]
    [InlineData("LPT1")]
    [InlineData("LPT9")]
    [InlineData("CON.txt")]
    [InlineData("nul.log")]
    public void IsValid_WindowsReservedNames_ReturnsFalse(string name)
    {
        var result = SessionNameValidator.IsValid(name, out var errorMessage);

        Assert.False(result);
        Assert.Contains("reserved", errorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsValid_TooLong_ReturnsFalse()
    {
        var name = new string('a', 256);

        var result = SessionNameValidator.IsValid(name, out var errorMessage);

        Assert.False(result);
        Assert.Contains("255", errorMessage!);
    }

    [Fact]
    public void IsValid_MaxLength_ReturnsTrue()
    {
        var name = new string('a', 255);

        var result = SessionNameValidator.IsValid(name, out var errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void Validate_ValidName_DoesNotThrow()
    {
        var exception = Record.Exception(() => SessionNameValidator.Validate("SUP-123"));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_InvalidName_ThrowsInvalidSessionNameException()
    {
        var exception = Assert.Throws<InvalidSessionNameException>(
            () => SessionNameValidator.Validate("invalid/name"));

        Assert.Equal("invalid/name", exception.SessionName);
        Assert.Contains("/", exception.Message);
    }
}
