namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for a Blob storage connection alias.
/// </summary>
/// <param name="Account">The storage account name.</param>
/// <param name="Container">The blob container name.</param>
/// <param name="AuthMode">The Azure CLI auth mode ("login" or "key"). Defaults to "login" if not specified.</param>
public sealed record BlobAliasConfig(string Account, string Container, string? AuthMode = null);
