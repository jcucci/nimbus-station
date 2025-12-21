using System.Reflection;
using Spectre.Console;

namespace NimbusStation.Cli;

/// <summary>
/// Entry point for the Nimbus Station CLI.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
        {
            PrintVersion();
            return;
        }

        PrintBanner();
    }

    private static void PrintBanner()
    {
        AnsiConsole.Write(
            new FigletText("Nimbus Station")
                .LeftJustified()
                .Color(Color.Cyan1));

        AnsiConsole.MarkupLine($"[grey]Cloud-agnostic investigation workbench[/] [dim]v{GetVersion()}[/]");
        AnsiConsole.WriteLine();
    }

    private static void PrintVersion()
    {
        AnsiConsole.MarkupLine($"[cyan]ns[/] [yellow]{GetVersion()}[/]");
    }

    /// <summary>
    /// Gets the application version from the assembly's informational version attribute.
    /// </summary>
    /// <returns>The semantic version string.</returns>
    public static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        // Strip the source revision hash if present (e.g., "0.1.0-alpha+abc123" -> "0.1.0-alpha")
        if (informationalVersion is not null)
        {
            int plusIndex = informationalVersion.IndexOf('+');
            if (plusIndex > 0)
            {
                return informationalVersion[..plusIndex];
            }
            return informationalVersion;
        }

        // Fallback to assembly version
        return assembly.GetName().Version?.ToString() ?? "unknown";
    }
}
