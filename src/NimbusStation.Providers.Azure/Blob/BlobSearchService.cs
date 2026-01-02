using NimbusStation.Core.Search;

namespace NimbusStation.Providers.Azure.Blob;

/// <summary>
/// Provides search functionality for Azure Blob Storage.
/// Converts flat blob listings into navigable directory structures.
/// </summary>
public sealed class BlobSearchService
{
    private readonly IBlobService _blobService;

    /// <summary>
    /// The default maximum number of items to return in a search result.
    /// </summary>
    public const int DefaultMaxResults = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobSearchService"/> class.
    /// </summary>
    /// <param name="blobService">The blob service for listing blobs.</param>
    public BlobSearchService(IBlobService blobService)
    {
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
    }

    /// <summary>
    /// Searches for blobs and directories at the specified prefix.
    /// </summary>
    /// <param name="aliasName">The blob alias name.</param>
    /// <param name="prefix">The search prefix (null or empty for root).</param>
    /// <param name="maxResults">Maximum number of blobs to fetch from the API.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A search result containing directories and files.</returns>
    public async Task<SearchResult> SearchAsync(
        string aliasName,
        string? prefix,
        int maxResults = DefaultMaxResults,
        CancellationToken cancellationToken = default)
    {
        var normalizedPrefix = SearchNavigator.IsRootPrefix(prefix)
            ? null
            : prefix;

        var blobListResult = await _blobService.ListBlobsAsync(
            aliasName,
            normalizedPrefix,
            maxResults,
            cancellationToken);

        if (blobListResult.Blobs.Count == 0)
            return SearchResult.Empty(prefix ?? string.Empty);

        var items = SearchNavigator.ParseItems(
            blobListResult.Blobs,
            normalizedPrefix,
            pathSelector: b => b.Name,
            sizeSelector: b => b.Size,
            lastModifiedSelector: b => b.LastModified);

        return new SearchResult(
            Items: items,
            CurrentPrefix: prefix ?? string.Empty,
            TotalCount: items.Count,
            IsTruncated: blobListResult.IsTruncated);
    }
}
