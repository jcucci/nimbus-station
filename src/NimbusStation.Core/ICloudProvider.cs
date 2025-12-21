namespace NimbusStation.Core;

/// <summary>
/// Represents a cloud provider abstraction for multi-cloud support.
/// </summary>
public interface ICloudProvider
{
    /// <summary>
    /// Gets the unique identifier for this cloud provider.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Gets the display name of the cloud provider.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Initializes the cloud provider with the current session context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
