namespace LiteSpec.Book;

/// <summary>One built outline: the parent per record, the layer trees, and the unplaced ids.</summary>
/// <param name="ParentOf">The parent id per record id, or null when no rule places it.</param>
/// <param name="Layers">One root group node per layer key, in layer order.</param>
/// <param name="Unplaced">The ids with no outline node: no rule places them or their parent is itself unplaced, in identifier order.</param>
sealed record BookOutlineResult(
    IReadOnlyDictionary<string, string?> ParentOf,
    IReadOnlyList<BookOutlineNode> Layers,
    IReadOnlyList<string> Unplaced);
