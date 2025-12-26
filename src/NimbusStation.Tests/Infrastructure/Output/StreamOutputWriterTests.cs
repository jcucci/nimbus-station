using System.Text;
using NimbusStation.Infrastructure.Output;

namespace NimbusStation.Tests.Infrastructure.Output;

public sealed class StreamOutputWriterTests
{
    [Fact]
    public void WriteLine_SimpleText_WritesWithNewline()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);

        writer.WriteLine("Hello World");
        writer.Flush();

        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Hello World" + Environment.NewLine, result);
    }

    [Fact]
    public void Write_SimpleText_WritesWithoutNewline()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);

        writer.Write("Hello");
        writer.Write(" World");
        writer.Flush();

        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void WriteLine_MultipleCalls_WritesAll()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);

        writer.WriteLine("Line 1");
        writer.WriteLine("Line 2");
        writer.WriteLine("Line 3");
        writer.Flush();

        var result = Encoding.UTF8.GetString(stream.ToArray());
        var expected = "Line 1" + Environment.NewLine +
                      "Line 2" + Environment.NewLine +
                      "Line 3" + Environment.NewLine;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteLine_WithMarkup_StripsMarkup()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);

        writer.WriteLine("[red]Error:[/] Something went wrong");
        writer.Flush();

        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Error: Something went wrong" + Environment.NewLine, result);
    }

    [Fact]
    public void Write_WithMarkup_StripsMarkup()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);

        writer.Write("[bold]Important[/]");
        writer.Flush();

        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Important", result);
    }

    [Fact]
    public void WriteLine_EscapedBrackets_PreservesBrackets()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);

        writer.WriteLine("[cyan]Array[[0]][/] = 42");
        writer.Flush();

        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Array[0] = 42" + Environment.NewLine, result);
    }

    [Fact]
    public void WriteRenderable_WithObject_WritesToString()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);
        var obj = new TestRenderable("Test", 42);

        writer.WriteRenderable(obj);
        writer.Flush();

        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("Test", result);
        Assert.Contains("42", result);
    }

    private sealed record TestRenderable(string Name, int Value)
    {
        public override string ToString() => $"Name={Name}, Value={Value}";
    }

    [Fact]
    public void WriteRenderable_WithNull_WritesNothing()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);

        writer.WriteRenderable(null);
        writer.Flush();

        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void WriteRaw_WithBytes_WritesDirectlyToStream()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);
        var bytes = "Raw data"u8.ToArray();

        writer.WriteRaw(bytes);

        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Raw data", result);
    }

    [Fact]
    public void WriteRaw_AfterWrite_FlushesTextFirst()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);

        writer.Write("Text ");
        writer.WriteRaw("Raw"u8);

        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Text Raw", result);
    }

    [Fact]
    public void WriteRaw_MultipleCalls_WritesAll()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);

        writer.WriteRaw("Hello "u8);
        writer.WriteRaw("World"u8);

        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void SupportsFormatting_ReturnsFalse()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);

        Assert.False(writer.SupportsFormatting);
    }

    [Fact]
    public void Dispose_WithOwnsStreamFalse_DoesNotDisposeStream()
    {
        var stream = new MemoryStream();
        var writer = new StreamOutputWriter(stream, ownsStream: false);

        writer.Dispose();

        // Stream should still be usable
        var exception = Record.Exception(() => stream.WriteByte(0));
        Assert.Null(exception);
        stream.Dispose();
    }

    [Fact]
    public void Dispose_WithOwnsStreamTrue_DisposesStream()
    {
        var stream = new MemoryStream();
        var writer = new StreamOutputWriter(stream, ownsStream: true);

        writer.Dispose();

        // Stream should be disposed
        Assert.Throws<ObjectDisposedException>(() => stream.WriteByte(0));
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        using var stream = new MemoryStream();
        var writer = new StreamOutputWriter(stream);

        writer.Dispose();
        var exception = Record.Exception(() => writer.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void WriteLine_AfterDispose_Throws()
    {
        using var stream = new MemoryStream();
        var writer = new StreamOutputWriter(stream);
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteLine("Test"));
    }

    [Fact]
    public void Write_AfterDispose_Throws()
    {
        using var stream = new MemoryStream();
        var writer = new StreamOutputWriter(stream);
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.Write("Test"));
    }

    [Fact]
    public void WriteRaw_AfterDispose_Throws()
    {
        using var stream = new MemoryStream();
        var writer = new StreamOutputWriter(stream);
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteRaw("Test"u8));
    }

    [Fact]
    public void Flush_AfterDispose_Throws()
    {
        using var stream = new MemoryStream();
        var writer = new StreamOutputWriter(stream);
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.Flush());
    }

    [Fact]
    public async Task DisposeAsync_WithOwnsStreamFalse_DoesNotDisposeStream()
    {
        var stream = new MemoryStream();
        var writer = new StreamOutputWriter(stream, ownsStream: false);

        await writer.DisposeAsync();

        // Stream should still be usable
        var exception = Record.Exception(() => stream.WriteByte(0));
        Assert.Null(exception);
        await stream.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WithOwnsStreamTrue_DisposesStream()
    {
        var stream = new MemoryStream();
        var writer = new StreamOutputWriter(stream, ownsStream: true);

        await writer.DisposeAsync();

        // Stream should be disposed
        Assert.Throws<ObjectDisposedException>(() => stream.WriteByte(0));
    }

    [Fact]
    public async Task DisposeAsync_MultipleCalls_DoesNotThrow()
    {
        await using var stream = new MemoryStream();
        var writer = new StreamOutputWriter(stream);

        await writer.DisposeAsync();
        var exception = await Record.ExceptionAsync(() => writer.DisposeAsync().AsTask());

        Assert.Null(exception);
    }

    [Fact]
    public void Flush_FlushesBufferedContent()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamOutputWriter(stream);

        writer.Write("Buffered");
        writer.Flush();

        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Buffered", result);
    }

    [Fact]
    public void Constructor_WithNullStream_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new StreamOutputWriter(null!));
    }
}
