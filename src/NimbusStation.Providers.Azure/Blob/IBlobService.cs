namespace NimbusStation.Providers.Azure.Blob;

/// <summary>
/// Service for interacting with Azure Blob Storage using the Azure CLI.
/// </summary>
public interface IBlobService
{
    /// <summary>
    /// Lists containers in a storage account.
    /// </summary>
    /// <param name="storageAliasName">The name of the configured storage alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of containers.</returns>
    Task<ContainerListResult> ListContainersAsync(
        string storageAliasName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists blobs in a container with optional prefix filter.
    /// </summary>
    /// <param name="blobAliasName">The name of the configured blob alias.</param>
    /// <param name="prefix">Optional prefix to filter blobs.</param>
    /// <param name="maxResults">Maximum number of results to return. Null for no limit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of blobs.</returns>
    Task<BlobListResult> ListBlobsAsync(
        string blobAliasName,
        string? prefix = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the content of a blob.
    /// </summary>
    /// <param name="blobAliasName">The name of the configured blob alias.</param>
    /// <param name="blobName">The name (path) of the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The blob content with metadata.</returns>
    Task<BlobContentResult> GetBlobContentAsync(
        string blobAliasName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a blob to a local file, preserving the blob's path structure.
    /// </summary>
    /// <param name="blobAliasName">The name of the configured blob alias.</param>
    /// <param name="blobName">The name (path) of the blob.</param>
    /// <param name="destinationDirectory">The base directory to download to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full path to the downloaded file.</returns>
    Task<string> DownloadBlobAsync(
        string blobAliasName,
        string blobName,
        string destinationDirectory,
        CancellationToken cancellationToken = default);
}
