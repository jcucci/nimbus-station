namespace NimbusStation.Core.ShellPiping;

/// <summary>
/// Parses command strings into executable and arguments.
/// </summary>
public static class CommandParser
{
    /// <summary>
    /// Parses a command string into executable and arguments.
    /// </summary>
    /// <param name="commandString">The full command string (e.g., "jq '.name'").</param>
    /// <returns>A tuple of (command, arguments) where arguments may be null.</returns>
    /// <remarks>
    /// <para>
    /// Handles quoted arguments correctly. The first unquoted whitespace separates
    /// the command from arguments. Everything after is preserved as-is.
    /// </para>
    /// <para>Examples:</para>
    /// <list type="bullet">
    ///   <item><c>jq</c> → <c>("jq", null)</c></item>
    ///   <item><c>jq .</c> → <c>("jq", ".")</c></item>
    ///   <item><c>jq '.name'</c> → <c>("jq", "'.name'")</c></item>
    ///   <item><c>grep -i "hello world"</c> → <c>("grep", "-i \"hello world\"")</c></item>
    /// </list>
    /// </remarks>
    public static (string Command, string? Arguments) Parse(string? commandString)
    {
        if (string.IsNullOrWhiteSpace(commandString))
            return ("", null);

        var trimmed = commandString.Trim();
        var firstSpace = -1;
        var inQuote = false;
        var quoteChar = '\0';

        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];

            if (inQuote)
            {
                if (c == quoteChar)
                    inQuote = false;
                continue;
            }

            if (c is '"' or '\'')
            {
                inQuote = true;
                quoteChar = c;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                firstSpace = i;
                break;
            }
        }

        if (firstSpace == -1)
            return (trimmed, null);

        var command = trimmed[..firstSpace];
        var arguments = trimmed[(firstSpace + 1)..].TrimStart();

        return (command, string.IsNullOrEmpty(arguments) ? null : arguments);
    }
}
