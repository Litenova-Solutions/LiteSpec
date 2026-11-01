using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The held-by detail of one record: the draft up-links holding a settled
///     record at effective draft.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookHeldByDetail
{
    /// <summary>The draft up-links holding one settled record at effective draft.</summary>
    /// <param name="outline">The built outline.</param>
    /// <param name="decisions">The decision ids per record id.</param>
    /// <param name="effective">The effective status per record id.</param>
    /// <param name="record">The record.</param>
    /// <param name="id">The record id.</param>
    /// <returns>The holding record ids, at most six.</returns>
    public static JsonArray HeldBy(BookOutlineResult outline, Dictionary<string, List<string>> decisions, Dictionary<string, string> effective, JsonObject record, string id)
    {
        var held = new JsonArray();
        var status = BookRender.StrProp(record, "status");
        if (string.IsNullOrEmpty(status))
        {
            status = "draft";
        }

        if (status == "draft" || (effective.TryGetValue(id, out var current) ? current : status) != "draft")
        {
            return held;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();
        void TryAdd(string? candidate)
        {
            if (string.IsNullOrEmpty(candidate))
            {
                return;
            }

            var target = BookDetailValues.BaseId(candidate);
            if (target == id || !effective.TryGetValue(target, out var state) || state != "draft")
            {
                return;
            }

            if (seen.Add(target))
            {
                ordered.Add(target);
            }
        }

        TryAdd(outline.ParentOf.TryGetValue(id, out var parent) ? parent : null);
        if (decisions.TryGetValue(id, out var deciding))
        {
            foreach (var decision in deciding)
            {
                TryAdd(decision);
            }
        }

        foreach (var target in BookDetailValues.StringsOf(BookRender.Fields(record)?["fulfills"]))
        {
            TryAdd(target);
        }

        foreach (var target in ordered.Take(6))
        {
            held.Add(target);
        }

        return held;
    }
}
