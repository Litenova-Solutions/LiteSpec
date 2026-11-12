using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Output;

namespace LiteSpec.Cli.Book.Details;

/// <summary>
///     The decided-by edges of the book: each external system gains the
///     decision its implementing classes follow, so the system page names
///     the decision that chooses its provider without a written link.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgements-in-the-book</follows>
static class BookDecisionDetail
{
    /// <summary>
    ///     Adds one decides edge from the deciding decision to each
    ///     external system whose implementing classes follow a decision:
    ///     the outgoing edge on the decision and the incoming edge on the
    ///     system. A decision that already writes decides to the system
    ///     keeps that edge, and gains no second one.
    /// </summary>
    /// <param name="model">The parsed model object that holds the follows links.</param>
    /// <param name="index">The model index that holds the records and the implements links.</param>
    /// <param name="outs">The outgoing edges per record id, which receive the decides edges.</param>
    /// <param name="ins">The incoming edges per record id, which receive the decides edges.</param>
    public static void AddDecidesEdges(JsonObject model, BookIndex index, Dictionary<string, List<BookEdge>> outs, Dictionary<string, List<BookEdge>> ins)
    {
        foreach (var pair in index.ById)
        {
            if (!string.Equals(BookRender.StrProp(pair.Value, "type"), "external-system", StringComparison.Ordinal))
            {
                continue;
            }

            var deciding = DecidingDecision(model, index, pair.Key);
            if (deciding is null)
            {
                continue;
            }

            if (!outs.TryGetValue(deciding, out var from) || !ins.TryGetValue(pair.Key, out var to))
            {
                continue;
            }

            if (from.Any(edge => string.Equals(edge.Id, pair.Key, StringComparison.Ordinal) && IsDecides(edge.Key)))
            {
                continue;
            }

            from.Add(new BookEdge(pair.Key, "decides"));
            to.Add(new BookEdge(deciding, "decides"));
        }
    }

    /// <summary>
    ///     The decision that chooses one external system's provider: the
    ///     first decision in identifier order that a class implementing the
    ///     system follows, or null when no implementing class follows one.
    /// </summary>
    /// <param name="model">The parsed model object that holds the follows links.</param>
    /// <param name="index">The model index that holds the records and the implements links.</param>
    /// <param name="id">The external-system record id.</param>
    /// <returns>The deciding decision id, or null when no implementing class follows a decision.</returns>
    public static string? DecidingDecision(JsonObject model, BookIndex index, string id)
    {
        if (!index.ImplementsByTarget.TryGetValue(id, out var implements) || implements.Count == 0)
        {
            return null;
        }

        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in implements)
        {
            if (BookRender.StrProp(link, "path") is { Length: > 0 } path)
            {
                paths.Add(path);
            }
        }

        if (paths.Count == 0)
        {
            return null;
        }

        string? first = null;
        var links = model["links"] as JsonObject;
        foreach (var entry in links?["follows"] as JsonArray ?? [])
        {
            if (entry is not JsonObject follow)
            {
                continue;
            }

            var path = BookRender.StrProp(follow, "path");
            var to = BookRender.StrProp(follow, "to");
            if (path is not { Length: > 0 } || to is not { Length: > 0 } || !paths.Contains(path))
            {
                continue;
            }

            if (!index.ById.TryGetValue(to, out var target)
                || !string.Equals(BookRender.StrProp(target, "type"), "decision", StringComparison.Ordinal))
            {
                continue;
            }

            if (first is null || string.Compare(to, first, StringComparison.Ordinal) < 0)
            {
                first = to;
            }
        }

        return first;
    }

    /// <summary>Whether an edge key is the decides link, written on the record or computed here.</summary>
    /// <param name="key">The edge key.</param>
    /// <returns>True for the bare computed key and any dotted written key ending in the decides field.</returns>
    static bool IsDecides(string key) =>
        string.Equals(key, "decides", StringComparison.Ordinal) || key.EndsWith(".decides", StringComparison.Ordinal);
}
