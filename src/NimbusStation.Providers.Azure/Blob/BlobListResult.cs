namespace NimbusStation.Providers.Azure.Blob;

/// <summary>
/// Represents the result of listing blobs in a container.
/// </summary>
/// <param name="Blobs">The list of blobs found.</param>
/// <param name="IsTruncated">Whether more results exist beyond the requested limit.</param>
public sealed record BlobListResult(IReadOnlyList<BlobInfo> Blobs, bool IsTruncated = false);
