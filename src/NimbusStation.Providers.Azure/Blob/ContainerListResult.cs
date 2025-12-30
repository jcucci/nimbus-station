namespace NimbusStation.Providers.Azure.Blob;

/// <summary>
/// Represents the result of listing containers in a storage account.
/// </summary>
/// <param name="Containers">The list of containers found.</param>
public sealed record ContainerListResult(IReadOnlyList<ContainerInfo> Containers);
