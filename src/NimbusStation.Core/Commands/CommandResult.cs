using NimbusStation.Core.Session;

namespace NimbusStation.Core.Commands;

/// <summary>
/// Represents the result of executing a command.
/// </summary>
/// <param name="Success">Whether the command executed successfully.</param>
/// <param name="Message">An optional message to display to the user.</param>
/// <param name="Data">Optional data returned by the command.</param>
/// <param name="NewSession">
/// The new session state after command execution, if the command modified the session.
/// A value of <see cref="SessionChange.None"/> indicates no change.
/// </param>
public sealed record CommandResult(
    bool Success,
    string? Message = null,
    object? Data = null,
    SessionChange? NewSession = null)
{
    /// <summary>
    /// Creates a successful result with an optional message.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A successful command result.</returns>
    public static CommandResult Ok(string? message = null) => new(Success: true, Message: message);

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <param name="message">An optional message.</param>
    /// <returns>A successful command result with data.</returns>
    public static CommandResult Ok(object data, string? message = null) => new(Success: true, Message: message, Data: data);

    /// <summary>
    /// Creates a successful result that also updates the current session.
    /// </summary>
    /// <param name="session">The new session (or null to clear the session).</param>
    /// <returns>A successful command result that signals a session change.</returns>
    public static CommandResult OkWithSession(Session.Session? session) =>
        new(Success: true, NewSession: new SessionChange(session));

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A failed command result.</returns>
    public static CommandResult Error(string message) => new(Success: false, Message: message);
}

/// <summary>
/// Represents a session change requested by a command.
/// Wrapping in a record allows distinguishing between "no change" (null) and "clear session" (value with null Session).
/// </summary>
/// <param name="Session">The new session value, or null to clear the session.</param>
public sealed record SessionChange(Session.Session? Session);
