using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NimbusStation.Cli.Commands;
using NimbusStation.Cli.Output;
using NimbusStation.Cli.Repl;
using NimbusStation.Core.Aliases;
using NimbusStation.Core.Errors;
using NimbusStation.Core.Options;
using NimbusStation.Core.Session;
using NimbusStation.Core.ShellPiping;
using NimbusStation.Infrastructure.Aliases;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Infrastructure.Sessions;
using NimbusStation.Infrastructure.ShellPiping;
using NimbusStation.Providers.Azure.Auth;
using NimbusStation.Providers.Azure.Blob;
using NimbusStation.Providers.Azure.Cli;
using NimbusStation.Providers.Azure.Cosmos;
using Spectre.Console;

namespace NimbusStation.Cli;

/// <summary>
/// Entry point for the Nimbus Station CLI.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Handle --version first (before parsing other options)
        if (args.Length > 0 && args[0] is "--version" or "-v")
        {
            PrintVersion();
            return ExitCodes.Success;
        }

        // Parse global options (--verbose, --quiet, --no-color, --yes)
        var (globalOptions, _) = GlobalOptions.Parse(args);

        // Apply --no-color setting before any console output
        if (globalOptions.NoColor)
            AnsiConsole.Profile.Capabilities.Ansi = false;

        var services = ConfigureServices(globalOptions);

        // Load configuration and print themed banner (unless quiet mode)
        var configService = services.GetRequiredService<IConfigurationService>();
        await configService.LoadConfigurationAsync();

        if (!globalOptions.Quiet)
            BannerPrinter.Print(configService.GetTheme(), GetVersion());

        var repl = services.GetRequiredService<ReplLoop>();

        using var cts = new CancellationTokenSource();

        // Note: We don't set up Console.CancelKeyPress here because ReadLine
        // handles Ctrl+C internally by returning null, which ReplLoop handles.
        // Setting e.Cancel = true would interfere with ReadLine's Ctrl+C handling.

        var exitCode = await repl.RunAsync(cts.Token);
        return exitCode;
    }

    private static ServiceProvider ConfigureServices(GlobalOptions globalOptions)
    {
        var services = new ServiceCollection();

        // Register global options
        services.AddSingleton(globalOptions);

        // Logging - use Debug level if verbose, Warning otherwise
        var logLevel = globalOptions.Verbose ? LogLevel.Debug : LogLevel.Warning;
        services.AddLogging(builder => builder
            .SetMinimumLevel(logLevel)
            .AddConsole());

        // Core services
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<ISessionStateManager, SessionStateManager>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Alias services
        services.AddSingleton<IAliasService, AliasService>();
        services.AddSingleton<IAliasResolver, AliasResolver>();

        // Shell piping services
        services.AddSingleton<IExternalProcessExecutor, ExternalProcessExecutor>();
        services.AddSingleton<IShellDelegator, ShellDelegator>();
        services.AddSingleton<IPipelineExecutor, PipelineExecutor>();

        // Azure services
        services.AddSingleton<IAzureCliExecutor, AzureCliExecutor>();
        services.AddSingleton<IAzureAuthService, AzureAuthService>();
        services.AddSingleton<ICosmosService, CosmosService>();
        services.AddSingleton<IBlobService, BlobService>();

        // Commands
        services.AddSingleton<SessionCommand>();
        services.AddSingleton<AuthCommand>();
        services.AddSingleton<AliasCommand>();
        services.AddSingleton<UseCommand>();
        services.AddSingleton<InfoCommand>();
        services.AddSingleton<ThemeCommand>();
        services.AddSingleton<CosmosCommand>();
        services.AddSingleton<BlobCommand>();
        services.AddSingleton<BrowseCommand>();
        services.AddSingleton<ExitCommand>();

        // CommandRegistry and HelpCommand have a circular dependency - use Func to defer resolution
        services.AddSingleton<Func<CommandRegistry>>(sp => () => sp.GetRequiredService<CommandRegistry>());
        services.AddSingleton<HelpCommand>();

        services.AddSingleton<CommandRegistry>(sp =>
        {
            var registry = new CommandRegistry();
            registry.Register(sp.GetRequiredService<SessionCommand>());
            registry.Register(sp.GetRequiredService<AliasCommand>());
            registry.Register(sp.GetRequiredService<UseCommand>());
            registry.Register(sp.GetRequiredService<InfoCommand>());
            registry.Register(sp.GetRequiredService<ThemeCommand>());
            registry.Register(sp.GetRequiredService<AuthCommand>());
            registry.Register(sp.GetRequiredService<CosmosCommand>());
            registry.Register(sp.GetRequiredService<BlobCommand>());
            registry.Register(sp.GetRequiredService<BrowseCommand>());
            registry.Register(sp.GetRequiredService<HelpCommand>());
            registry.Register(sp.GetRequiredService<ExitCommand>());
            return registry;
        });

        // REPL
        services.AddSingleton<ReplLoop>();

        return services.BuildServiceProvider();
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
