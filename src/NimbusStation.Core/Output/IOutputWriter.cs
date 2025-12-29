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
    /// When <paramref name="renderable"/> is <c>null</c>, implementations must not throw;
    /// they should treat it as "no renderable content" (e.g., no output or an empty line).
    /// </summary>
    /// <param name="renderable">
    /// The renderable object to write. May be <c>null</c> to indicate no renderable content.
    /// </param>
    void WriteRenderable(object? renderable);

    /// <summary>
    /// Writes raw data to the output, bypassing any markup or higher-level text formatting.
    /// Stream-based writers write the bytes directly to the underlying stream.
    /// Console-like writers that cannot emit arbitrary binary data may interpret the bytes
    /// as UTF-8 encoded text and write the resulting characters without additional formatting.
    /// </summary>
    /// <param name="data">
    /// The raw bytes to write. Callers must not rely on strict binary preservation when targeting
    /// console-like writers, which may decode the data as UTF-8 text before output.
    /// </param>
    void WriteRaw(ReadOnlySpan<byte> data);

    /// <summary>
    /// Writes text to the error output stream without a trailing newline.
    /// </summary>
    /// <param name="text">The text to write.</param>
    void WriteError(string text);

    /// <summary>
    /// Writes text followed by a newline to the error output stream.
    /// </summary>
    /// <param name="text">The text to write.</param>
    void WriteErrorLine(string text);

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
