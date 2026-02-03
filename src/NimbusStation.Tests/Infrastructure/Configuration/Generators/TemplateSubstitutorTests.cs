using NimbusStation.Infrastructure.Configuration.Generators;

namespace NimbusStation.Tests.Infrastructure.Configuration.Generators;

public sealed class TemplateSubstitutorTests
{
    [Fact]
    public void Substitute_SimpleVariable_ReplacesCorrectly()
    {
        var context = new Dictionary<string, string> { ["name"] = "ninja" };

        var result = TemplateSubstitutor.Substitute("{name}-alias", context);

        Assert.Equal("ninja-alias", result);
    }

    [Fact]
    public void Substitute_MultipleVariables_ReplacesAll()
    {
        var context = new Dictionary<string, string>
        {
            ["kingdom"] = "ninja",
            ["backend"] = "invoices"
        };

        var result = TemplateSubstitutor.Substitute("{kingdom}-{backend}-data", context);

        Assert.Equal("ninja-invoices-data", result);
    }

    [Fact]
    public void Substitute_MissingVariable_ThrowsKeyNotFoundException()
    {
        var context = new Dictionary<string, string> { ["name"] = "ninja" };

        Assert.Throws<KeyNotFoundException>(() =>
            TemplateSubstitutor.Substitute("{name}-{missing}", context));
    }

    [Fact]
    public void TrySubstitute_MissingVariable_ReturnsNull()
    {
        var context = new Dictionary<string, string> { ["name"] = "ninja" };

        var result = TemplateSubstitutor.TrySubstitute("{name}-{missing}", context);

        Assert.Null(result);
    }

    [Fact]
    public void TrySubstitute_AllVariablesPresent_ReturnsExpandedString()
    {
        var context = new Dictionary<string, string>
        {
            ["kingdom"] = "ninja",
            ["backend"] = "invoices"
        };

        var result = TemplateSubstitutor.TrySubstitute("{kingdom}-{backend}", context);

        Assert.Equal("ninja-invoices", result);
    }

    [Fact]
    public void ExtractVariables_MultipleVariables_ReturnsDistinctList()
    {
        var variables = TemplateSubstitutor.ExtractVariables("{a}-{b}-{a}-{c}");

        Assert.Equal(3, variables.Count);
        Assert.Contains("a", variables);
        Assert.Contains("b", variables);
        Assert.Contains("c", variables);
    }

    [Fact]
    public void ExtractVariables_NoVariables_ReturnsEmptyList()
    {
        var variables = TemplateSubstitutor.ExtractVariables("no-variables-here");

        Assert.Empty(variables);
    }

    [Fact]
    public void HasVariables_WithVariables_ReturnsTrue()
    {
        Assert.True(TemplateSubstitutor.HasVariables("{name}"));
    }

    [Fact]
    public void HasVariables_NoVariables_ReturnsFalse()
    {
        Assert.False(TemplateSubstitutor.HasVariables("plain-string"));
    }

    [Fact]
    public void Substitute_EmptyTemplate_ReturnsEmpty()
    {
        var context = new Dictionary<string, string> { ["name"] = "ninja" };

        var result = TemplateSubstitutor.Substitute("", context);

        Assert.Equal("", result);
    }

    [Fact]
    public void Substitute_ComplexEndpoint_ExpandsCorrectly()
    {
        var context = new Dictionary<string, string>
        {
            ["abbrev"] = "ninja"
        };

        var result = TemplateSubstitutor.Substitute(
            "https://king-{abbrev}-sharp-be-cdb.documents.azure.com:443/",
            context);

        Assert.Equal("https://king-ninja-sharp-be-cdb.documents.azure.com:443/", result);
    }
}
