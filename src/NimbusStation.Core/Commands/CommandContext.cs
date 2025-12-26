using NimbusStation.Core.Output;
using NimbusStation.Core.Session;

namespace NimbusStation.Core.Commands;

/// <summary>
/// Provides context for command execution, including read-only access to the current session
/// and output writer for producing command output.
/// </summary>
public sealed class CommandContext
{
    private readonly ISessionStateManager _sessionStateManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    /// <param name="sessionStateManager">The session state manager for reading current session state.</param>
    /// <param name="output">The output writer for command results.</param>
    public CommandContext(ISessionStateManager sessionStateManager, IOutputWriter output)
    {
        _sessionStateManager = sessionStateManager ?? throw new ArgumentNullException(nameof(sessionStateManager));
        Output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// Gets the currently active session, if any.
    /// </summary>
    public Session.Session? CurrentSession => _sessionStateManager.CurrentSession;

    /// <summary>
    /// Gets a value indicating whether a session is currently active.
    /// </summary>
    public bool HasActiveSession => _sessionStateManager.HasActiveSession;

    /// <summary>
    /// Gets the output writer for command results.
    /// </summary>
    public IOutputWriter Output { get; }
}
