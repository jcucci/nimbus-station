using System.Text;

namespace NimbusStation.Infrastructure.ShellPiping;

/// <summary>
/// Provides platform-specific escaping for shell command strings.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Security-critical:</strong> This class prevents shell injection attacks when
/// delegating commands to system shells. All user-provided command segments must be
/// escaped before being passed to the shell.
/// </para>
/// <para>
/// On Unix, commands are wrapped in single quotes with embedded single quotes escaped.
/// On Windows (PowerShell), commands use single quotes with embedded single quotes doubled.
/// </para>
/// </remarks>
public static class ShellEscaper
{
    /// <summary>
    /// Escapes a command string for safe execution on the current platform's shell.
    /// </summary>
    /// <param name="command">The command string to escape.</param>
    /// <returns>The escaped command string safe for shell execution.</returns>
    public static string Escape(string command)
    {
        if (PlatformHelper.IsWindows)
            return EscapeForPowerShell(command);

        return EscapeForUnixShell(command);
    }

    /// <summary>
    /// Escapes a command string for safe execution via <c>/bin/sh -c</c>.
    /// </summary>
    /// <param name="command">The command string to escape.</param>
    /// <returns>The escaped command string wrapped in single quotes.</returns>
    /// <remarks>
    /// <para>
    /// Uses single-quote wrapping which treats all characters literally except for
    /// single quotes themselves. Embedded single quotes are escaped using the
    /// <c>'\''</c> pattern (end quote, escaped quote, start quote).
    /// </para>
    /// <para>
    /// Example: <c>echo 'hello'</c> becomes <c>'echo '\''hello'\'''</c>
    /// </para>
    /// </remarks>
    public static string EscapeForUnixShell(string command)
    {
        if (string.IsNullOrEmpty(command))
            return "''";

        // Single-quote escaping: wrap in single quotes, escape embedded ' as '\''
        // This is the safest approach as single quotes treat everything literally
        var escaped = command.Replace("'", @"'\''");
        return $"'{escaped}'";
    }

    /// <summary>
    /// Escapes a command string for safe execution via PowerShell.
    /// </summary>
    /// <param name="command">The command string to escape.</param>
    /// <returns>The escaped command string safe for PowerShell execution.</returns>
    /// <remarks>
    /// <para>
    /// Uses single-quote wrapping (literal strings in PowerShell). Embedded single
    /// quotes are escaped by doubling them (<c>''</c>).
    /// </para>
    /// <para>
    /// Example: <c>echo 'hello'</c> becomes <c>'echo ''hello'''</c>
    /// </para>
    /// </remarks>
    public static string EscapeForPowerShell(string command)
    {
        if (string.IsNullOrEmpty(command))
            return "''";

        // PowerShell single-quote escaping: embedded ' becomes ''
        var escaped = command.Replace("'", "''");
        return $"'{escaped}'";
    }

    /// <summary>
    /// Builds a complete pipeline command string from multiple command segments.
    /// </summary>
    /// <param name="commands">The command segments to join with pipes.</param>
    /// <returns>A single command string with segments joined by <c>|</c>.</returns>
    /// <remarks>
    /// The individual commands are NOT escaped by this method. This builds the
    /// raw pipeline string that will be passed to the shell. The entire pipeline
    /// should be escaped as a unit if needed.
    /// </remarks>
    public static string BuildPipelineCommand(IReadOnlyList<string> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        if (commands.Count == 0)
            return string.Empty;

        if (commands.Count == 1)
            return commands[0];

        return string.Join(" | ", commands);
    }
}
