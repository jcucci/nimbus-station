namespace NimbusStation.Core.Commands;

/// <summary>
/// A label + description pair used in structured help output.
/// </summary>
/// <param name="Label">The short label (e.g., subcommand name or flag).</param>
/// <param name="Description">A brief description of the label.</param>
public sealed record HelpEntry(string Label, string Description);
