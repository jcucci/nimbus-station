using NimbusStation.Core.Session;

namespace NimbusStation.Tests.Fixtures;

/// <summary>
/// A stub implementation of ISessionStateManager for testing.
/// Provides full functionality for unit tests without external dependencies.
/// </summary>
public sealed class StubSessionStateManager : ISessionStateManager
{
    private Session? _currentSession;

    public Session? CurrentSession => _currentSession;

    public bool HasActiveSession => _currentSession is not null;

    public void ActivateSession(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _currentSession = session;
    }

    public void DeactivateSession() => _currentSession = null;

    public void SetCosmosAlias(string aliasName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aliasName);
        var session = GetActiveSessionOrThrow();
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = currentContext with { ActiveCosmosAlias = aliasName };
        _currentSession = session.WithContext(newContext);
    }

    public void SetBlobAlias(string aliasName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aliasName);
        var session = GetActiveSessionOrThrow();
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = currentContext with { ActiveBlobAlias = aliasName };
        _currentSession = session.WithContext(newContext);
    }

    public void ClearCosmosAlias()
    {
        var session = GetActiveSessionOrThrow();
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = currentContext with { ActiveCosmosAlias = null };
        _currentSession = session.WithContext(newContext);
    }

    public void ClearBlobAlias()
    {
        var session = GetActiveSessionOrThrow();
        var currentContext = session.ActiveContext ?? SessionContext.Empty;
        var newContext = currentContext with { ActiveBlobAlias = null };
        _currentSession = session.WithContext(newContext);
    }

    public void ClearAllAliases()
    {
        var session = GetActiveSessionOrThrow();
        _currentSession = session.WithContext(SessionContext.Empty);
    }

    private Session GetActiveSessionOrThrow() =>
        _currentSession ?? throw new InvalidOperationException("No active session.");
}
