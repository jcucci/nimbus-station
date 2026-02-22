namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for a storage account alias (account-level operations like container listing).
/// </summary>
/// <param name="Account">The storage account name.</param>
/// <param name="AuthMode">The Azure CLI auth mode ("login" or "key"). Defaults to "login" if not specified.</param>
public sealed record StorageAliasConfig(string Account, string? AuthMode = null);
