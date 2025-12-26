using NimbusStation.Core.Session;

namespace NimbusStation.Tests.Fixtures;

/// <summary>
/// Stub implementation of <see cref="ISessionService"/> for testing.
/// </summary>
public sealed class StubSessionService : ISessionService
{
    public Task<Session> StartSessionAsync(string sessionName, CancellationToken cancellationToken = default)
        => Task.FromResult(Session.Create(sessionName));

    public Task<IReadOnlyList<Session>> ListSessionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Session>>([]);

    public Task<Session> ResumeSessionAsync(string sessionName, CancellationToken cancellationToken = default)
        => Task.FromResult(Session.Create(sessionName));

    public Task DeleteSessionAsync(string sessionName, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<Session> UpdateSessionContextAsync(string sessionName, SessionContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(Session.Create(sessionName).WithContext(context));

    public bool SessionExists(string sessionName) => true;

    public string GetSessionDirectory(string sessionName) => $"/tmp/nimbus/{sessionName}";

    public string GetDownloadsDirectory(string sessionName) => $"/tmp/nimbus/{sessionName}/downloads";

    public string GetQueriesDirectory(string sessionName) => $"/tmp/nimbus/{sessionName}/queries";
}
