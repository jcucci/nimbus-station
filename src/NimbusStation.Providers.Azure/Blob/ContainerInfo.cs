namespace NimbusStation.Providers.Azure.Blob;

/// <summary>
/// Represents information about a container in Azure Blob Storage.
/// </summary>
/// <param name="Name">The name of the container.</param>
/// <param name="LastModified">The last modified timestamp.</param>
public sealed record ContainerInfo(string Name, DateTimeOffset LastModified);
