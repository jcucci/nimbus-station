namespace NimbusStation.Core.Parsing;

/// <summary>
/// Represents a single segment in a piped command chain.
/// </summary>
/// <param name="Content">The command content (trimmed).</param>
/// <param name="Index">Zero-based index in the pipeline.</param>
/// <param name="IsFirst">Whether this is the first segment (internal command).</param>
/// <param name="IsLast">Whether this is the last segment.</param>
public sealed record PipelineSegment(
    string Content,
    int Index,
    bool IsFirst,
    bool IsLast);
