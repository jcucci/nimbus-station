using NimbusStation.Core;
using NimbusStation.Providers.Azure;

namespace NimbusStation.Tests.Core;

public class CloudProviderTests
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
    public void AzureCloudProvider_ImplementsICloudProvider()
    {
        // Arrange & Act
        var provider = new AzureCloudProvider();

        // Assert
        Assert.IsAssignableFrom<ICloudProvider>(provider);
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
