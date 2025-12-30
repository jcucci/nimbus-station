namespace NimbusStation.Core.Errors;

/// <summary>
/// Standard exit codes for the CLI application.
/// </summary>
public static class ExitCodes
{
    /// <summary>Command completed successfully.</summary>
    public const int Success = 0;

    /// <summary>General/unclassified error.</summary>
    public const int GeneralError = 1;

    /// <summary>User cancellation (Ctrl+C or declined confirmation).</summary>
    public const int UserCancellation = 2;

    /// <summary>Authentication or authorization error.</summary>
    public const int AuthenticationError = 3;

    /// <summary>Configuration error (invalid config, missing settings).</summary>
    public const int ConfigurationError = 4;

    /// <summary>
    /// Maps an error category to the appropriate exit code.
    /// </summary>
    public static int FromCategory(ErrorCategory category) => category switch
    {
        ErrorCategory.General => GeneralError,
        ErrorCategory.Authentication => AuthenticationError,
        ErrorCategory.Configuration => ConfigurationError,
        ErrorCategory.Network => GeneralError,
        ErrorCategory.NotFound => GeneralError,
        ErrorCategory.Throttling => GeneralError,
        ErrorCategory.Cancelled => UserCancellation,
        _ => GeneralError
    };
}
