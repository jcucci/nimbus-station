using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NimbusStation.Cli.Commands;
using NimbusStation.Cli.Repl;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Sessions;
using Spectre.Console;

namespace NimbusStation.Cli;

/// <summary>
/// Entry point for the Nimbus Station CLI.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] is "--version" or "-v")
        {
            PrintVersion();
            return;
        }

        PrintBanner();

        var services = ConfigureServices();
        var repl = services.GetRequiredService<ReplLoop>();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        await repl.RunAsync(cts.Token);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Warning)
            .AddConsole());

        // Core services
        services.AddSingleton<ISessionService, SessionService>();

        // Commands
        services.AddSingleton<SessionCommand>();
        services.AddSingleton<CommandRegistry>(sp =>
        {
            var registry = new CommandRegistry();
            registry.Register(sp.GetRequiredService<SessionCommand>());
            return registry;
        });

        // REPL
        services.AddSingleton<ReplLoop>();

        return services.BuildServiceProvider();
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

    private static void PrintVersion() =>
        AnsiConsole.MarkupLine($"[cyan]ns[/] [yellow]{GetVersion()}[/]");

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

        if (informationalVersion is null)
            return assembly.GetName().Version?.ToString() ?? "unknown";

        var plusIndex = informationalVersion.IndexOf('+');
        return plusIndex > 0 ? informationalVersion[..plusIndex] : informationalVersion;
    }
}
