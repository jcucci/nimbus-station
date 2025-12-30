namespace NimbusStation.Core.Options;

/// <summary>
/// Global CLI options that affect all commands.
/// </summary>
public class GlobalOptions
{
    /// <summary>
    /// Enable verbose/debug output.
    /// </summary>
    public bool Verbose { get; init; }

    /// <summary>
    /// Suppress non-essential output (just data, no chrome).
    /// </summary>
    public bool Quiet { get; init; }

    /// <summary>
    /// Disable ANSI color codes in output.
    /// </summary>
    public bool NoColor { get; init; }

    /// <summary>
    /// Skip all confirmation prompts (auto-yes).
    /// </summary>
    public bool YesToAll { get; init; }

    /// <summary>
    /// Default options (all flags disabled).
    /// </summary>
    public static GlobalOptions Default => new();

    /// <summary>
    /// Parses global options from command line arguments.
    /// Returns the options and the remaining arguments (with global flags removed).
    /// </summary>
    public static (GlobalOptions Options, string[] RemainingArgs) Parse(string[] args)
    {
        var verbose = false;
        var quiet = false;
        var noColor = false;
        var yesToAll = false;
        var remaining = new List<string>();

        foreach (var arg in args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "--verbose":
                    verbose = true;
                    break;
                case "--quiet":
                    quiet = true;
                    break;
                case "--no-color":
                    noColor = true;
                    break;
                case "--yes":
                    yesToAll = true;
                    break;
                default:
                    remaining.Add(arg);
                    break;
            }
        }

        var options = new GlobalOptions
        {
            Verbose = verbose,
            Quiet = quiet,
            NoColor = noColor,
            YesToAll = yesToAll
        };

        return (options, remaining.ToArray());
    }
}
