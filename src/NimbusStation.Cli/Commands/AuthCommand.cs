using NimbusStation.Core.Commands;
using NimbusStation.Providers.Azure.Auth;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for managing Azure authentication (status, login).
/// </summary>
public sealed class AuthCommand : ICommand
{
    private readonly IAzureAuthService _authService;

    private static readonly HashSet<string> _subcommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "status", "login"
    };

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
    public AuthCommand(IAzureAuthService authService)
    {
        _authService = authService;
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

        if (!status.IsCliInstalled)
        {
            context.Output.WriteLine("[red]Azure CLI is not installed.[/]");
            context.Output.WriteLine("[dim]Install from: https://aka.ms/installazurecli[/]");
            return CommandResult.Error("Azure CLI not found");
        }

        if (!status.IsAuthenticated)
        {
            var panel = new Panel(new Rows(
                new Markup("[yellow]Not authenticated[/]"),
                new Markup("[dim]Run 'auth login' to authenticate with Azure.[/]"),
                new Markup($"[dim]CLI Version: {status.CliVersion ?? "unknown"}[/]")
            ))
            {
                Header = new PanelHeader("[bold yellow]Azure Authentication[/]"),
                Border = BoxBorder.Rounded
            };

            context.Output.WriteRenderable(panel);
            return CommandResult.Ok(status);
        }

        var authenticatedPanel = new Panel(new Rows(
            new Markup($"[bold]Identity:[/] [cyan]{status.Identity}[/]"),
            new Markup($"[bold]Subscription:[/] {status.SubscriptionName}"),
            new Markup($"[bold]Subscription ID:[/] [dim]{status.SubscriptionId}[/]"),
            new Markup($"[bold]Tenant ID:[/] [dim]{status.TenantId}[/]"),
            new Markup($"[bold]CLI Version:[/] [dim]{status.CliVersion}[/]")
        ))
        {
            Header = new PanelHeader("[bold green]Azure Authentication[/]"),
            Border = BoxBorder.Rounded
        };

        context.Output.WriteRenderable(authenticatedPanel);
        return CommandResult.Ok(status);
    }

    private async Task<CommandResult> HandleLoginAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var isInstalled = await _authService.IsCliInstalledAsync();

        if (!isInstalled)
        {
            context.Output.WriteLine("[red]Azure CLI is not installed.[/]");
            context.Output.WriteLine("[dim]Install from: https://aka.ms/installazurecli[/]");
            return CommandResult.Error("Azure CLI not found");
        }

        context.Output.WriteLine("[dim]Opening browser for Azure login...[/]");

        var status = await _authService.LoginAsync(cancellationToken);

        if (!status.IsAuthenticated)
        {
            context.Output.WriteLine($"[red]Login failed:[/] {status.ErrorMessage}");
            return CommandResult.Error(status.ErrorMessage ?? "Login failed");
        }

        context.Output.WriteLine($"[green]Successfully authenticated as[/] [cyan]{status.Identity}[/]");
        context.Output.WriteLine($"[dim]Subscription: {status.SubscriptionName}[/]");

        return CommandResult.Ok(status);
    }
}
