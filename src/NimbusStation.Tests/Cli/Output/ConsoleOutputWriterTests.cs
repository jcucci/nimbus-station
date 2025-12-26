using NimbusStation.Cli.Output;
using Spectre.Console;
using Spectre.Console.Testing;

namespace NimbusStation.Tests.Cli.Output;

public sealed class ConsoleOutputWriterTests
{
    [Fact]
    public void WriteLine_SimpleText_WritesToConsole()
    {
        var console = new TestConsole();
        var writer = new ConsoleOutputWriter(console);

        writer.WriteLine("Hello World");

        Assert.Contains("Hello World", console.Output);
    }

    [Fact]
    public void Write_SimpleText_WritesToConsole()
    {
        var console = new TestConsole();
        var writer = new ConsoleOutputWriter(console);

        writer.Write("Hello");
        writer.Write(" World");

        Assert.Contains("Hello", console.Output);
        Assert.Contains("World", console.Output);
    }

    [Fact]
    public void WriteLine_WithMarkup_RendersFormatting()
    {
        var console = new TestConsole();
        var writer = new ConsoleOutputWriter(console);

        writer.WriteLine("[red]Error:[/] Something went wrong");

        // TestConsole captures ANSI codes, so we just verify the text is present
        Assert.Contains("Error:", console.Output);
        Assert.Contains("Something went wrong", console.Output);
    }

    [Fact]
    public void WriteRenderable_WithTable_WritesToConsole()
    {
        var console = new TestConsole();
        var writer = new ConsoleOutputWriter(console);
        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Value");
        table.AddRow("Test", "42");

        writer.WriteRenderable(table);

        Assert.Contains("Name", console.Output);
        Assert.Contains("Value", console.Output);
        Assert.Contains("Test", console.Output);
        Assert.Contains("42", console.Output);
    }

    [Fact]
    public void WriteRenderable_WithPanel_WritesToConsole()
    {
        var console = new TestConsole();
        var writer = new ConsoleOutputWriter(console);
        var panel = new Panel("Content") { Header = new PanelHeader("Title") };

        writer.WriteRenderable(panel);

        Assert.Contains("Title", console.Output);
        Assert.Contains("Content", console.Output);
    }

    [Fact]
    public void WriteRenderable_WithNonRenderable_WritesToString()
    {
        var console = new TestConsole();
        var writer = new ConsoleOutputWriter(console);
        var obj = new TestObject("Test", 42);

        writer.WriteRenderable(obj);

        Assert.Contains("Test", console.Output);
        Assert.Contains("42", console.Output);
    }

    private sealed record TestObject(string Name, int Value)
    {
        public override string ToString() => $"Name={Name}, Value={Value}";
    }

    [Fact]
    public void WriteRenderable_WithNull_DoesNotThrow()
    {
        var console = new TestConsole();
        var writer = new ConsoleOutputWriter(console);

        var exception = Record.Exception(() => writer.WriteRenderable(null));

        Assert.Null(exception);
    }

    [Fact]
    public void WriteRaw_WithBytes_WritesToConsole()
    {
        var console = new TestConsole();
        var writer = new ConsoleOutputWriter(console);

        writer.WriteRaw("Raw data"u8);

        Assert.Contains("Raw data", console.Output);
    }

    [Fact]
    public void SupportsFormatting_ReturnsTrue()
    {
        var console = new TestConsole();
        var writer = new ConsoleOutputWriter(console);

        Assert.True(writer.SupportsFormatting);
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        var console = new TestConsole();
        var writer = new ConsoleOutputWriter(console);
        writer.WriteLine("Test");

        var exception = Record.Exception(() => writer.Flush());

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithNullConsole_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ConsoleOutputWriter(null!));
    }

    [Fact]
    public void DefaultConstructor_UsesAnsiConsole()
    {
        // Just verify it doesn't throw - we can't easily test the actual console
        var exception = Record.Exception(() => new ConsoleOutputWriter());

        Assert.Null(exception);
    }
}
