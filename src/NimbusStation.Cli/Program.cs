using Spectre.Console;

namespace NimbusStation.Cli;

/// <summary>
/// Entry point for the Nimbus Station CLI.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        // TODO: Parse and handle command-line arguments in a future version.
        _ = args;

        AnsiConsole.Write(
            new FigletText("Nimbus Station")
                .LeftJustified()
                .Color(Color.Cyan1));

        AnsiConsole.MarkupLine("[grey]Cloud-agnostic investigation workbench[/]");
        AnsiConsole.WriteLine();
    }
}
