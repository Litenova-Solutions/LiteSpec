namespace LiteSpec.Book;

/// <summary>One outline node: a record, a group, or the code, test or finding under its subject.</summary>
/// <param name="Id">The record id, the layer key, the layer and type, or the code, test or finding id.</param>
/// <param name="Kind">One of record, group, code, test or finding.</param>
/// <param name="Label">The title, the type plural, the layer label, or the file, test or finding name.</param>
/// <param name="Type">The record type, the code, test or finding type, or null for a layer group.</param>
/// <param name="Status">The record status, or null for anything else.</param>
/// <param name="Children">The nested nodes.</param>
sealed record BookOutlineNode(string Id, string Kind, string Label, string? Type, string? Status, List<BookOutlineNode> Children);
