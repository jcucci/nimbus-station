namespace NimbusStation.Core.Commands;

/// <summary>
/// Structured help metadata for a command, used by the help renderer
/// to produce TLDR-style inline help.
/// </summary>
public sealed record CommandHelpMetadata
{
    /// <summary>
    /// Gets the available subcommands for this command.
    /// </summary>
    public IReadOnlyList<HelpEntry> Subcommands { get; init; } = [];

    /// <summary>
    /// Gets the available flags for this command.
    /// </summary>
    public IReadOnlyList<HelpEntry> Flags { get; init; } = [];

    /// <summary>
    /// Gets usage examples for this command.
    /// </summary>
    public IReadOnlyList<HelpEntry> Examples { get; init; } = [];

    /// <summary>
    /// Gets optional notes displayed at the end of the help output.
    /// </summary>
    public string? Notes { get; init; }
}
