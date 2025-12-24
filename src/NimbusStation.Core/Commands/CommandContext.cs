using NimbusStation.Core.Output;
using NimbusStation.Core.Session;

namespace NimbusStation.Core.Commands;

/// <summary>
/// Provides immutable context for command execution, including the current session state
/// and output writer for producing command output.
/// </summary>
/// <param name="CurrentSession">The currently active session, if any.</param>
/// <param name="Output">The output writer for command results.</param>
public sealed record CommandContext(
    Session.Session? CurrentSession,
    IOutputWriter Output)
{
    /// <summary>
    /// Gets a value indicating whether a session is currently active.
    /// </summary>
    public bool HasActiveSession => CurrentSession is not null;

    /// <summary>
    /// Creates a new context with the specified session, preserving other properties.
    /// </summary>
    /// <param name="session">The new session value.</param>
    /// <returns>A new <see cref="CommandContext"/> with the updated session.</returns>
    public CommandContext WithSession(Session.Session? session) => this with { CurrentSession = session };

    /// <summary>
    /// Creates a new context with the specified output writer, preserving other properties.
    /// </summary>
    /// <param name="output">The new output writer.</param>
    /// <returns>A new <see cref="CommandContext"/> with the updated output writer.</returns>
    public CommandContext WithOutput(IOutputWriter output) => this with { Output = output };
}
