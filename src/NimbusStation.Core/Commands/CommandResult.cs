namespace NimbusStation.Core.Commands;

/// <summary>
/// Represents the result of executing a command.
/// </summary>
/// <param name="Success">Whether the command executed successfully.</param>
/// <param name="Message">An optional message to display to the user.</param>
/// <param name="Data">Optional data returned by the command.</param>
/// <param name="IsExitSignal">Whether this result signals the REPL should exit.</param>
public sealed record CommandResult(
    bool Success,
    string? Message = null,
    object? Data = null,
    bool IsExitSignal = false)
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
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A failed command result.</returns>
    public static CommandResult Error(string message) => new(Success: false, Message: message);

    /// <summary>
    /// Creates a result that signals the REPL to exit.
    /// </summary>
    /// <param name="message">The exit message to display.</param>
    /// <returns>A command result that signals exit.</returns>
    public static CommandResult Exit(string? message = "Goodbye!") =>
        new(Success: true, Message: message, IsExitSignal: true);

    /// <summary>
    /// Creates a result that signals the REPL to exit with a specific exit code.
    /// </summary>
    /// <param name="exitCode">The exit code to return.</param>
    /// <param name="message">The exit message to display.</param>
    /// <returns>A command result that signals exit with the specified code.</returns>
    public static CommandResult Exit(int exitCode, string? message = "Goodbye!") =>
        new(Success: true, Message: message, Data: exitCode, IsExitSignal: true);
}
