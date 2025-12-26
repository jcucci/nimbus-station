namespace NimbusStation.Core.Session;

/// <summary>
/// Manages the current active session state during REPL execution.
/// Provides domain-focused methods for session lifecycle and context mutations.
/// </summary>
public interface ISessionStateManager
{
    /// <summary>
    /// Gets the currently active session, if any.
    /// </summary>
    Session? CurrentSession { get; }

    /// <summary>
    /// Gets a value indicating whether there is an active session.
    /// </summary>
    bool HasActiveSession { get; }

    /// <summary>
    /// Activates a session as the current working session.
    /// </summary>
    /// <param name="session">The session to activate.</param>
    void ActivateSession(Session session);

    /// <summary>
    /// Deactivates the current session, clearing the active state.
    /// </summary>
    void DeactivateSession();

    /// <summary>
    /// Sets the active Cosmos DB alias for the current session.
    /// </summary>
    /// <param name="aliasName">The alias name to set.</param>
    /// <exception cref="InvalidOperationException">Thrown when no session is active.</exception>
    void SetCosmosAlias(string aliasName);

    /// <summary>
    /// Sets the active Blob storage alias for the current session.
    /// </summary>
    /// <param name="aliasName">The alias name to set.</param>
    /// <exception cref="InvalidOperationException">Thrown when no session is active.</exception>
    void SetBlobAlias(string aliasName);

    /// <summary>
    /// Clears the active Cosmos DB alias for the current session.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no session is active.</exception>
    void ClearCosmosAlias();

    /// <summary>
    /// Clears the active Blob storage alias for the current session.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no session is active.</exception>
    void ClearBlobAlias();

    /// <summary>
    /// Clears all aliases (Cosmos and Blob) for the current session.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no session is active.</exception>
    void ClearAllAliases();
}
