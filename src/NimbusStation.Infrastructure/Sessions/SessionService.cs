using Microsoft.Extensions.Logging;
using NimbusStation.Core.Session;

namespace NimbusStation.Infrastructure.Sessions;

/// <summary>
/// File-based implementation of the session service.
/// Manages sessions in ~/.nimbus/sessions/{session-name}/.
/// </summary>
public sealed class SessionService : ISessionService
{
    private const string SessionFileName = "session.json";
    private const string DownloadsDirName = "downloads";
    private const string QueriesDirName = "queries";

    private readonly ILogger<SessionService> _logger;
    private readonly SessionSerializer _serializer;
    private readonly string _sessionsRoot;

    /// <inheritdoc/>
    public Session? CurrentSession { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SessionService(ILogger<SessionService> logger)
        : this(logger, GetDefaultSessionsRoot())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionService"/> class with a custom sessions root.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionsRoot">The root directory for sessions.</param>
    public SessionService(ILogger<SessionService> logger, string sessionsRoot)
    {
        _logger = logger;
        _serializer = new SessionSerializer();
        _sessionsRoot = sessionsRoot;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This method is forgiving: if the session already exists, it will resume it instead of failing.
    /// This allows users to use "session start" without needing to remember if the session exists.
    /// </remarks>
    public async Task<Session> StartSessionAsync(string sessionName, CancellationToken cancellationToken = default)
    {
        SessionNameValidator.Validate(sessionName);

        var sessionDir = GetSessionDirectory(sessionName);
        var sessionFile = Path.Combine(sessionDir, SessionFileName);

        // If session already exists, resume it instead of failing
        if (Directory.Exists(sessionDir) && File.Exists(sessionFile))
        {
            _logger.LogInformation("Session '{SessionName}' already exists, resuming", sessionName);
            return await ResumeSessionAsync(sessionName, cancellationToken);
        }

        // Create session directory structure
        Directory.CreateDirectory(sessionDir);
        Directory.CreateDirectory(GetDownloadsDirectory(sessionName));
        Directory.CreateDirectory(GetQueriesDirectory(sessionName));

        // Create and persist session
        var session = Session.Create(sessionName);
        await _serializer.WriteSessionAsync(session, sessionFile, cancellationToken);

        _logger.LogInformation("Created session '{SessionName}' at {SessionDir}", sessionName, sessionDir);

        return session;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Session>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        EnsureSessionsRootExists();

        var sessions = new List<Session>();

        if (!Directory.Exists(_sessionsRoot))
        {
            return sessions;
        }

        foreach (var sessionDir in Directory.GetDirectories(_sessionsRoot))
        {
            var sessionFile = Path.Combine(sessionDir, SessionFileName);
            if (!File.Exists(sessionFile))
            {
                _logger.LogWarning("Session directory {SessionDir} has no session.json, skipping", sessionDir);
                continue;
            }

            try
            {
                var session = await _serializer.ReadSessionAsync(sessionFile, cancellationToken);
                sessions.Add(session);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read session from {SessionFile}, skipping", sessionFile);
            }
        }

        return sessions.OrderByDescending(s => s.LastAccessedAt).ToList();
    }

    /// <inheritdoc/>
    public async Task<Session> ResumeSessionAsync(string sessionName, CancellationToken cancellationToken = default)
    {
        var sessionDir = GetSessionDirectory(sessionName);
        var sessionFile = Path.Combine(sessionDir, SessionFileName);

        if (!Directory.Exists(sessionDir) || !File.Exists(sessionFile))
        {
            throw new SessionNotFoundException(sessionName);
        }

        // Read, touch, and persist
        var session = await _serializer.ReadSessionAsync(sessionFile, cancellationToken);
        session = session.Touch();
        await _serializer.WriteSessionAsync(session, sessionFile, cancellationToken);

        _logger.LogInformation("Resumed session '{SessionName}'", sessionName);

        return session;
    }

    /// <inheritdoc/>
    public Task DeleteSessionAsync(string sessionName, CancellationToken cancellationToken = default)
    {
        var sessionDir = GetSessionDirectory(sessionName);

        if (!Directory.Exists(sessionDir))
        {
            throw new SessionNotFoundException(sessionName);
        }

        Directory.Delete(sessionDir, recursive: true);
        _logger.LogInformation("Deleted session '{SessionName}'", sessionName);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<Session> UpdateSessionContextAsync(string sessionName, SessionContext context, CancellationToken cancellationToken = default)
    {
        var sessionDir = GetSessionDirectory(sessionName);
        var sessionFile = Path.Combine(sessionDir, SessionFileName);

        if (!Directory.Exists(sessionDir) || !File.Exists(sessionFile))
        {
            throw new SessionNotFoundException(sessionName);
        }

        var session = await _serializer.ReadSessionAsync(sessionFile, cancellationToken);
        session = session.WithContext(context).Touch();
        await _serializer.WriteSessionAsync(session, sessionFile, cancellationToken);

        _logger.LogDebug("Updated context for session '{SessionName}'", sessionName);

        return session;
    }

    /// <inheritdoc/>
    public bool SessionExists(string sessionName)
    {
        var sessionDir = GetSessionDirectory(sessionName);
        var sessionFile = Path.Combine(sessionDir, SessionFileName);
        return Directory.Exists(sessionDir) && File.Exists(sessionFile);
    }

    /// <inheritdoc/>
    public string GetSessionDirectory(string sessionName)
    {
        return Path.Combine(_sessionsRoot, sessionName);
    }

    /// <inheritdoc/>
    public string GetDownloadsDirectory(string sessionName)
    {
        return Path.Combine(GetSessionDirectory(sessionName), DownloadsDirName);
    }

    /// <inheritdoc/>
    public string GetQueriesDirectory(string sessionName)
    {
        return Path.Combine(GetSessionDirectory(sessionName), QueriesDirName);
    }

    private static string GetDefaultSessionsRoot()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".nimbus", "sessions");
    }

    private void EnsureSessionsRootExists()
    {
        if (!Directory.Exists(_sessionsRoot))
        {
            Directory.CreateDirectory(_sessionsRoot);
            _logger.LogDebug("Created sessions root directory at {SessionsRoot}", _sessionsRoot);
        }
    }
}
