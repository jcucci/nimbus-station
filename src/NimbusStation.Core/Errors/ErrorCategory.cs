namespace NimbusStation.Core.Errors;

/// <summary>
/// Categories of errors for consistent handling and exit code mapping.
/// </summary>
public enum ErrorCategory
{
    /// <summary>No error (success).</summary>
    None,

    /// <summary>General/unclassified error.</summary>
    General,

    /// <summary>Authentication or authorization failure.</summary>
    Authentication,

    /// <summary>Configuration error (invalid config, missing settings).</summary>
    Configuration,

    /// <summary>Network connectivity error.</summary>
    Network,

    /// <summary>Resource not found (404).</summary>
    NotFound,

    /// <summary>Rate limiting/throttling (429).</summary>
    Throttling,

    /// <summary>Operation cancelled by user.</summary>
    Cancelled
}
