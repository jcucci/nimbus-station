using System.Text;
using NimbusStation.Infrastructure.Output;

namespace NimbusStation.Tests.Infrastructure.Output;

public sealed class CaptureOutputWriterTests
{
    #region Basic Output Tests

    [Fact]
    public void WriteLine_SimpleText_CapturesWithNewline()
    {
        var writer = new CaptureOutputWriter();

        writer.WriteLine("Hello World");

        Assert.Equal("Hello World" + Environment.NewLine, writer.GetOutput());
    }

    [Fact]
    public void Write_SimpleText_CapturesWithoutNewline()
    {
        var writer = new CaptureOutputWriter();

        writer.Write("Hello");
        writer.Write(" World");

        Assert.Equal("Hello World", writer.GetOutput());
    }

    [Fact]
    public void WriteLine_MultipleCalls_CapturesAll()
    {
        var writer = new CaptureOutputWriter();

        writer.WriteLine("Line 1");
        writer.WriteLine("Line 2");
        writer.WriteLine("Line 3");

        var expected = "Line 1" + Environment.NewLine +
                      "Line 2" + Environment.NewLine +
                      "Line 3" + Environment.NewLine;
        Assert.Equal(expected, writer.GetOutput());
    }

    #endregion

    #region Markup Stripping Tests

    [Fact]
    public void WriteLine_WithMarkup_StripsMarkup()
    {
        var writer = new CaptureOutputWriter();

        writer.WriteLine("[red]Error:[/] Something went wrong");

        Assert.Equal("Error: Something went wrong" + Environment.NewLine, writer.GetOutput());
    }

    [Fact]
    public void Write_WithMarkup_StripsMarkup()
    {
        var writer = new CaptureOutputWriter();

        writer.Write("[bold]Important[/]");

        Assert.Equal("Important", writer.GetOutput());
    }

    [Fact]
    public void WriteLine_ComplexMarkup_StripsAll()
    {
        var writer = new CaptureOutputWriter();

        writer.WriteLine("[green]Success:[/] [cyan]Operation completed[/] [dim](2.5s)[/]");

        Assert.Equal("Success: Operation completed (2.5s)" + Environment.NewLine, writer.GetOutput());
    }

    [Fact]
    public void WriteLine_EscapedBrackets_PreservesBrackets()
    {
        var writer = new CaptureOutputWriter();

        writer.WriteLine("[cyan]Array[[0]][/] = 42");

        Assert.Equal("Array[0] = 42" + Environment.NewLine, writer.GetOutput());
    }

    #endregion

    #region WriteRenderable Tests

    [Fact]
    public void WriteRenderable_WithObject_WritesToString()
    {
        var writer = new CaptureOutputWriter();
        var obj = new { Name = "Test", Value = 42 };

        writer.WriteRenderable(obj);

        Assert.Contains("Name", writer.GetOutput());
        Assert.Contains("Test", writer.GetOutput());
    }

    [Fact]
    public void WriteRenderable_WithNull_WritesEmptyLine()
    {
        var writer = new CaptureOutputWriter();

        writer.WriteRenderable(null!);

        Assert.Equal(Environment.NewLine, writer.GetOutput());
    }

    #endregion

    #region WriteRaw Tests

    [Fact]
    public void WriteRaw_WithBytes_CapturesAsUtf8()
    {
        var writer = new CaptureOutputWriter();
        var bytes = Encoding.UTF8.GetBytes("Raw data");

        writer.WriteRaw(bytes);

        Assert.Equal("Raw data", writer.GetOutput());
    }

    [Fact]
    public void WriteRaw_MultipleCalls_CapturesAll()
    {
        var writer = new CaptureOutputWriter();

        writer.WriteRaw("Hello "u8);
        writer.WriteRaw("World"u8);

        Assert.Equal("Hello World", writer.GetOutput());
    }

    #endregion

    #region GetOutputBytes Tests

    [Fact]
    public void GetOutputBytes_ReturnsUtf8Bytes()
    {
        var writer = new CaptureOutputWriter();
        writer.WriteLine("Test");

        var bytes = writer.GetOutputBytes();

        var expected = Encoding.UTF8.GetBytes("Test" + Environment.NewLine);
        Assert.Equal(expected, bytes);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_AfterWriting_ClearsBuffer()
    {
        var writer = new CaptureOutputWriter();
        writer.WriteLine("Initial content");

        writer.Clear();

        Assert.Equal(string.Empty, writer.GetOutput());
    }

    [Fact]
    public void Clear_ThenWrite_CapturesNewContent()
    {
        var writer = new CaptureOutputWriter();
        writer.WriteLine("Initial");
        writer.Clear();

        writer.WriteLine("New content");

        Assert.Equal("New content" + Environment.NewLine, writer.GetOutput());
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void SupportsFormatting_ReturnsFalse()
    {
        var writer = new CaptureOutputWriter();

        Assert.False(writer.SupportsFormatting);
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        var writer = new CaptureOutputWriter();
        writer.WriteLine("Test");

        var exception = Record.Exception(() => writer.Flush());

        Assert.Null(exception);
    }

    #endregion

    #region Mixed Operations Tests

    [Fact]
    public void MixedOperations_CapturesInOrder()
    {
        var writer = new CaptureOutputWriter();

        writer.Write("[bold]Name:[/] ");
        writer.WriteLine("[cyan]Test[/]");
        writer.WriteRaw("Raw"u8);
        writer.WriteLine("");
        writer.WriteLine("[dim]Done[/]");

        var expected = "Name: Test" + Environment.NewLine +
                      "Raw" + Environment.NewLine +
                      "Done" + Environment.NewLine;
        Assert.Equal(expected, writer.GetOutput());
    }

    #endregion
}
