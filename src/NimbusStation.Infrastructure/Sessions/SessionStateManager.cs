using NimbusStation.Core.Session;

namespace NimbusStation.Infrastructure.Sessions;

/// <summary>
/// Manages the current active session state during REPL execution.
/// This is an in-memory state manager; persistence is handled separately by ISessionService.
/// </summary>
public sealed class SessionStateManager : ISessionStateManager
{
    private Session? _currentSession;

    /// <inheritdoc/>
    public Session? CurrentSession => _currentSession;

    /// <inheritdoc/>
    public bool HasActiveSession => _currentSession is not null;

    /// <inheritdoc/>
    public void ActivateSession(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _currentSession = session;
    }

    /// <inheritdoc/>
    public void DeactivateSession() => _currentSession = null;

    /// <inheritdoc/>
    public void SetCosmosAlias(string aliasName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aliasName);
        var session = GetActiveSessionOrThrow();
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = currentContext with { ActiveCosmosAlias = aliasName };
        _currentSession = session.WithContext(newContext);
    }

    /// <inheritdoc/>
    public void SetBlobAlias(string aliasName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aliasName);
        var session = GetActiveSessionOrThrow();
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = currentContext with { ActiveBlobAlias = aliasName };
        _currentSession = session.WithContext(newContext);
    }

    /// <inheritdoc/>
    public void ClearCosmosAlias()
    {
        var session = GetActiveSessionOrThrow();
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = currentContext with { ActiveCosmosAlias = null };
        _currentSession = session.WithContext(newContext);
    }

    /// <inheritdoc/>
    public void ClearBlobAlias()
    {
        var session = GetActiveSessionOrThrow();
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = currentContext with { ActiveBlobAlias = null };
        _currentSession = session.WithContext(newContext);
    }

    /// <inheritdoc/>
    public void SetStorageAlias(string aliasName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aliasName);
        var session = GetActiveSessionOrThrow();
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = currentContext with { ActiveStorageAlias = aliasName };
        _currentSession = session.WithContext(newContext);
    }

    /// <inheritdoc/>
    public void ClearStorageAlias()
    {
        var session = GetActiveSessionOrThrow();
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = currentContext with { ActiveStorageAlias = null };
        _currentSession = session.WithContext(newContext);
    }

    /// <inheritdoc/>
    public void ClearAllAliases()
    {
        var session = GetActiveSessionOrThrow();
        _currentSession = session.WithContext(SessionContext.Empty);
    }

    private Session GetActiveSessionOrThrow() =>
        _currentSession ?? throw new InvalidOperationException("No active session. Use 'session start <name>' to begin.");
}
