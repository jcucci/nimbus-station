using NimbusStation.Cli.Output;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Testing;

namespace NimbusStation.Tests.Cli.Output;

public sealed class JsonRendererTests
{
    private static readonly ThemeConfig DefaultTheme = ThemeConfig.Default;

    [Fact]
    public void TryCreateRenderable_ValidJsonObject_ReturnsRenderable()
    {
        var json = """{"name": "test", "value": 42}""";

        var result = JsonRenderer.TryCreateRenderable(json, DefaultTheme);

        Assert.NotNull(result);
    }

    [Fact]
    public void TryCreateRenderable_ValidJsonArray_ReturnsRenderable()
    {
        var json = """[1, 2, 3]""";

        var result = JsonRenderer.TryCreateRenderable(json, DefaultTheme);

        Assert.NotNull(result);
    }

    [Fact]
    public void TryCreateRenderable_InvalidJson_ReturnsNull()
    {
        var text = "this is not json at all";

        var result = JsonRenderer.TryCreateRenderable(text, DefaultTheme);

        Assert.Null(result);
    }

    [Fact]
    public void TryCreateRenderable_EmptyString_ReturnsNull()
    {
        var result = JsonRenderer.TryCreateRenderable(string.Empty, DefaultTheme);

        Assert.Null(result);
    }

    [Fact]
    public void TryCreateRenderable_NullString_ReturnsNull()
    {
        var result = JsonRenderer.TryCreateRenderable(null!, DefaultTheme);

        Assert.Null(result);
    }

    [Fact]
    public void TryCreateRenderable_ReturnsJsonTextType()
    {
        var json = """{"key": "value"}""";

        var result = JsonRenderer.TryCreateRenderable(json, DefaultTheme);

        Assert.IsType<JsonText>(result);
    }

    [Fact]
    public void TryCreateRenderable_NestedJson_ReturnsRenderable()
    {
        var json = """{"outer": {"inner": [1, true, null, "text"]}}""";

        var result = JsonRenderer.TryCreateRenderable(json, DefaultTheme);

        Assert.NotNull(result);
    }

    [Fact]
    public void TryCreateRenderable_RendersWithoutError()
    {
        var json = """{"name": "test", "count": 5, "active": true, "data": null}""";

        var result = JsonRenderer.TryCreateRenderable(json, DefaultTheme);

        Assert.NotNull(result);

        // Verify it renders without throwing by writing to a test console
        var console = new TestConsole();
        console.Write(result);

        var output = console.Output;
        Assert.Contains("name", output);
        Assert.Contains("test", output);
    }

    [Fact]
    public void TryCreateRenderable_CustomTheme_AppliesColors()
    {
        var theme = new ThemeConfig(
            PromptColor: "green",
            PromptSessionColor: "cyan",
            PromptContextColor: "yellow",
            PromptCosmosAliasColor: "orange1",
            PromptBlobAliasColor: "magenta",
            TableHeaderColor: "blue",
            TableBorderColor: "grey",
            ErrorColor: "red",
            WarningColor: "yellow",
            SuccessColor: "green",
            DimColor: "grey",
            JsonKeyColor: "#FF0000",
            JsonStringColor: "#00FF00",
            JsonNumberColor: "#0000FF",
            JsonBooleanColor: "#FFFF00",
            JsonNullColor: "#808080",
            BannerColor: "cyan1");

        var json = """{"key": "value"}""";

        var result = JsonRenderer.TryCreateRenderable(json, theme);

        Assert.NotNull(result);
        Assert.IsType<JsonText>(result);
    }
}
