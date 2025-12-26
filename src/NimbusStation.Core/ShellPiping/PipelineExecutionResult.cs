namespace NimbusStation.Core.ShellPiping;

/// <summary>
/// Represents the result of executing a pipeline (internal command piped to external process).
/// </summary>
/// <param name="Success">Whether the pipeline executed successfully.</param>
/// <param name="Output">The standard output from the external process.</param>
/// <param name="ErrorOutput">The standard error from the external process.</param>
/// <param name="ExternalExitCode">The exit code from the external process, if one was executed.</param>
/// <param name="Error">Error message if the pipeline failed.</param>
public sealed record PipelineExecutionResult(
    bool Success,
    string? Output,
    string? ErrorOutput,
    int? ExternalExitCode,
    string? Error)
{
    /// <summary>
    /// Creates a successful pipeline result.
    /// </summary>
    /// <param name="output">The standard output from the pipeline.</param>
    /// <param name="errorOutput">The standard error from the external process.</param>
    /// <param name="exitCode">The exit code from the external process.</param>
    public static PipelineExecutionResult Succeeded(string? output, string? errorOutput, int exitCode) =>
        new(Success: true, Output: output, ErrorOutput: errorOutput, ExternalExitCode: exitCode, Error: null);

    /// <summary>
    /// Creates a failed pipeline result due to an error.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    public static PipelineExecutionResult Failed(string error) =>
        new(Success: false, Output: null, ErrorOutput: null, ExternalExitCode: null, Error: error);

    /// <summary>
    /// Creates a failed pipeline result due to internal command failure.
    /// </summary>
    /// <param name="error">The error message from the internal command.</param>
    public static PipelineExecutionResult InternalCommandFailed(string error) =>
        new(Success: false, Output: null, ErrorOutput: null, ExternalExitCode: null, Error: error);

    /// <summary>
    /// Creates a result for when the pipeline was cancelled.
    /// </summary>
    /// <param name="partialOutput">Any output captured before cancellation.</param>
    /// <param name="partialErrorOutput">Any error output captured before cancellation.</param>
    public static PipelineExecutionResult Cancelled(string? partialOutput = null, string? partialErrorOutput = null) =>
        new(Success: false, Output: partialOutput, ErrorOutput: partialErrorOutput, ExternalExitCode: null, Error: "Pipeline cancelled");

    /// <summary>
    /// Whether the external process exited with a non-zero exit code.
    /// </summary>
    public bool HasNonZeroExitCode => ExternalExitCode is not null && ExternalExitCode != 0;

    /// <summary>
    /// Whether there is error output from the external process.
    /// </summary>
    public bool HasErrorOutput => !string.IsNullOrEmpty(ErrorOutput);
}
