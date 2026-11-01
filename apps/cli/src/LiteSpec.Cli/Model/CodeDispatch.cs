namespace LiteSpec.Model;

/// <summary>
///     One dispatch call a handler, reaction or job class makes. <paramref name="Verb" /> is
///     <c>runs</c>, <c>sends</c>, <c>emits</c> or <c>calls</c>. <paramref name="Target" />
///     is the message type, the template key, or the port method. <paramref name="Detail" />
///     is the port type for <c>calls</c>, <c>type</c> or <c>template</c> for <c>sends</c>,
///     and empty otherwise. <paramref name="Via" /> names the helper <c>Type.Method</c> a
///     one-hop attribution found the dispatch through, or null for a direct dispatch.
/// </summary>
sealed record CodeDispatch(string Verb, string Target, string Detail, string File, int Line, string? Via = null);
