namespace NimbusStation.Core.ShellPiping;

/// <summary>
/// Represents the result of executing an external process.
/// </summary>
/// <param name="ExitCode">The process exit code (0 typically means success).</param>
/// <param name="StandardOutput">Captured standard output.</param>
/// <param name="StandardError">Captured standard error.</param>
/// <param name="WasKilled">Whether the process was killed (e.g., via cancellation).</param>
/// <param name="Error">Error message if the process failed to start.</param>
public sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool WasKilled,
    string? Error = null)
{
    /// <summary>
    /// Whether the process executed successfully (exit code 0, not killed, no startup error).
    /// </summary>
    public bool IsSuccess => ExitCode == 0 && !WasKilled && Error is null;

    /// <summary>
    /// Creates a successful result with exit code 0.
    /// </summary>
    /// <param name="stdout">The standard output content.</param>
    /// <param name="stderr">The standard error content (optional).</param>
    public static ProcessResult Success(string stdout, string stderr = "") =>
        new(ExitCode: 0, StandardOutput: stdout, StandardError: stderr, WasKilled: false);

    /// <summary>
    /// Creates a result for a process that exited with a non-zero code.
    /// </summary>
    /// <param name="exitCode">The non-zero exit code.</param>
    /// <param name="stdout">The standard output content.</param>
    /// <param name="stderr">The standard error content.</param>
    public static ProcessResult Failed(int exitCode, string stdout, string stderr) =>
        new(ExitCode: exitCode, StandardOutput: stdout, StandardError: stderr, WasKilled: false);

    /// <summary>
    /// Creates a result for a process that was killed (e.g., via cancellation).
    /// </summary>
    /// <param name="stdout">Any standard output captured before the process was killed.</param>
    /// <param name="stderr">Any standard error captured before the process was killed.</param>
    public static ProcessResult Killed(string stdout = "", string stderr = "") =>
        new(ExitCode: -1, StandardOutput: stdout, StandardError: stderr, WasKilled: true);

    /// <summary>
    /// Creates a result for a process that failed to start.
    /// </summary>
    /// <param name="error">The error message describing why the process failed to start.</param>
    public static ProcessResult StartupError(string error) =>
        new(ExitCode: -1, StandardOutput: "", StandardError: "", WasKilled: false, Error: error);
}
