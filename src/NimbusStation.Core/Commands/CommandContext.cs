using NimbusStation.Core.Session;

namespace NimbusStation.Core.Commands;

/// <summary>
/// Provides context for command execution, including the current session state.
/// </summary>
public sealed class CommandContext
{
    /// <summary>
    /// Gets or sets the currently active session, if any.
    /// </summary>
    public Session.Session? CurrentSession { get; set; }

    /// <summary>
    /// Gets a value indicating whether a session is currently active.
    /// </summary>
    public bool HasActiveSession => CurrentSession is not null;

    /// <summary>
    /// Creates a new command context with no active session.
    /// </summary>
    public CommandContext()
    {
    }

    /// <summary>
    /// Creates a new command context with the specified session.
    /// </summary>
    /// <param name="session">The current session.</param>
    public CommandContext(Session.Session? session)
    {
        CurrentSession = session;
    }
}
