namespace NimbusStation.Providers.Azure.Blob;

/// <summary>
/// Represents information about a blob in Azure Blob Storage.
/// </summary>
/// <param name="Name">The full name (path) of the blob.</param>
/// <param name="Size">The size of the blob in bytes.</param>
/// <param name="LastModified">The last modified timestamp.</param>
/// <param name="ContentType">The content type (MIME type) of the blob.</param>
public sealed record BlobInfo(
    string Name,
    long Size,
    DateTimeOffset LastModified,
    string ContentType);
