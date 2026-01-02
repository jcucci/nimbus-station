using NimbusStation.Core.Commands;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Providers.Azure.Auth;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for managing Azure authentication (status, login).
/// </summary>
public sealed class AuthCommand : ICommand
{
    private readonly IAzureAuthService _authService;
    private readonly IConfigurationService _configurationService;

    private static readonly HashSet<string> _subcommands = ["status", "login"];

    /// <inheritdoc/>
    public string Name => "auth";

    /// <inheritdoc/>
    public string Description => "Manage Azure authentication";

    /// <inheritdoc/>
    public string Usage => "auth <status|login>";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands => _subcommands;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthCommand"/> class.
    /// </summary>
    /// <param name="authService">The Azure authentication service.</param>
    /// <param name="configurationService">The configuration service for theme settings.</param>
    public AuthCommand(IAzureAuthService authService, IConfigurationService configurationService)
    {
        _authService = authService;
        _configurationService = configurationService;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0)
            return await HandleStatusAsync(context, cancellationToken);

        var subcommand = args[0].ToLowerInvariant();

        return subcommand switch
        {
            "status" => await HandleStatusAsync(context, cancellationToken),
            "login" => await HandleLoginAsync(context, cancellationToken),
            _ => CommandResult.Error($"Unknown subcommand '{subcommand}'. Usage: {Usage}")
        };
    }

    private async Task<CommandResult> HandleStatusAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var status = await _authService.GetStatusAsync(cancellationToken);
        var theme = _configurationService.GetTheme();

        if (!status.IsCliInstalled)
        {
            context.Output.WriteLine($"[{theme.ErrorColor}]Azure CLI is not installed.[/]");
            context.Output.WriteLine($"[{theme.DimColor}]Install from: https://aka.ms/installazurecli[/]");
            return CommandResult.Error("Azure CLI not found");
        }

        if (!status.IsAuthenticated)
        {
            var panel = new Panel(new Rows(
                new Markup($"[{theme.WarningColor}]Not authenticated[/]"),
                new Markup($"[{theme.DimColor}]Run 'auth login' to authenticate with Azure.[/]"),
                new Markup($"[{theme.DimColor}]CLI Version: {status.CliVersion ?? "unknown"}[/]")
            ))
            {
                Header = new PanelHeader($"[bold {theme.WarningColor}]Azure Authentication[/]"),
                Border = BoxBorder.Rounded
            };

            context.Output.WriteRenderable(panel);
            return CommandResult.Ok(status);
        }

        var authenticatedPanel = new Panel(new Rows(
            new Markup($"[bold]Identity:[/] [{theme.PromptSessionColor}]{status.Identity}[/]"),
            new Markup($"[bold]Subscription:[/] {status.SubscriptionName}"),
            new Markup($"[bold]Subscription ID:[/] [{theme.DimColor}]{status.SubscriptionId}[/]"),
            new Markup($"[bold]Tenant ID:[/] [{theme.DimColor}]{status.TenantId}[/]"),
            new Markup($"[bold]CLI Version:[/] [{theme.DimColor}]{status.CliVersion}[/]")
        ))
        {
            Header = new PanelHeader($"[bold {theme.SuccessColor}]Azure Authentication[/]"),
            Border = BoxBorder.Rounded
        };

        context.Output.WriteRenderable(authenticatedPanel);
        return CommandResult.Ok(status);
    }

    private async Task<CommandResult> HandleLoginAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var theme = _configurationService.GetTheme();
        var isInstalled = await _authService.IsCliInstalledAsync();

        if (!isInstalled)
        {
            context.Output.WriteLine($"[{theme.ErrorColor}]Azure CLI is not installed.[/]");
            context.Output.WriteLine($"[{theme.DimColor}]Install from: https://aka.ms/installazurecli[/]");
            return CommandResult.Error("Azure CLI not found");
        }

        context.Output.WriteLine($"[{theme.DimColor}]Opening browser for Azure login...[/]");

        var status = await _authService.LoginAsync(cancellationToken);

        if (!status.IsAuthenticated)
        {
            context.Output.WriteLine($"[{theme.ErrorColor}]Login failed:[/] {status.ErrorMessage}");
            return CommandResult.Error(status.ErrorMessage ?? "Login failed");
        }

        context.Output.WriteLine($"[{theme.SuccessColor}]Successfully authenticated as[/] [{theme.PromptSessionColor}]{status.Identity}[/]");
        context.Output.WriteLine($"[{theme.DimColor}]Subscription: {status.SubscriptionName}[/]");

        return CommandResult.Ok(status);
    }
}
