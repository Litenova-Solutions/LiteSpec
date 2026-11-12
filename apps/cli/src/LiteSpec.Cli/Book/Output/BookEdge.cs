namespace LiteSpec.Cli.Book.Output;

/// <summary>One outgoing or incoming edge: the other end and the full dotted field key.</summary>
/// <param name="Id">The other end: the target record id for an outgoing edge, the source record id for an incoming one, or a code or test id for a computed edge.</param>
/// <param name="Key">The full dotted field key, such as use-case.effects.calls, or a computed edge kind such as implements, proves, follows or calls.</param>
sealed record BookEdge(string Id, string Key);
