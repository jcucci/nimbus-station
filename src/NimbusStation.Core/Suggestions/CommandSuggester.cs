namespace NimbusStation.Core.Suggestions;

/// <summary>
/// Suggests similar commands for "did you mean?" functionality.
/// </summary>
public static class CommandSuggester
{
    /// <summary>
    /// The default maximum edit distance for a suggestion to be considered.
    /// </summary>
    public const int DefaultMaxDistance = 2;

    /// <summary>
    /// Finds commands similar to the input within the specified edit distance.
    /// </summary>
    /// <param name="input">The mistyped command.</param>
    /// <param name="validCommands">The list of valid command names.</param>
    /// <param name="maxDistance">Maximum edit distance to consider (default: 2).</param>
    /// <returns>A list of suggestions sorted by similarity (closest first), or empty if none found.</returns>
    public static IReadOnlyList<string> GetSuggestions(
        string input,
        IEnumerable<string> validCommands,
        int maxDistance = DefaultMaxDistance)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        var suggestions = validCommands
            .Select(cmd => new { Command = cmd, Distance = LevenshteinDistance.Compute(input, cmd) })
            .Where(x => x.Distance <= maxDistance && x.Distance > 0) // Exclude exact matches
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Command, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Command)
            .ToList();

        return suggestions;
    }

    /// <summary>
    /// Gets the single best suggestion if one exists within the threshold.
    /// </summary>
    /// <param name="input">The mistyped command.</param>
    /// <param name="validCommands">The list of valid command names.</param>
    /// <param name="maxDistance">Maximum edit distance to consider (default: 2).</param>
    /// <returns>The best suggestion, or null if none found.</returns>
    public static string? GetBestSuggestion(
        string input,
        IEnumerable<string> validCommands,
        int maxDistance = DefaultMaxDistance)
    {
        var suggestions = GetSuggestions(input, validCommands, maxDistance);
        return suggestions.Count > 0 ? suggestions[0] : null;
    }
}
