using System.Diagnostics;
using System.Text;
using NimbusStation.Core.ShellPiping;

namespace NimbusStation.Infrastructure.ShellPiping;

/// <summary>
/// Executes external processes with stdin streaming and output capture.
/// </summary>
/// <remarks>
/// <para>
/// This executor spawns processes directly without shell interpretation. The command
/// and arguments are passed directly to <see cref="ProcessStartInfo"/>.
/// </para>
/// <para>
/// <strong>Security note:</strong> This class does not perform any validation or
/// sanitization of command or argument inputs. Callers should validate inputs if
/// they originate from untrusted sources to prevent command injection attacks.
/// </para>
/// </remarks>
public sealed class ExternalProcessExecutor : IExternalProcessExecutor
{
    /// <inheritdoc/>
    public async Task<ProcessResult> ExecuteAsync(
        string command,
        string? arguments = null,
        string? stdinContent = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments ?? "",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        Process process;
        try
        {
            process = new Process { StartInfo = psi };
            if (!process.Start())
                return ProcessResult.StartupError($"Failed to start process: {command}");
        }
        catch (Exception ex)
        {
            return ProcessResult.StartupError($"Failed to start '{command}': {ex.Message}");
        }

        using (process)
        {
            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();
            var wasKilled = 0; // Use int for Interlocked operations

            // Set up cancellation to kill the process
            await using var registration = cancellationToken.Register(() =>
            {
                Interlocked.Exchange(ref wasKilled, 1);
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // Process has already exited
                }
            });

            try
            {
                // Run stdin writing, stdout reading, and stderr reading concurrently.
                // This prevents deadlocks when the process's output buffer fills up.
                var tasks = new List<Task>();

                // Write to stdin if we have content
                if (stdinContent is not null)
                {
                    tasks.Add(WriteStdinAsync(process, stdinContent, cancellationToken));
                }
                else
                {
                    // Close stdin immediately if we have no content to write
                    await process.StandardInput.DisposeAsync();
                }

                // Read stdout
                tasks.Add(ReadStreamAsync(process.StandardOutput, stdoutBuilder, cancellationToken));

                // Read stderr
                tasks.Add(ReadStreamAsync(process.StandardError, stderrBuilder, cancellationToken));

                // Wait for process to exit
                tasks.Add(process.WaitForExitAsync(cancellationToken));

                // Wait for all tasks - note that some may throw on cancellation
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelled - mark as killed if not already
                    Interlocked.Exchange(ref wasKilled, 1);
                }

                if (Volatile.Read(ref wasKilled) == 1)
                {
                    return ProcessResult.Killed(
                        stdoutBuilder.ToString(),
                        stderrBuilder.ToString());
                }

                return new ProcessResult(
                    ExitCode: process.ExitCode,
                    StandardOutput: stdoutBuilder.ToString(),
                    StandardError: stderrBuilder.ToString(),
                    WasKilled: false);
            }
            catch (OperationCanceledException)
            {
                // Cancelled - mark as killed and return
                Interlocked.Exchange(ref wasKilled, 1);
                return ProcessResult.Killed(
                    stdoutBuilder.ToString(),
                    stderrBuilder.ToString());
            }
        }
    }

    private static async Task WriteStdinAsync(
        Process process,
        string content,
        CancellationToken cancellationToken)
    {
        try
        {
            await process.StandardInput.WriteAsync(content.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync(cancellationToken);
            process.StandardInput.Close();
        }
        catch (IOException)
        {
            // Process closed stdin early (e.g., head -1 after reading first line).
            // This is expected behavior, not an error.
        }
        catch (OperationCanceledException)
        {
            // Cancelled - rethrow to propagate cancellation
            throw;
        }
    }

    private static async Task ReadStreamAsync(
        StreamReader reader,
        StringBuilder builder,
        CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        int charsRead;

        try
        {
            while ((charsRead = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
            {
                builder.Append(buffer, 0, charsRead);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled - rethrow to propagate cancellation
            throw;
        }
    }
}
