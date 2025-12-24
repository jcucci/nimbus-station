using System.Diagnostics;
using System.Text;
using NimbusStation.Core.ShellPiping;

namespace NimbusStation.Infrastructure.ShellPiping;

/// <summary>
/// Executes external processes with stdin streaming and output capture.
/// </summary>
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
            var wasKilled = false;

            // Set up cancellation to kill the process
            await using var registration = cancellationToken.Register(() =>
            {
                wasKilled = true;
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Process may have already exited
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
                    process.StandardInput.Close();
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
                catch (OperationCanceledException) when (wasKilled)
                {
                    // Expected when cancelled
                }

                if (wasKilled)
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
            catch (OperationCanceledException) when (wasKilled)
            {
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
