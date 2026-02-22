namespace NimbusStation.Core.Search;

/// <summary>
/// Provides utilities for navigating and parsing search paths.
/// Works with cloud storage path conventions (forward slashes, trailing slashes for directories).
/// </summary>
public static class SearchNavigator
{
    private const char PathSeparator = '/';

    /// <summary>
    /// Gets the parent prefix of the given path.
    /// </summary>
    /// <param name="prefix">The current prefix (e.g., "data/logs/2024/").</param>
    /// <returns>The parent prefix (e.g., "data/logs/"), or empty string if at root.</returns>
    public static string GetParentPrefix(string? prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return string.Empty;

        // Remove trailing slash if present
        var trimmed = prefix.TrimEnd(PathSeparator);
        if (string.IsNullOrEmpty(trimmed))
            return string.Empty;

        var lastSeparator = trimmed.LastIndexOf(PathSeparator);
        return lastSeparator < 0 ? string.Empty : trimmed[..(lastSeparator + 1)];
    }

    /// <summary>
    /// Combines a prefix with a segment to form a new path.
    /// </summary>
    /// <param name="prefix">The base prefix (e.g., "data/logs/").</param>
    /// <param name="segment">The segment to append (e.g., "2024-01/").</param>
    /// <returns>The combined path (e.g., "data/logs/2024-01/").</returns>
    public static string CombinePrefix(string? prefix, string segment)
    {
        if (string.IsNullOrEmpty(prefix))
            return segment;

        // Ensure prefix ends with separator
        var normalizedPrefix = prefix.EndsWith(PathSeparator) ? prefix : prefix + PathSeparator;
        return normalizedPrefix + segment;
    }

    /// <summary>
    /// Checks if a prefix represents the root (empty or null).
    /// </summary>
    /// <param name="prefix">The prefix to check.</param>
    /// <returns>True if the prefix is root.</returns>
    public static bool IsRootPrefix(string? prefix) =>
        string.IsNullOrEmpty(prefix);

    /// <summary>
    /// Extracts the display name from a full path relative to the current prefix.
    /// </summary>
    /// <param name="fullPath">The full path (e.g., "data/logs/file.json").</param>
    /// <param name="currentPrefix">The current prefix (e.g., "data/logs/").</param>
    /// <returns>The display name (e.g., "file.json").</returns>
    public static string GetDisplayName(string fullPath, string? currentPrefix)
    {
        if (string.IsNullOrEmpty(currentPrefix))
            return fullPath;

        return fullPath.StartsWith(currentPrefix, StringComparison.Ordinal)
            ? fullPath[currentPrefix.Length..]
            : fullPath;
    }

    /// <summary>
    /// Parses a flat list of file paths into a hierarchical list of directories and files
    /// relative to the current prefix.
    /// </summary>
    /// <typeparam name="T">The type of source items.</typeparam>
    /// <param name="items">The source items to parse.</param>
    /// <param name="currentPrefix">The current search prefix.</param>
    /// <param name="pathSelector">Function to extract the path from an item.</param>
    /// <param name="sizeSelector">Function to extract the size from an item.</param>
    /// <param name="lastModifiedSelector">Function to extract the last modified date from an item.</param>
    /// <returns>A list of search items with directories first, then files, both sorted alphabetically.</returns>
    public static IReadOnlyList<SearchItem> ParseItems<T>(
        IEnumerable<T> items,
        string? currentPrefix,
        Func<T, string> pathSelector,
        Func<T, long> sizeSelector,
        Func<T, DateTimeOffset> lastModifiedSelector)
    {
        var filterPrefix = currentPrefix ?? string.Empty;

        // For display names, only strip up to the last directory separator.
        // A prefix like "Organizations/888" should filter on the full string
        // but display names relative to "Organizations/" so we see the full
        // directory name "88853aa0-.../" rather than the truncated "53aa0-.../".
        var displayPrefix = filterPrefix.Contains(PathSeparator)
            ? filterPrefix[..(filterPrefix.LastIndexOf(PathSeparator) + 1)]
            : string.Empty;

        var directories = new HashSet<string>(StringComparer.Ordinal);
        var files = new List<SearchItem>();

        foreach (var item in items)
        {
            var fullPath = pathSelector(item);

            // Skip items that don't match the prefix
            if (!string.IsNullOrEmpty(filterPrefix) &&
                !fullPath.StartsWith(filterPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            // Get the relative path after the display prefix
            var relativePath = string.IsNullOrEmpty(displayPrefix)
                ? fullPath
                : fullPath[displayPrefix.Length..];

            // Check if there's a directory separator in the relative path
            var separatorIndex = relativePath.IndexOf(PathSeparator);

            if (separatorIndex >= 0)
            {
                // This item is inside a subdirectory - extract the immediate directory
                var directoryName = relativePath[..(separatorIndex + 1)]; // Include trailing slash
                var directoryFullPath = displayPrefix + directoryName;
                directories.Add(directoryFullPath);
            }
            else
            {
                // This is a file directly under the current prefix
                files.Add(SearchItem.File(
                    name: relativePath,
                    fullPath: fullPath,
                    size: sizeSelector(item),
                    lastModified: lastModifiedSelector(item)));
            }
        }

        // Build result: directories first (sorted), then files (sorted)
        var result = new List<SearchItem>(directories.Count + files.Count);

        foreach (var dirPath in directories.OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var displayName = GetDisplayName(dirPath, displayPrefix);
            result.Add(SearchItem.Directory(displayName, dirPath));
        }

        result.AddRange(files.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase));

        return result;
    }

    /// <summary>
    /// Normalizes a prefix by ensuring it ends with a separator (unless empty).
    /// </summary>
    /// <param name="prefix">The prefix to normalize.</param>
    /// <returns>The normalized prefix.</returns>
    public static string NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return string.Empty;

        return prefix.EndsWith(PathSeparator) ? prefix : prefix + PathSeparator;
    }
}
