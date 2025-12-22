namespace NimbusStation.Core.Commands;

/// <summary>
/// Represents a command that is specific to a cloud provider.
/// Provider commands are prefixed with the provider name (e.g., "azure cosmos query").
/// </summary>
public interface IProviderCommand : ICommand
{
    /// <summary>
    /// Gets the provider identifier this command belongs to (e.g., "azure", "aws", "gcp").
    /// </summary>
    string ProviderId { get; }
}
