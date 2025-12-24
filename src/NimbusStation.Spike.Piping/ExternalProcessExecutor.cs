using System.Diagnostics;
using System.Text;

namespace NimbusStation.Spike.Piping;

/// <summary>
/// Executes external processes with stdin streaming and output capture.
/// </summary>
public sealed class ExternalProcessExecutor
{
    /// <summary>
    /// Executes an external command, optionally piping content to stdin.
    /// </summary>
    /// <param name="command">The command/executable to run.</param>
    /// <param name="arguments">Command arguments (optional).</param>
    /// <param name="stdinContent">Content to write to the process's stdin (optional).</param>
    /// <param name="cancellationToken">Cancellation token to kill the process.</param>
    /// <returns>The process result with captured output.</returns>
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

            // Set up cancellation
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
                // Run stdin writing, stdout reading, and stderr reading concurrently
                // This prevents deadlocks when the process's output buffer fills up
                var tasks = new List<Task>();

                // Write to stdin if we have content
                if (stdinContent is not null)
                {
                    tasks.Add(WriteStdinAsync(process, stdinContent, cancellationToken));
                }

                // Read stdout
                var stdoutTask = ReadStreamAsync(process.StandardOutput, stdoutBuilder, cancellationToken);
                tasks.Add(stdoutTask);

                // Read stderr
                var stderrTask = ReadStreamAsync(process.StandardError, stderrBuilder, cancellationToken);
                tasks.Add(stderrTask);

                // Wait for process to exit
                var exitTask = process.WaitForExitAsync(cancellationToken);
                tasks.Add(exitTask);

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

    /// <summary>
    /// Parses a command string into executable and arguments.
    /// Handles simple cases like "jq '.name'" -> ("jq", "'.name'")
    /// </summary>
    public static (string Command, string? Arguments) ParseCommand(string commandString)
    {
        if (string.IsNullOrWhiteSpace(commandString))
            return ("", null);

        var trimmed = commandString.Trim();
        var firstSpace = -1;
        var inQuote = false;
        var quoteChar = '\0';

        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];

            if (inQuote)
            {
                if (c == quoteChar)
                    inQuote = false;
                continue;
            }

            if (c is '"' or '\'')
            {
                inQuote = true;
                quoteChar = c;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                firstSpace = i;
                break;
            }
        }

        if (firstSpace == -1)
            return (trimmed, null);

        var command = trimmed[..firstSpace];
        var arguments = trimmed[(firstSpace + 1)..].TrimStart();

        return (command, string.IsNullOrEmpty(arguments) ? null : arguments);
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
            // Process closed stdin early (e.g., head -1 after reading first line)
            // This is expected behavior, not an error
        }
        catch (OperationCanceledException)
        {
            // Cancelled - expected
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
            // Cancelled - expected
            throw;
        }
    }
}
