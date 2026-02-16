namespace NimbusStation.Infrastructure.Browser;

/// <summary>
/// Represents a node in the alias hierarchy tree.
/// Leaf nodes have an alias name; branch nodes have children keyed by dimension value.
/// </summary>
public sealed class AliasHierarchyNode
{
    /// <summary>
    /// Gets the display label for this node (the dimension entry key, e.g., "ninja").
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Gets the dimension name this node belongs to (e.g., "kingdoms").
    /// Null for the root node. Set for generated nodes (both branch and leaf).
    /// </summary>
    public string? DimensionName { get; init; }

    /// <summary>
    /// Gets the child nodes, keyed by the dimension entry key.
    /// Empty for leaf nodes.
    /// </summary>
    public Dictionary<string, AliasHierarchyNode> Children { get; init; } = [];

    /// <summary>
    /// Gets the resolved alias name for leaf nodes.
    /// Null for branch nodes.
    /// </summary>
    public string? AliasName { get; init; }

    /// <summary>
    /// Gets whether this node is a leaf (has a resolved alias).
    /// </summary>
    public bool IsLeaf => AliasName is not null;
}
