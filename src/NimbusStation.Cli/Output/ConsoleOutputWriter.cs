using NimbusStation.Core.Output;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace NimbusStation.Cli.Output;

/// <summary>
/// Output writer implementation that writes to the console using Spectre.Console
/// for rich formatted output including markup, tables, and panels.
/// </summary>
public sealed class ConsoleOutputWriter : IOutputWriter
{
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleOutputWriter"/> class
    /// using the default <see cref="AnsiConsole"/>.
    /// </summary>
    public ConsoleOutputWriter() : this(AnsiConsole.Console)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleOutputWriter"/> class
    /// with a specific console instance (useful for testing).
    /// </summary>
    /// <param name="console">The Spectre.Console instance to write to.</param>
    public ConsoleOutputWriter(IAnsiConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <inheritdoc/>
    public bool SupportsFormatting => true;

    /// <inheritdoc/>
    public void WriteLine(string text) => _console.MarkupLine(text);

    /// <inheritdoc/>
    public void Write(string text) => _console.Markup(text);

    /// <inheritdoc/>
    public void WriteRenderable(object renderable)
    {
        if (renderable is IRenderable spectreRenderable)
            _console.Write(spectreRenderable);
        else
            _console.WriteLine(renderable?.ToString() ?? string.Empty);
    }

    /// <inheritdoc/>
    public void WriteRaw(ReadOnlySpan<byte> data)
    {
        // For console output, convert bytes to string and write as unformatted text
        // This is a fallback - binary data should typically go through StreamOutputWriter
        var text = System.Text.Encoding.UTF8.GetString(data);
        _console.Write(new Text(text));
    }

    /// <inheritdoc/>
    public void Flush()
    {
        // Console output is typically unbuffered, but flush stdout just in case
        Console.Out.Flush();
    }
}
