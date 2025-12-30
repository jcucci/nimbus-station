namespace NimbusStation.Infrastructure.Configuration;

/// <summary>
/// Configuration for a storage account alias (account-level operations like container listing).
/// </summary>
/// <param name="Account">The storage account name.</param>
public sealed record StorageAliasConfig(string Account);
