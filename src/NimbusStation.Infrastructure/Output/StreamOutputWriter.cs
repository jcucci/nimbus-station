using System.Text;
using NimbusStation.Core.Output;

namespace NimbusStation.Infrastructure.Output;

/// <summary>
/// Output writer implementation that writes to a stream.
/// Used for piping command output to external processes via their stdin.
/// </summary>
public sealed class StreamOutputWriter : IOutputWriter, IDisposable
{
    private readonly Stream _stream;
    private readonly StreamWriter _writer;
    private readonly bool _ownsStream;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamOutputWriter"/> class.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="ownsStream">Whether this writer owns and should dispose the stream.</param>
    public StreamOutputWriter(Stream stream, bool ownsStream = false)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _ownsStream = ownsStream;
        _writer = new StreamWriter(_stream, Encoding.UTF8, leaveOpen: !ownsStream)
        {
            AutoFlush = false
        };
    }

    /// <inheritdoc/>
    public bool SupportsFormatting => false;

    /// <inheritdoc/>
    public void WriteLine(string text)
    {
        ThrowIfDisposed();
        _writer.WriteLine(MarkupStripper.Strip(text));
    }

    /// <inheritdoc/>
    public void Write(string text)
    {
        ThrowIfDisposed();
        _writer.Write(MarkupStripper.Strip(text));
    }

    /// <inheritdoc/>
    public void WriteRenderable(object renderable)
    {
        ThrowIfDisposed();
        _writer.WriteLine(renderable?.ToString() ?? string.Empty);
    }

    /// <inheritdoc/>
    public void WriteRaw(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();
        _writer.Flush(); // Flush any buffered text first
        _stream.Write(data);
    }

    /// <inheritdoc/>
    public void Flush()
    {
        ThrowIfDisposed();
        _writer.Flush();
        _stream.Flush();
    }

    /// <summary>
    /// Disposes the writer and optionally the underlying stream.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _writer.Dispose();

        if (_ownsStream)
            _stream.Dispose();

        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
