namespace NimbusStation.Providers.Azure.Blob;

/// <summary>
/// Represents the result of getting blob content.
/// </summary>
/// <param name="Content">The raw content of the blob.</param>
/// <param name="ContentType">The content type (MIME type) of the blob.</param>
/// <param name="IsBinary">Whether the content appears to be binary (not text-safe for terminal output).</param>
public sealed record BlobContentResult(byte[] Content, string ContentType, bool IsBinary)
{
    private static readonly HashSet<string> BinaryPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/octet-stream",
        "application/zip",
        "application/gzip",
        "application/x-tar",
        "application/x-gzip",
        "application/x-bzip2",
        "application/x-7z-compressed",
        "application/x-rar-compressed",
        "application/pdf",
        "application/msword",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats",
        "image/",
        "audio/",
        "video/"
    };

    /// <summary>
    /// Determines if a content type represents binary content.
    /// </summary>
    /// <param name="contentType">The content type to check.</param>
    /// <returns>True if the content type is considered binary.</returns>
    public static bool IsBinaryContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        foreach (var prefix in BinaryPrefixes)
        {
            if (contentType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
