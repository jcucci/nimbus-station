using NimbusStation.Core.Options;
using NimbusStation.Core.Output;
using NimbusStation.Core.Session;

namespace NimbusStation.Core.Commands;

/// <summary>
/// Provides context for command execution, including read-only access to the current session,
/// output writer for producing command output, and global CLI options.
/// </summary>
public sealed class CommandContext
{
    private readonly ISessionStateManager _sessionStateManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    /// <param name="sessionStateManager">The session state manager for reading current session state.</param>
    /// <param name="output">The output writer for command results.</param>
    /// <param name="options">The global CLI options.</param>
    public CommandContext(ISessionStateManager sessionStateManager, IOutputWriter output, GlobalOptions? options = null)
    {
        _sessionStateManager = sessionStateManager ?? throw new ArgumentNullException(nameof(sessionStateManager));
        Output = output ?? throw new ArgumentNullException(nameof(output));
        Options = options ?? GlobalOptions.Default;
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

    /// <summary>
    /// Gets the global CLI options.
    /// </summary>
    public GlobalOptions Options { get; }
}
