using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The computed What It Requires list of one constraint: the requirements
///     citing it, in identifier order.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgements-in-the-book</follows>
static class BookRequiresDetail
{
    /// <summary>Adds the citing requirements of one constraint.</summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="index">The model index.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    public static void Add(JsonObject detail, BookIndex index, string id, string type)
    {
        if (type != "constraint")
        {
            return;
        }

        var citing = new List<string>();
        if (index.ByType.TryGetValue("requirement", out var requirements))
        {
            foreach (var requirement in requirements)
            {
                var requirementId = BookRender.StrProp(requirement, "id");
                if (string.IsNullOrEmpty(requirementId))
                {
                    continue;
                }

                foreach (var cited in BookDetailValues.StringsOf(BookRender.Fields(requirement)?["cites"]))
                {
                    if (string.Equals(cited, id, StringComparison.Ordinal))
                    {
                        citing.Add(requirementId);
                        break;
                    }
                }
            }
        }

        citing.Sort(StringComparer.Ordinal);
        var list = new JsonArray();
        foreach (var requirementId in citing)
        {
            list.Add((JsonNode)requirementId);
        }

        detail["requires"] = list;
    }
}
