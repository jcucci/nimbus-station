using NimbusStation.Core;
using NimbusStation.Providers.Azure;

namespace NimbusStation.Tests.Providers.Azure;

public class AzureCloudProviderTests
{
    [Fact]
    public void AzureCloudProvider_HasCorrectProviderId()
    {
        // Arrange
        var provider = new AzureCloudProvider();

        // Act
        string providerId = provider.ProviderId;

        // Assert
        Assert.Equal("azure", providerId);
    }

    [Fact]
    public void AzureCloudProvider_HasCorrectDisplayName()
    {
        // Arrange
        var provider = new AzureCloudProvider();

        // Act
        string displayName = provider.DisplayName;

        // Assert
        Assert.Equal("Microsoft Azure", displayName);
    }

    [Fact]
    public async Task AzureCloudProvider_InitializeAsync_CompletesSuccessfully()
    {
        // Arrange
        var provider = new AzureCloudProvider();

        // Act & Assert (should not throw)
        await provider.InitializeAsync();
    }
}
