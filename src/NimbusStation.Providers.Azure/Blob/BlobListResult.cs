namespace NimbusStation.Providers.Azure.Blob;

/// <summary>
/// Represents the result of listing blobs in a container.
/// </summary>
/// <param name="Blobs">The list of blobs found.</param>
public sealed record BlobListResult(IReadOnlyList<BlobInfo> Blobs);
