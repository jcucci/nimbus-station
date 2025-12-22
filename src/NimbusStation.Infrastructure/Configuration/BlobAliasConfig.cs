namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for a Blob storage connection alias.
/// </summary>
/// <param name="Account">The storage account name.</param>
/// <param name="Container">The blob container name.</param>
public sealed record BlobAliasConfig(string Account, string Container);
