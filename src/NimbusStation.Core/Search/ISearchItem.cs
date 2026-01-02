namespace NimbusStation.Core.Search;

/// <summary>
/// Represents an item in a search result that can be navigated or selected.
/// This abstraction supports multiple cloud providers (Azure, AWS, GCP).
/// </summary>
public interface ISearchItem
{
    /// <summary>
    /// Gets the display name of the item (e.g., "2024-01/" for a directory, "file.json" for a file).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the full path of the item for API calls and navigation.
    /// </summary>
    string FullPath { get; }

    /// <summary>
    /// Gets the kind of item (directory or file).
    /// </summary>
    SearchItemKind Kind { get; }

    /// <summary>
    /// Gets the size of the item in bytes. Null for directories.
    /// </summary>
    long? Size { get; }

    /// <summary>
    /// Gets the last modified timestamp. Null if not available.
    /// </summary>
    DateTimeOffset? LastModified { get; }
}
