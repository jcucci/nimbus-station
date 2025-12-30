using NimbusStation.Cli.Output;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Errors;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Providers.Azure.Blob;
using NimbusStation.Providers.Azure.Errors;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for Azure Blob Storage operations.
/// </summary>
public sealed class BlobCommand : ICommand
{
    private readonly IBlobService _blobService;
    private readonly IConfigurationService _configurationService;
    private readonly ISessionService _sessionService;

    private static readonly HashSet<string> _subcommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "containers", "list", "get", "download"
    };

    /// <inheritdoc/>
    public string Name => "blob";

    /// <inheritdoc/>
    public string Description => "Azure Blob Storage operations";

    /// <inheritdoc/>
    public string Usage => "blob [containers | list [prefix] | get <path> | download <path>]";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands => _subcommands;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobCommand"/> class.
    /// </summary>
    public BlobCommand(IBlobService blobService, IConfigurationService configurationService, ISessionService sessionService)
    {
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(string[] args, CommandContext context, CancellationToken cancellationToken = default)
    {
        if (!context.HasActiveSession)
            return CommandResult.Error("No active session. Use 'session start <ticket>' first.");

        if (args.Length == 0)
            return CommandResult.Error($"Usage: {Usage}");

        var subcommand = args[0].ToLowerInvariant();
        var subArgs = args.Skip(1).ToArray();

        return subcommand switch
        {
            "containers" => await HandleContainersAsync(context, cancellationToken),
            "list" => await HandleListAsync(subArgs, context, cancellationToken),
            "get" => await HandleGetAsync(subArgs, context, cancellationToken),
            "download" => await HandleDownloadAsync(subArgs, context, cancellationToken),
            _ => CommandResult.Error($"Unknown subcommand '{args[0]}'. Available: containers, list, get, download")
        };
    }

    private async Task<CommandResult> HandleContainersAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var activeAlias = context.CurrentSession?.ActiveContext?.ActiveStorageAlias;
        if (string.IsNullOrEmpty(activeAlias))
            return CommandResult.Error("No active storage context. Use 'use storage <alias>' first.");

        var aliasConfig = _configurationService.GetStorageAlias(activeAlias);
        if (aliasConfig is null)
            return CommandResult.Error($"Storage alias '{activeAlias}' not found in config.");

        var spinner = new SpinnerService(context.Options);
        var theme = _configurationService.GetTheme();

        try
        {
            var result = await spinner.RunWithSpinnerAsync(
                "Loading containers...",
                () => _blobService.ListContainersAsync(activeAlias, cancellationToken));

            var table = new Table();
            table.AddColumn(new TableColumn("[bold]Name[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Last Modified[/]").RightAligned());

            foreach (var container in result.Containers)
            {
                table.AddRow(
                    container.Name,
                    container.LastModified.ToString("yyyy-MM-dd HH:mm"));
            }

            context.Output.WriteRenderable(table);

            if (!context.Options.Quiet)
                context.Output.WriteErrorLine($"[{theme.DimColor}]{result.Containers.Count} container(s) in {aliasConfig.Account}[/]");

            return CommandResult.Ok(data: result);
        }
        catch (InvalidOperationException ex)
        {
            var error = AzureErrorMapper.FromCliError(ex.Message, activeAlias);
            ErrorFormatter.WriteError(error, theme.ErrorColor, context.Options.Quiet);
            return CommandResult.Error(error.Message);
        }
    }

    private async Task<CommandResult> HandleListAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        var activeAlias = context.CurrentSession?.ActiveContext?.ActiveBlobAlias;
        if (string.IsNullOrEmpty(activeAlias))
            return CommandResult.Error("No active blob context. Use 'use blob <alias>' first.");

        var aliasConfig = _configurationService.GetBlobAlias(activeAlias);
        if (aliasConfig is null)
            return CommandResult.Error($"Blob alias '{activeAlias}' not found in config.");

        var prefix = args.Length > 0 ? args[0] : null;
        var spinner = new SpinnerService(context.Options);
        var theme = _configurationService.GetTheme();

        try
        {
            var result = await spinner.RunWithSpinnerAsync(
                "Loading blobs...",
                () => _blobService.ListBlobsAsync(activeAlias, prefix, cancellationToken));

            var table = new Table();
            table.AddColumn(new TableColumn("[bold]Name[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Size[/]").RightAligned());
            table.AddColumn(new TableColumn("[bold]Modified[/]").RightAligned());

            foreach (var blob in result.Blobs)
            {
                table.AddRow(
                    blob.Name,
                    FormatSize(blob.Size),
                    blob.LastModified.ToString("yyyy-MM-dd HH:mm"));
            }

            context.Output.WriteRenderable(table);

            if (!context.Options.Quiet)
                context.Output.WriteErrorLine($"[{theme.DimColor}]{result.Blobs.Count} blob(s) in {aliasConfig.Container}[/]");

            return CommandResult.Ok(data: result);
        }
        catch (InvalidOperationException ex)
        {
            var error = AzureErrorMapper.FromCliError(ex.Message, activeAlias);
            ErrorFormatter.WriteError(error, theme.ErrorColor, context.Options.Quiet);
            return CommandResult.Error(error.Message);
        }
    }

    private async Task<CommandResult> HandleGetAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        var activeAlias = context.CurrentSession?.ActiveContext?.ActiveBlobAlias;
        if (string.IsNullOrEmpty(activeAlias))
            return CommandResult.Error("No active blob context. Use 'use blob <alias>' first.");

        var aliasConfig = _configurationService.GetBlobAlias(activeAlias);
        if (aliasConfig is null)
            return CommandResult.Error($"Blob alias '{activeAlias}' not found in config.");

        if (args.Length == 0)
            return CommandResult.Error("Usage: blob get <path>");

        var blobName = args[0];

        var spinner = new SpinnerService(context.Options);
        var prompt = new PromptService(context.Options);
        var theme = _configurationService.GetTheme();

        try
        {
            var result = await spinner.RunWithSpinnerAsync(
                "Fetching blob...",
                () => _blobService.GetBlobContentAsync(activeAlias, blobName, cancellationToken));

            // Check for binary content and warn if not piped
            if (result.IsBinary && !Console.IsOutputRedirected)
            {
                context.Output.WriteErrorLine($"[{theme.WarningColor}]Warning: This appears to be a binary file ({result.ContentType}).[/]");
                context.Output.WriteErrorLine($"[{theme.WarningColor}]Use 'blob download' to save to a file, or pipe the output.[/]");

                if (!prompt.Confirm("Continue anyway?", defaultValue: false))
                    return CommandResult.Ok();
            }

            // Output raw content for piping
            context.Output.WriteRaw(result.Content);

            return CommandResult.Ok(data: result);
        }
        catch (InvalidOperationException ex)
        {
            var error = AzureErrorMapper.FromCliError(ex.Message, blobName);
            ErrorFormatter.WriteError(error, theme.ErrorColor, context.Options.Quiet);
            return CommandResult.Error(error.Message);
        }
    }

    private async Task<CommandResult> HandleDownloadAsync(string[] args, CommandContext context, CancellationToken cancellationToken)
    {
        var activeAlias = context.CurrentSession?.ActiveContext?.ActiveBlobAlias;
        if (string.IsNullOrEmpty(activeAlias))
            return CommandResult.Error("No active blob context. Use 'use blob <alias>' first.");

        var aliasConfig = _configurationService.GetBlobAlias(activeAlias);
        if (aliasConfig is null)
            return CommandResult.Error($"Blob alias '{activeAlias}' not found in config.");

        if (args.Length == 0)
            return CommandResult.Error("Usage: blob download <path>");

        var blobName = args[0];
        var downloadsDir = _sessionService.GetDownloadsDirectory(context.CurrentSession!.TicketId);
        var spinner = new SpinnerService(context.Options);
        var prompt = new PromptService(context.Options);
        var theme = _configurationService.GetTheme();

        // Check if file already exists
        var expectedPath = Path.Combine(downloadsDir, Path.GetFileName(blobName));
        if (File.Exists(expectedPath) && !prompt.ConfirmOverwrite(expectedPath))
        {
            context.Output.WriteLine($"[{theme.DimColor}]Download cancelled.[/]");
            return CommandResult.Ok();
        }

        try
        {
            var downloadedPath = await spinner.RunWithSpinnerAsync(
                $"Downloading {Path.GetFileName(blobName)}...",
                () => _blobService.DownloadBlobAsync(activeAlias, blobName, downloadsDir, cancellationToken));

            context.Output.WriteLine($"[{theme.SuccessColor}]Downloaded to:[/] {downloadedPath}");

            return CommandResult.Ok(data: downloadedPath);
        }
        catch (InvalidOperationException ex)
        {
            var error = AzureErrorMapper.FromCliError(ex.Message, blobName);
            ErrorFormatter.WriteError(error, theme.ErrorColor, context.Options.Quiet);
            return CommandResult.Error(error.Message);
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return suffixIndex == 0 ? $"{size:F0} {suffixes[suffixIndex]}" : $"{size:F1} {suffixes[suffixIndex]}";
    }
}
