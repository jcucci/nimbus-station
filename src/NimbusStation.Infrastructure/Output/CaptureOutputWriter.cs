using System.Text;
using NimbusStation.Core.Output;

namespace NimbusStation.Infrastructure.Output;

/// <summary>
/// Output writer implementation that captures all output to a StringBuilder.
/// Useful for testing and for capturing command output to pipe to external processes.
/// </summary>
public sealed class CaptureOutputWriter : IOutputWriter
{
    private readonly StringBuilder _builder = new();

    /// <inheritdoc/>
    public bool SupportsFormatting => false;

    /// <inheritdoc/>
    public void WriteLine(string text) => _builder.AppendLine(MarkupStripper.Strip(text));

    /// <inheritdoc/>
    public void Write(string text) => _builder.Append(MarkupStripper.Strip(text));

    /// <inheritdoc/>
    public void WriteRenderable(object renderable)
    {
        // For renderables, just write the ToString representation
        // Tables and panels won't render properly without a console
        _builder.AppendLine(renderable?.ToString() ?? string.Empty);
    }

    /// <inheritdoc/>
    public void WriteRaw(ReadOnlySpan<byte> data) => _builder.Append(Encoding.UTF8.GetString(data));

    /// <inheritdoc/>
    public void Flush()
    {
        // No-op for StringBuilder capture
    }

    /// <summary>
    /// Gets the captured output as a string.
    /// </summary>
    /// <returns>The captured output.</returns>
    public string GetOutput() => _builder.ToString();

    /// <summary>
    /// Gets the captured output as a byte array (UTF-8 encoded).
    /// </summary>
    /// <returns>The captured output as bytes.</returns>
    public byte[] GetOutputBytes() => Encoding.UTF8.GetBytes(_builder.ToString());

    /// <summary>
    /// Clears the captured output buffer.
    /// </summary>
    public void Clear() => _builder.Clear();
}
