namespace NimbusStation.Core.Output;

/// <summary>
/// Abstraction for command output, allowing commands to write output
/// without being coupled to a specific output destination (console, stream, capture).
/// </summary>
public interface IOutputWriter
{
    /// <summary>
    /// Writes a line of text to the output.
    /// </summary>
    /// <param name="text">The text to write (may contain markup for formatted writers).</param>
    void WriteLine(string text);

    /// <summary>
    /// Writes text to the output without a trailing newline.
    /// </summary>
    /// <param name="text">The text to write (may contain markup for formatted writers).</param>
    void Write(string text);

    /// <summary>
    /// Writes a blank line to the output.
    /// </summary>
    void WriteLine() => WriteLine(string.Empty);

    /// <summary>
    /// Writes a renderable object to the output.
    /// This is used for rich content like tables, panels, and other complex formatting.
    /// For non-console writers, implementations may render to plain text or ignore.
    /// </summary>
    /// <param name="renderable">The renderable object to write.</param>
    void WriteRenderable(object renderable);

    /// <summary>
    /// Writes raw data to the output stream (for binary or unformatted data).
    /// This bypasses any text formatting and writes directly to the underlying stream.
    /// </summary>
    /// <param name="data">The raw bytes to write.</param>
    void WriteRaw(ReadOnlySpan<byte> data);

    /// <summary>
    /// Flushes any buffered output to the underlying destination.
    /// </summary>
    void Flush();

    /// <summary>
    /// Gets a value indicating whether this writer supports rich/formatted output.
    /// When false, callers should provide plain text alternatives.
    /// </summary>
    bool SupportsFormatting { get; }
}
