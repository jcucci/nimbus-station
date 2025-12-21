using NimbusStation.Core;

namespace NimbusStation.Providers.Azure;

/// <summary>
/// Azure cloud provider implementation using Azure.Identity for authentication.
/// </summary>
public class AzureCloudProvider : ICloudProvider
{
    /// <inheritdoc />
    public string ProviderId => "azure";

    /// <inheritdoc />
    public string DisplayName => "Microsoft Azure";

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Initialize Azure credentials using DefaultAzureCredential
        return Task.CompletedTask;
    }
}
