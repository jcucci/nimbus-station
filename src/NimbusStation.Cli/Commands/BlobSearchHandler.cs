using NimbusStation.Cli.Output;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Formatting;
using NimbusStation.Core.Search;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Providers.Azure.Blob;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Handles interactive blob search navigation using Spectre.Console.
/// </summary>
public sealed class BlobSearchHandler
{
    private readonly BlobSearchService _searchService;
    private readonly IBlobService _blobService;
    private readonly ISessionService _sessionService;
    private readonly ThemeConfig _theme;

    // Sentinel values for special menu options
    private const string SearchNewPrefixSentinel = "__SEARCH_NEW_PREFIX__";
    private const string GoBackSentinel = "__GO_BACK__";

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobSearchHandler"/> class.
    /// </summary>
    public BlobSearchHandler(
        BlobSearchService searchService,
        IBlobService blobService,
        ISessionService sessionService,
        ThemeConfig theme)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    /// <summary>
    /// Runs the interactive search loop.
    /// </summary>
    /// <param name="aliasName">The blob alias name.</param>
    /// <param name="initialPrefix">The initial search prefix.</param>
    /// <param name="context">The command context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The command result.</returns>
    public async Task<CommandResult> RunAsync(
        string aliasName,
        string? initialPrefix,
        CommandContext context,
        CancellationToken cancellationToken = default)
    {
        var currentPrefix = initialPrefix ?? string.Empty;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Fetch results for current prefix
                var spinner = new SpinnerService(context.Options);
                var searchResult = await spinner.RunWithSpinnerAsync(
                    $"Searching{(string.IsNullOrEmpty(currentPrefix) ? " root" : $": {currentPrefix}")}...",
                    () => _searchService.SearchAsync(aliasName, currentPrefix, cancellationToken: cancellationToken));

                // Display search info
                DisplaySearchInfo(searchResult, context);

                if (searchResult.Items.Count == 0)
                {
                    context.Output.WriteErrorLine($"[{_theme.DimColor}]No items found at this prefix.[/]");

                    if (SearchNavigator.IsRootPrefix(currentPrefix))
                        return CommandResult.Ok();

                    // Offer to go back
                    if (AnsiConsole.Confirm("Go back to parent?", defaultValue: true))
                    {
                        currentPrefix = SearchNavigator.GetParentPrefix(currentPrefix);
                        continue;
                    }
                    return CommandResult.Ok();
                }

                // Build and show selection prompt
                var selection = ShowSelectionPrompt(searchResult, currentPrefix);

                // Handle selection
                var (shouldContinue, newPrefix) = await HandleSelectionAsync(
                    selection,
                    searchResult,
                    aliasName,
                    context,
                    cancellationToken);

                if (!shouldContinue)
                    return CommandResult.Ok();

                currentPrefix = newPrefix;
            }
            catch (OperationCanceledException)
            {
                // User pressed Ctrl+C
                return CommandResult.Ok();
            }
        }

        return CommandResult.Ok();
    }

    private void DisplaySearchInfo(SearchResult result, CommandContext context)
    {
        var prefixDisplay = string.IsNullOrEmpty(result.CurrentPrefix) ? "root" : result.CurrentPrefix;
        var countDisplay = result.IsTruncated
            ? $"{result.Items.Count}+ items (results truncated)"
            : $"{result.Items.Count} item(s)";

        if (!context.Options.Quiet)
        {
            context.Output.WriteErrorLine($"[{_theme.DimColor}]Location: {prefixDisplay}[/]");
            context.Output.WriteErrorLine($"[{_theme.DimColor}]Found {countDisplay}[/]");
        }
    }

    private string ShowSelectionPrompt(SearchResult result, string currentPrefix)
    {
        var prompt = new SelectionPrompt<string>()
            .Title("Select an item")
            .UseConverter(MarkupSanitizer.SanitizeBrackets)
            .PageSize(15)
            .EnableSearch()
            .SearchPlaceholderText("Type to filter...")
            .WrapAround()
            .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold));

        // Add special options first (search new prefix is first = default selection)
        prompt.AddChoice(FormatSpecialOption(SearchNewPrefixSentinel));

        if (!SearchNavigator.IsRootPrefix(currentPrefix))
            prompt.AddChoice(FormatSpecialOption(GoBackSentinel));

        // Add directories first, then files
        foreach (var item in result.Items)
        {
            prompt.AddChoice(FormatItem(item));
        }

        return AnsiConsole.Prompt(prompt);
    }

    private static string FormatSpecialOption(string sentinel) => sentinel switch
    {
        SearchNewPrefixSentinel => "🔍 Search with new prefix...",
        GoBackSentinel => "⬅️  Go back to parent",
        _ => sentinel
    };

    private static string FormatItem(ISearchItem item)
    {
        if (item.Kind == SearchItemKind.Directory)
            return $"📁 {item.Name}";

        var sizeDisplay = item.Size.HasValue ? $" ({SizeFormatter.Format(item.Size.Value)})" : string.Empty;
        return $"📄 {item.Name}{sizeDisplay}";
    }

    private static (string Sentinel, ISearchItem? Item) ParseSelection(string selection, SearchResult result)
    {
        // Check for special options
        if (selection.Contains("Search with new prefix..."))
            return (SearchNewPrefixSentinel, null);
        if (selection.Contains("Go back to parent"))
            return (GoBackSentinel, null);

        // Find matching item by name (strip emoji prefix and size suffix)
        foreach (var item in result.Items)
        {
            var formatted = FormatItem(item);
            if (selection == formatted)
                return (string.Empty, item);
        }

        return (string.Empty, null);
    }

    private async Task<(bool ShouldContinue, string NewPrefix)> HandleSelectionAsync(
        string selection,
        SearchResult searchResult,
        string aliasName,
        CommandContext context,
        CancellationToken cancellationToken)
    {
        var currentPrefix = searchResult.CurrentPrefix;
        var (sentinel, item) = ParseSelection(selection, searchResult);

        switch (sentinel)
        {
            case SearchNewPrefixSentinel:
                var promptText = string.IsNullOrEmpty(currentPrefix)
                    ? "Enter new prefix:"
                    : $"Enter prefix within {currentPrefix}:";
                var newPrefix = AnsiConsole.Prompt(
                    new TextPrompt<string>(promptText)
                        .AllowEmpty());
                return (true, currentPrefix + newPrefix);

            case GoBackSentinel:
                return (true, SearchNavigator.GetParentPrefix(currentPrefix));
        }

        if (item is null)
        {
            context.Output.WriteErrorLine($"[{_theme.WarningColor}]Could not parse selection.[/]");
            return (true, currentPrefix);
        }

        if (item.Kind == SearchItemKind.Directory)
        {
            // Navigate into directory
            return (true, item.FullPath);
        }

        // File selected - download and display
        await HandleFileSelectionAsync(aliasName, item, context, cancellationToken);
        return (false, currentPrefix);
    }

    private async Task HandleFileSelectionAsync(
        string aliasName,
        ISearchItem item,
        CommandContext context,
        CancellationToken cancellationToken)
    {
        var spinner = new SpinnerService(context.Options);

        // Always download to session directory
        var downloadsDir = _sessionService.GetDownloadsDirectory(context.CurrentSession!.TicketId);
        var downloadedPath = await spinner.RunWithSpinnerAsync(
            $"Downloading {item.Name}...",
            () => _blobService.DownloadBlobAsync(aliasName, item.FullPath, downloadsDir, cancellationToken));

        context.Output.WriteErrorLine($"[{_theme.SuccessColor}]Downloaded to:[/] {downloadedPath}");

        // Read the downloaded file for display
        var content = await File.ReadAllBytesAsync(downloadedPath, cancellationToken);

        // Check for binary content (null-byte heuristic on first 8KB)
        var checkLength = Math.Min(content.Length, 8192);
        bool isBinary = content.AsSpan(0, checkLength).IndexOf((byte)0) >= 0;

        if (isBinary && context.Output.SupportsFormatting)
        {
            context.Output.WriteErrorLine($"[{_theme.WarningColor}]Warning: This appears to be a binary file.[/]");

            var prompt = new PromptService(context.Options);
            if (!prompt.Confirm("Display anyway?", defaultValue: false))
                return;
        }

        // If interactive, try JSON syntax highlighting
        if (context.Output.SupportsFormatting)
        {
            var text = System.Text.Encoding.UTF8.GetString(content);
            var renderable = JsonRenderer.TryCreateRenderable(text, _theme);
            if (renderable is not null)
            {
                context.Output.WriteRenderable(renderable);
                return;
            }
        }

        // Fallback: raw output (for piping or non-JSON content)
        context.Output.WriteRaw(content);
    }
}
