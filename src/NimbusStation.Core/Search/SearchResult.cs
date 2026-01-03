namespace NimbusStation.Core.Search;

/// <summary>
/// Represents the result of a search operation.
/// </summary>
/// <param name="Items">The search items found (directories and files).</param>
/// <param name="CurrentPrefix">The current search prefix.</param>
/// <param name="IsTruncated">Whether the results were truncated due to limits.</param>
public sealed record SearchResult(
    IReadOnlyList<ISearchItem> Items,
    string CurrentPrefix,
    bool IsTruncated)
{
    /// <summary>
    /// Creates an empty search result.
    /// </summary>
    /// <param name="prefix">The prefix that was searched.</param>
    public static SearchResult Empty(string prefix) =>
        new(Items: [], CurrentPrefix: prefix, IsTruncated: false);
}
