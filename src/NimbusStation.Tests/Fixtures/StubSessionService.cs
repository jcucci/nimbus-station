using NimbusStation.Core.Session;

namespace NimbusStation.Tests.Fixtures;

/// <summary>
/// Stub implementation of <see cref="ISessionService"/> for testing.
/// </summary>
public sealed class StubSessionService : ISessionService
{
    public Session? CurrentSession { get; set; }

    public Task<Session> StartSessionAsync(string sessionName, CancellationToken cancellationToken = default)
        => Task.FromResult(CurrentSession ?? Session.Create(sessionName));

    public Task<IReadOnlyList<Session>> ListSessionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Session>>(CurrentSession is not null ? [CurrentSession] : []);

    public Task<Session> ResumeSessionAsync(string sessionName, CancellationToken cancellationToken = default)
        => Task.FromResult(CurrentSession ?? throw new SessionNotFoundException(sessionName));

    public Task DeleteSessionAsync(string sessionName, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<Session> UpdateSessionContextAsync(string sessionName, SessionContext context, CancellationToken cancellationToken = default)
    {
        if (CurrentSession is null)
            throw new SessionNotFoundException(sessionName);

        CurrentSession = CurrentSession.WithContext(context);
        return Task.FromResult(CurrentSession);
    }

    public bool SessionExists(string sessionName) => CurrentSession?.TicketId == sessionName;

    public string GetSessionDirectory(string sessionName) => $"/tmp/nimbus/{sessionName}";

    public string GetDownloadsDirectory(string sessionName) => $"/tmp/nimbus/{sessionName}/downloads";

    public string GetQueriesDirectory(string sessionName) => $"/tmp/nimbus/{sessionName}/queries";
}
