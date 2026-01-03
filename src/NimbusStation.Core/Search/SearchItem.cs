namespace NimbusStation.Core.Search;

/// <summary>
/// Default implementation of <see cref="ISearchItem"/>.
/// </summary>
/// <param name="Name">The display name of the item.</param>
/// <param name="FullPath">The full path for API calls.</param>
/// <param name="Kind">The kind of item (directory or file).</param>
/// <param name="Size">The size in bytes (null for directories).</param>
/// <param name="LastModified">The last modified timestamp.</param>
public sealed record SearchItem(
    string Name,
    string FullPath,
    SearchItemKind Kind,
    long? Size,
    DateTimeOffset? LastModified) : ISearchItem
{
    /// <summary>
    /// Creates a directory search item.
    /// </summary>
    /// <param name="name">The display name (e.g., "2024-01/").</param>
    /// <param name="fullPath">The full path for navigation.</param>
    public static SearchItem Directory(string name, string fullPath) =>
        new(name, fullPath, SearchItemKind.Directory, Size: null, LastModified: null);

    /// <summary>
    /// Creates a file search item.
    /// </summary>
    /// <param name="name">The display name (e.g., "data.json").</param>
    /// <param name="fullPath">The full path for retrieval.</param>
    /// <param name="size">The file size in bytes.</param>
    /// <param name="lastModified">The last modified timestamp.</param>
    public static SearchItem File(string name, string fullPath, long size, DateTimeOffset lastModified) =>
        new(name, fullPath, SearchItemKind.File, size, lastModified);
}
