namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Resolves include file paths and detects circular includes.
/// </summary>
public sealed class IncludeResolver
{
    private readonly HashSet<string> _visitedPaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves an include path relative to the base file path.
    /// </summary>
    /// <param name="includePath">The path specified in the include directive.</param>
    /// <param name="baseFilePath">The path of the file containing the include.</param>
    /// <returns>The resolved absolute path.</returns>
    public string ResolvePath(string includePath, string baseFilePath)
    {
        if (includePath.StartsWith('~'))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var relativePart = includePath.Length > 1 && includePath[1] == Path.DirectorySeparatorChar
                ? includePath[2..]
                : includePath[1..];
            return Path.GetFullPath(Path.Combine(homeDir, relativePart));
        }

        if (Path.IsPathRooted(includePath))
            return Path.GetFullPath(includePath);

        var baseDir = Path.GetDirectoryName(baseFilePath) ?? string.Empty;
        return Path.GetFullPath(Path.Combine(baseDir, includePath));
    }

    /// <summary>
    /// Marks a file path as being processed. Returns false if the path was already visited (circular include).
    /// </summary>
    /// <param name="resolvedPath">The resolved absolute path.</param>
    /// <returns>True if this is the first visit; false if it's a circular include.</returns>
    public bool TryMarkVisited(string resolvedPath) =>
        _visitedPaths.Add(Path.GetFullPath(resolvedPath));

    /// <summary>
    /// Checks if a path has already been visited (without marking it).
    /// </summary>
    /// <param name="resolvedPath">The resolved absolute path.</param>
    /// <returns>True if the path has been visited.</returns>
    public bool HasVisited(string resolvedPath) =>
        _visitedPaths.Contains(Path.GetFullPath(resolvedPath));

    /// <summary>
    /// Resets the visited paths tracking.
    /// </summary>
    public void Reset() => _visitedPaths.Clear();
}
