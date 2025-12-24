using NimbusStation.Core.Output;
using NimbusStation.Core.Session;

namespace NimbusStation.Core.Commands;

/// <summary>
/// Provides context for command execution, including read-only access to the current session
/// and output writer for producing command output.
/// </summary>
public sealed class CommandContext
{
    private readonly ISessionService _sessionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    /// <param name="sessionService">The session service for reading current session state.</param>
    /// <param name="output">The output writer for command results.</param>
    public CommandContext(ISessionService sessionService, IOutputWriter output)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        Output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// Gets the currently active session, if any.
    /// </summary>
    public Session.Session? CurrentSession => _sessionService.CurrentSession;

    /// <summary>
    /// Gets a value indicating whether a session is currently active.
    /// </summary>
    public bool HasActiveSession => CurrentSession is not null;

    /// <summary>
    /// Gets the output writer for command results.
    /// </summary>
    public IOutputWriter Output { get; }
}
