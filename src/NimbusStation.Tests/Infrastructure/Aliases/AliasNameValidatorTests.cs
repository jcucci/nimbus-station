using NimbusStation.Infrastructure.Aliases;

namespace NimbusStation.Tests.Infrastructure.Aliases;

public sealed class AliasNameValidatorTests
{
    [Theory]
    [InlineData("cq")]
    [InlineData("prod-users")]
    [InlineData("my_alias")]
    [InlineData("a1")]
    [InlineData("test123")]
    [InlineData("CamelCase")]
    [InlineData("with-hyphen-and_underscore")]
    public void Validate_ValidNames_ReturnsValid(string name)
    {
        var result = AliasNameValidator.Validate(name);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespace_ReturnsInvalid(string name)
    {
        var result = AliasNameValidator.Validate(name);

        Assert.False(result.IsValid);
        Assert.Contains("cannot be empty", result.ErrorMessage);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1abc")]
    [InlineData("-start")]
    [InlineData("_underscore")]
    public void Validate_InvalidStartCharacter_ReturnsInvalid(string name)
    {
        var result = AliasNameValidator.Validate(name);

        Assert.False(result.IsValid);
        Assert.Contains("must start with a letter", result.ErrorMessage);
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("special!char")]
    [InlineData("path/slash")]
    [InlineData("dot.name")]
    [InlineData("at@sign")]
    public void Validate_InvalidCharacters_ReturnsInvalid(string name)
    {
        var result = AliasNameValidator.Validate(name);

        Assert.False(result.IsValid);
        Assert.Contains("only letters, numbers, hyphens, and underscores", result.ErrorMessage);
    }

    [Theory]
    [InlineData("session")]
    [InlineData("SESSION")]
    [InlineData("Session")]
    [InlineData("alias")]
    [InlineData("help")]
    [InlineData("exit")]
    [InlineData("quit")]
    public void Validate_ReservedNames_ReturnsInvalid(string name)
    {
        var result = AliasNameValidator.Validate(name);

        Assert.False(result.IsValid);
        Assert.Contains("reserved command name", result.ErrorMessage);
    }

    [Theory]
    [InlineData("session")]
    [InlineData("SESSION")]
    [InlineData("alias")]
    [InlineData("help")]
    [InlineData("exit")]
    [InlineData("quit")]
    public void IsReserved_ReservedNames_ReturnsTrue(string name)
    {
        Assert.True(AliasNameValidator.IsReserved(name));
    }

    [Theory]
    [InlineData("cq")]
    [InlineData("my-alias")]
    [InlineData("custom-cmd")]
    [InlineData("helper")]
    public void IsReserved_NonReservedNames_ReturnsFalse(string name)
    {
        Assert.False(AliasNameValidator.IsReserved(name));
    }
}
