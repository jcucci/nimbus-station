namespace NimbusStation.Infrastructure.ShellPiping;

/// <summary>
/// Provides platform-specific escaping for shell command strings.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Security-critical:</strong> This class prevents shell injection attacks when
/// delegating commands to system shells.
/// </para>
/// <para>
/// Two escaping strategies are provided:
/// <list type="bullet">
/// <item><description>Single-quote escaping (<see cref="EscapeForUnixShell"/>, <see cref="EscapeForPowerShell"/>):
/// For escaping individual arguments where no shell interpretation is desired.</description></item>
/// <item><description>Double-quote escaping (<see cref="EscapeForShellArgument"/>): For wrapping pipeline
/// commands passed to shell's -c flag where pipes should be interpreted.</description></item>
/// </list>
/// </para>
/// </remarks>
public static class ShellEscaper
{
    /// <summary>
    /// Escapes a command string for safe execution on the current platform's shell.
    /// Uses single-quote escaping which treats content literally.
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
    /// Uses single-quote wrapping which treats all characters literally except for
    /// single quotes themselves. Embedded single quotes are escaped using the
    /// <c>'\''</c> pattern (end quote, escaped quote, start quote).
    /// </remarks>
    public static string EscapeForUnixShell(string command)
    {
        if (string.IsNullOrEmpty(command))
            return "''";

        var escaped = command.Replace("'", @"'\''");
        return $"'{escaped}'";
    }

    /// <summary>
    /// Escapes a command string for safe execution via PowerShell.
    /// </summary>
    /// <param name="command">The command string to escape.</param>
    /// <returns>The escaped command string safe for PowerShell execution.</returns>
    /// <remarks>
    /// Uses single-quote wrapping (literal strings in PowerShell). Embedded single
    /// quotes are escaped by doubling them (<c>''</c>).
    /// </remarks>
    public static string EscapeForPowerShell(string command)
    {
        if (string.IsNullOrEmpty(command))
            return "''";

        var escaped = command.Replace("'", "''");
        return $"'{escaped}'";
    }

    /// <summary>
    /// Escapes a pipeline command for passing as an argument to a shell's -c or -Command flag.
    /// </summary>
    /// <param name="command">The pipeline command string to escape.</param>
    /// <returns>The escaped command wrapped in double quotes.</returns>
    /// <remarks>
    /// <para>
    /// Uses double-quote wrapping to allow shell interpretation of pipes and other operators.
    /// Special characters that could cause injection are escaped:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Unix: backslash, double-quote, dollar sign, backtick, newline, carriage return</description></item>
    /// <item><description>Windows (PowerShell): backtick, double-quote, newline, carriage return, tab</description></item>
    /// </list>
    /// </remarks>
    public static string EscapeForShellArgument(string command)
    {
        if (PlatformHelper.IsWindows)
        {
            // PowerShell uses backtick as escape character
            var escaped = command
                .Replace("`", "``")
                .Replace("\"", "`\"")
                .Replace("\r", "`r")
                .Replace("\n", "`n")
                .Replace("\t", "`t");
            return $"\"{escaped}\"";
        }

        // Unix shell escaping - order is security-critical:
        // Backslashes must be escaped first so that backslashes introduced by
        // later replacements are not themselves re-escaped.
        return $"\"{command
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("$", "\\$")
            .Replace("`", "\\`")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")}\"";
    }

    /// <summary>
    /// Builds a complete pipeline command string from multiple command segments.
    /// </summary>
    /// <param name="commands">The command segments to join with pipes.</param>
    /// <returns>A single command string with segments joined by <c>|</c>.</returns>
    /// <remarks>
    /// The individual commands are NOT escaped by this method. This builds the
    /// raw pipeline string that will be passed to the shell.
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
