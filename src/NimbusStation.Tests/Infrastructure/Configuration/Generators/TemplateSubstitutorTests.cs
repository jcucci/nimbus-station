using NimbusStation.Infrastructure.Configuration.Generators;

namespace NimbusStation.Tests.Infrastructure.Configuration.Generators;

public sealed class TemplateSubstitutorTests
{
    [Fact]
    public void Substitute_SingleVariable_ReplacesCorrectly()
    {
        var template = "Hello, {name}!";
        var variables = new Dictionary<string, string> { ["name"] = "World" };

        var result = TemplateSubstitutor.Substitute(template, variables);

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Substitute_MultipleVariables_ReplacesAll()
    {
        var template = "{kingdom}-{backend}-{type}";
        var variables = new Dictionary<string, string>
        {
            ["kingdom"] = "ninja",
            ["backend"] = "activities",
            ["type"] = "event"
        };

        var result = TemplateSubstitutor.Substitute(template, variables);

        Assert.Equal("ninja-activities-event", result);
    }

    [Fact]
    public void Substitute_MissingVariable_LeavesPlaceholder()
    {
        var template = "{kingdom}-{backend}";
        var variables = new Dictionary<string, string> { ["kingdom"] = "ninja" };

        var result = TemplateSubstitutor.Substitute(template, variables);

        Assert.Equal("ninja-{backend}", result);
    }

    [Fact]
    public void Substitute_EmptyTemplate_ReturnsEmpty()
    {
        var result = TemplateSubstitutor.Substitute("", new Dictionary<string, string> { ["foo"] = "bar" });

        Assert.Equal("", result);
    }

    [Fact]
    public void Substitute_NullTemplate_ReturnsNull()
    {
        var result = TemplateSubstitutor.Substitute(null!, new Dictionary<string, string>());

        Assert.Null(result);
    }

    [Fact]
    public void Substitute_NoVariables_ReturnsTemplate()
    {
        var template = "no variables here";
        var variables = new Dictionary<string, string>();

        var result = TemplateSubstitutor.Substitute(template, variables);

        Assert.Equal("no variables here", result);
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
    public void TrySubstitute_MissingVariable_ReturnsNull()
    {
        var context = new Dictionary<string, string> { ["name"] = "ninja" };

        var result = TemplateSubstitutor.TrySubstitute("{name}-{missing}", context);

        Assert.Null(result);
    }

    [Fact]
    public void TrySubstitute_EmptyTemplate_ReturnsEmpty()
    {
        var context = new Dictionary<string, string> { ["name"] = "ninja" };

        var result = TemplateSubstitutor.TrySubstitute("", context);

        Assert.Equal("", result);
    }

    [Fact]
    public void HasUnresolvedVariables_WithUnresolved_ReturnsTrue()
    {
        Assert.True(TemplateSubstitutor.HasUnresolvedVariables("ninja-{backend}"));
    }

    [Fact]
    public void HasUnresolvedVariables_AllResolved_ReturnsFalse()
    {
        Assert.False(TemplateSubstitutor.HasUnresolvedVariables("ninja-activities"));
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
    public void GetVariables_ReturnsAllVariableNames()
    {
        var template = "{kingdom}-{backend}-{type}";

        var variables = TemplateSubstitutor.GetVariables(template);

        Assert.Equal(3, variables.Count);
        Assert.Contains("kingdom", variables);
        Assert.Contains("backend", variables);
        Assert.Contains("type", variables);
    }

    [Fact]
    public void GetVariables_DuplicateVariables_ReturnsDistinct()
    {
        var template = "{name}-{name}-{other}";

        var variables = TemplateSubstitutor.GetVariables(template);

        Assert.Equal(2, variables.Count);
        Assert.Contains("name", variables);
        Assert.Contains("other", variables);
    }

    [Fact]
    public void GetVariables_NoVariables_ReturnsEmpty()
    {
        var variables = TemplateSubstitutor.GetVariables("no variables");

        Assert.Empty(variables);
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
}
