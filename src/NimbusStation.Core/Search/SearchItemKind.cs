namespace NimbusStation.Core.Search;

/// <summary>
/// Represents the type of item in a search result.
/// </summary>
public enum SearchItemKind
{
    /// <summary>
    /// A directory that can be navigated into.
    /// </summary>
    Directory,

    /// <summary>
    /// A file that can be selected for output or download.
    /// </summary>
    File
}
