namespace NimbusStation.Core.Parsing;

/// <summary>
/// Represents a parsed pipeline with validation status.
/// </summary>
/// <param name="Segments">The parsed segments, if successful.</param>
/// <param name="IsValid">Whether the pipeline parsed successfully.</param>
/// <param name="Error">Error message if parsing failed.</param>
public sealed record ParsedPipeline(
    IReadOnlyList<PipelineSegment> Segments,
    bool IsValid,
    string? Error = null)
{
    /// <summary>
    /// Creates a successful parsed pipeline.
    /// </summary>
    public static ParsedPipeline Success(IReadOnlyList<PipelineSegment> segments) =>
        new(segments, IsValid: true);

    /// <summary>
    /// Creates a failed parsed pipeline with an error message.
    /// </summary>
    public static ParsedPipeline Failure(string error) =>
        new([], IsValid: false, error);

    /// <summary>
    /// Whether this pipeline has any external commands (pipes).
    /// </summary>
    public bool HasExternalCommands => Segments.Count > 1;

    /// <summary>
    /// The internal (first) command segment, or null if invalid.
    /// </summary>
    public PipelineSegment? InternalCommand =>
        Segments.Count > 0 ? Segments[0] : null;

    /// <summary>
    /// The external command segments (everything after the first pipe).
    /// </summary>
    public IEnumerable<PipelineSegment> ExternalCommands =>
        Segments.Skip(1);
}
