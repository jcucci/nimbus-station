namespace NimbusStation.Core.Session;

/// <summary>
/// Represents the active context within a session.
/// Tracks which resource aliases are currently selected for operations.
/// </summary>
/// <param name="ActiveCosmosAlias">The currently selected CosmosDB alias, if any.</param>
/// <param name="ActiveBlobAlias">The currently selected Blob storage alias, if any.</param>
/// <param name="ActiveStorageAlias">The currently selected Storage account alias, if any.</param>
public sealed record SessionContext(
    string? ActiveCosmosAlias,
    string? ActiveBlobAlias,
    string? ActiveStorageAlias = null)
{
    /// <summary>
    /// Gets an empty context with no active aliases.
    /// </summary>
    public static SessionContext Empty => new(null, null, null);
}
