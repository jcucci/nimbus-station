using NimbusStation.Infrastructure.Browser;
using NimbusStation.Infrastructure.Configuration;
using Spectre.Console;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Drives interactive Spectre.Console prompts for hierarchical alias browsing.
/// </summary>
public static class AliasBrowserHandler
{
    /// <summary>
    /// Specifies how the browser behaves after an alias is selected.
    /// </summary>
    public enum BrowseMode
    {
        /// <summary>Select mode: returns the alias name for activation.</summary>
        Select,

        /// <summary>Display mode: shows alias details without activating.</summary>
        Display
    }

    private const string GoBackSentinel = "__GO_BACK__";

    /// <summary>
    /// Runs the interactive alias browser.
    /// </summary>
    /// <param name="providerType">The provider type (cosmos, blob, storage).</param>
    /// <param name="hierarchy">The hierarchy root node, or null for flat list.</param>
    /// <param name="allAliasNames">All alias names (used for flat-list fallback).</param>
    /// <param name="theme">The current theme configuration.</param>
    /// <returns>The selected alias name, or null if the user cancelled.</returns>
    public static string? Run(
        string providerType,
        AliasHierarchyNode? hierarchy,
        IReadOnlyCollection<string> allAliasNames,
        ThemeConfig theme)
    {
        try
        {
            if (hierarchy is null || hierarchy.Children.Count == 0)
                return ShowFlatList(providerType, allAliasNames, theme);

            return RunHierarchicalBrowse(providerType, hierarchy, theme);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private static string? ShowFlatList(
        string providerType,
        IReadOnlyCollection<string> allAliasNames,
        ThemeConfig theme)
    {
        if (allAliasNames.Count == 0)
            return null;

        var prompt = new SelectionPrompt<string>()
            .Title($"[{theme.PromptColor}]Select {providerType} alias:[/]")
            .PageSize(15)
            .EnableSearch()
            .SearchPlaceholderText("Type to filter...")
            .WrapAround()
            .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold))
            .AddChoices(allAliasNames.OrderBy(a => a));

        return AnsiConsole.Prompt(prompt);
    }

    private static string? RunHierarchicalBrowse(
        string providerType,
        AliasHierarchyNode root,
        ThemeConfig theme)
    {
        var breadcrumb = new List<string>();
        AliasHierarchyNode currentNode = root;

        while (true)
        {
            string title = BuildTitle(providerType, breadcrumb, currentNode, theme);
            var prompt = new SelectionPrompt<string>()
                .Title(title)
                .PageSize(15)
                .EnableSearch()
                .SearchPlaceholderText("Type to filter...")
                .WrapAround()
                .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold));

            if (breadcrumb.Count > 0)
                prompt.AddChoice(FormatGoBack());

            foreach (var (key, child) in currentNode.Children.OrderBy(c => c.Key == "Custom" ? 1 : 0).ThenBy(c => c.Key))
            {
                if (child.IsLeaf)
                    prompt.AddChoice(FormatLeaf(key, child.AliasName!));
                else
                    prompt.AddChoice(FormatBranch(key, child.Children.Count));
            }

            string selection = AnsiConsole.Prompt(prompt);

            if (IsGoBack(selection))
            {
                breadcrumb.RemoveAt(breadcrumb.Count - 1);
                currentNode = NavigateToNode(root, breadcrumb);
                continue;
            }

            string? selectedKey = ParseSelectedKey(selection, currentNode);
            if (selectedKey is null)
                continue;

            AliasHierarchyNode selectedChild = currentNode.Children[selectedKey];

            if (selectedChild.IsLeaf)
                return selectedChild.AliasName;

            breadcrumb.Add(selectedKey);
            currentNode = selectedChild;
        }
    }

    private static string BuildTitle(
        string providerType,
        List<string> breadcrumb,
        AliasHierarchyNode currentNode,
        ThemeConfig theme)
    {
        if (breadcrumb.Count == 0)
        {
            string firstDimension = currentNode.Children.Values
                .FirstOrDefault(c => c.DimensionName is not null)?.DimensionName ?? "alias";
            return $"[{theme.PromptColor}]Select {firstDimension}:[/]";
        }

        string path = string.Join(" / ", breadcrumb);
        string nextDimension = currentNode.Children.Values
            .FirstOrDefault(c => !c.IsLeaf && c.DimensionName is not null)?.DimensionName
            ?? (currentNode.Children.Values.Any(c => c.IsLeaf) ? "alias" : "item");

        return $"[{theme.PromptColor}]Select {nextDimension}[/] [{theme.DimColor}]({path}):[/]";
    }

    private static string FormatGoBack() => $"{GoBackSentinel} [Go back]";

    private static string FormatBranch(string key, int childCount) =>
        $"  {key} ({childCount})";

    private static string FormatLeaf(string key, string aliasName) =>
        $"  {key} -> {aliasName}";

    private static bool IsGoBack(string selection) =>
        selection.Contains(GoBackSentinel);

    private static string? ParseSelectedKey(string selection, AliasHierarchyNode currentNode)
    {
        foreach (var (key, child) in currentNode.Children)
        {
            string formatted = child.IsLeaf
                ? FormatLeaf(key, child.AliasName!)
                : FormatBranch(key, child.Children.Count);

            if (selection == formatted)
                return key;
        }

        return null;
    }

    private static AliasHierarchyNode NavigateToNode(AliasHierarchyNode root, List<string> breadcrumb)
    {
        AliasHierarchyNode current = root;
        foreach (string key in breadcrumb)
        {
            if (current.Children.TryGetValue(key, out var child))
                current = child;
            else
                return root;
        }
        return current;
    }
}
