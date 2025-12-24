namespace NimbusStation.Spike.Piping;

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
    /// Creates a successful result.
    /// </summary>
    public static ProcessResult Success(string stdout, string stderr = "") =>
        new(ExitCode: 0, StandardOutput: stdout, StandardError: stderr, WasKilled: false);

    /// <summary>
    /// Creates a result for a process that exited with a non-zero code.
    /// </summary>
    public static ProcessResult Failed(int exitCode, string stdout, string stderr) =>
        new(ExitCode: exitCode, StandardOutput: stdout, StandardError: stderr, WasKilled: false);

    /// <summary>
    /// Creates a result for a process that was killed.
    /// </summary>
    public static ProcessResult Killed(string stdout = "", string stderr = "") =>
        new(ExitCode: -1, StandardOutput: stdout, StandardError: stderr, WasKilled: true);

    /// <summary>
    /// Creates a result for a process that failed to start.
    /// </summary>
    public static ProcessResult StartupError(string error) =>
        new(ExitCode: -1, StandardOutput: "", StandardError: "", WasKilled: false, Error: error);
}
