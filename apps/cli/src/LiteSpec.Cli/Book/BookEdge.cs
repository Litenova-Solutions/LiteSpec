namespace LiteSpec.Book;

/// <summary>One outgoing or incoming edge: the other end and the field key.</summary>
/// <param name="Id">The other record id, or a code or test id for computed edges.</param>
/// <param name="Key">The field key, such as use-case.effects.calls, or a computed kind.</param>
sealed record BookEdge(string Id, string Key);
