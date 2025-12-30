namespace NimbusStation.Core.Suggestions;

/// <summary>
/// Calculates the Levenshtein (edit) distance between two strings.
/// </summary>
public static class LevenshteinDistance
{
    /// <summary>
    /// Computes the minimum number of single-character edits (insertions, deletions, or substitutions)
    /// required to change one string into another.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="target">The target string.</param>
    /// <param name="ignoreCase">If true, comparison is case-insensitive.</param>
    /// <returns>The edit distance between the two strings.</returns>
    public static int Compute(string source, string target, bool ignoreCase = true)
    {
        if (string.IsNullOrEmpty(source))
            return string.IsNullOrEmpty(target) ? 0 : target.Length;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        if (ignoreCase)
        {
            source = source.ToLowerInvariant();
            target = target.ToLowerInvariant();
        }

        var sourceLength = source.Length;
        var targetLength = target.Length;

        // Use two rows instead of full matrix for memory efficiency
        var previousRow = new int[targetLength + 1];
        var currentRow = new int[targetLength + 1];

        // Initialize the first row (distance from empty string to each prefix of target)
        for (var j = 0; j <= targetLength; j++)
            previousRow[j] = j;

        for (var i = 1; i <= sourceLength; i++)
        {
            currentRow[0] = i;

            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                currentRow[j] = Math.Min(
                    Math.Min(
                        currentRow[j - 1] + 1,      // Insertion
                        previousRow[j] + 1),        // Deletion
                    previousRow[j - 1] + cost);     // Substitution
            }

            // Swap rows
            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[targetLength];
    }
}
