namespace NimbusStation.Core.Session;

/// <summary>
/// Exception thrown when attempting to create a session that already exists.
/// </summary>
public sealed class SessionAlreadyExistsException : Exception
{
    /// <summary>
    /// Gets the name of the session that already exists.
    /// </summary>
    public string SessionName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionAlreadyExistsException"/> class.
    /// </summary>
    /// <param name="sessionName">The name of the session that already exists.</param>
    public SessionAlreadyExistsException(string sessionName)
        : base($"Session '{sessionName}' already exists. Use 'session resume {sessionName}' to continue.")
    {
        SessionName = sessionName;
    }
}

/// <summary>
/// Exception thrown when attempting to access a session that does not exist.
/// </summary>
public sealed class SessionNotFoundException : Exception
{
    /// <summary>
    /// Gets the name of the session that was not found.
    /// </summary>
    public string SessionName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionNotFoundException"/> class.
    /// </summary>
    /// <param name="sessionName">The name of the session that was not found.</param>
    public SessionNotFoundException(string sessionName)
        : base($"Session '{sessionName}' not found. Use 'session list' to see available sessions.")
    {
        SessionName = sessionName;
    }
}

/// <summary>
/// Exception thrown when a session name is invalid.
/// </summary>
public sealed class InvalidSessionNameException : Exception
{
    /// <summary>
    /// Gets the invalid session name.
    /// </summary>
    public string SessionName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidSessionNameException"/> class.
    /// </summary>
    /// <param name="sessionName">The invalid session name.</param>
    /// <param name="reason">The reason the name is invalid.</param>
    public InvalidSessionNameException(string sessionName, string reason)
        : base($"Invalid session name '{sessionName}': {reason}")
    {
        SessionName = sessionName;
    }
}
