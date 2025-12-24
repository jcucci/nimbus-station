namespace NimbusStation.Core.Session;

/// <summary>
/// Service for managing session lifecycle operations.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Gets or sets the currently active session for the REPL.
    /// </summary>
    Session? CurrentSession { get; set; }

    /// <summary>
    /// Starts a session with the specified name. Creates a new session if it doesn't exist,
    /// or resumes the existing session if it does.
    /// </summary>
    /// <param name="sessionName">The name for the session (e.g., ticket ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session (newly created or resumed).</returns>
    /// <exception cref="InvalidSessionNameException">Thrown if the session name is invalid.</exception>
    Task<Session> StartSessionAsync(string sessionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all existing sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of all sessions.</returns>
    Task<IReadOnlyList<Session>> ListSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes an existing session by name.
    /// </summary>
    /// <param name="sessionName">The name of the session to resume.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resumed session with updated LastAccessedAt.</returns>
    /// <exception cref="SessionNotFoundException">Thrown if the session does not exist.</exception>
    Task<Session> ResumeSessionAsync(string sessionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session and its associated directory.
    /// </summary>
    /// <param name="sessionName">The name of the session to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="SessionNotFoundException">Thrown if the session does not exist.</exception>
    Task DeleteSessionAsync(string sessionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the active context for a session.
    /// </summary>
    /// <param name="sessionName">The name of the session.</param>
    /// <param name="context">The new active context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated session.</returns>
    /// <exception cref="SessionNotFoundException">Thrown if the session does not exist.</exception>
    Task<Session> UpdateSessionContextAsync(string sessionName, SessionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a session with the specified name exists.
    /// </summary>
    /// <param name="sessionName">The name of the session.</param>
    /// <returns>True if the session exists, false otherwise.</returns>
    bool SessionExists(string sessionName);

    /// <summary>
    /// Gets the root directory path for a session.
    /// </summary>
    /// <param name="sessionName">The name of the session.</param>
    /// <returns>The full path to the session directory.</returns>
    string GetSessionDirectory(string sessionName);

    /// <summary>
    /// Gets the downloads directory path for a session.
    /// </summary>
    /// <param name="sessionName">The name of the session.</param>
    /// <returns>The full path to the downloads directory.</returns>
    string GetDownloadsDirectory(string sessionName);

    /// <summary>
    /// Gets the queries directory path for a session.
    /// </summary>
    /// <param name="sessionName">The name of the session.</param>
    /// <returns>The full path to the queries directory.</returns>
    string GetQueriesDirectory(string sessionName);
}
