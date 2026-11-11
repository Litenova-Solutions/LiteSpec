namespace LiteSpec.Cli.Book.Outline;

/// <summary>One outline node: a record, a group, or the code, test or finding under its subject.</summary>
/// <param name="Id">The record id, the layer key, the layer and type, or the code, test or finding id.</param>
/// <param name="Kind">The node kind, always one of <see cref="BookOutlineKinds"/>: record, group, code, test or finding.</param>
/// <param name="Label">The title, the type plural, the layer label, or the file, test or finding name.</param>
/// <param name="Type">The record type, the code, test or finding type, or null for a layer group.</param>
/// <param name="Children">The nested nodes.</param>
sealed record BookOutlineNode(string Id, string Kind, string Label, string? Type, List<BookOutlineNode> Children);
