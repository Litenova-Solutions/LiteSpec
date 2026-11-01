using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The proof detail of one record: the state of a scenario or rule from its
///     proves links and its proving test.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookProofDetail
{
    /// <summary>Adds the proof state of one scenario or rule from its proves links.</summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="index">The model index.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    public static void Add(JsonObject detail, BookIndex index, string id, string type)
    {
        if (type != "scenario" && type != "rule")
        {
            return;
        }

        var links = index.ProvesByClaim.TryGetValue(id, out var found) ? found : [];
        JsonObject? test = null;
        var scenario = BookRender.StrProp(links.FirstOrDefault(), "scenario");
        if (!string.IsNullOrEmpty(scenario) && index.TestsByName.TryGetValue(scenario, out var match))
        {
            test = new JsonObject
            {
                ["name"] = BookDetailValues.HumanTestName(scenario),
                ["path"] = BookDetailValues.StrNode(BookRender.StrProp(match, "file")),
                ["line"] = BookDetailValues.IntNode(BookDetailValues.IntProp(match, "line")),
            };
        }

        detail["proof"] = new JsonObject
        {
            ["state"] = State(index, id),
            ["tests"] = links.Count,
            ["test"] = test,
        };
    }

    /// <summary>One scenario or rule proof state: the stored state, or the proves links while the model holds none.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="id">The record id.</param>
    /// <returns>The proof state.</returns>
    public static string State(BookIndex index, string id)
    {
        if (index.ScenarioStatus.TryGetValue(id, out var stored) && stored is { Length: > 0 })
        {
            return stored;
        }

        if (!BookModel.HasTestRun(index))
        {
            return "norun";
        }

        if (index.StaleIds.Contains(id))
        {
            return "stale";
        }

        var proof = BookModelQueries.ScenarioProof(index, id);
        return proof.State switch
        {
            "proved-passing" => "passing",
            "proved-failing" => "failing",
            "proved-indirect" => "indirect",
            "awaiting-run" => "awaiting",
            "norun" => "norun",
            _ => proof.Claimed ? "unconfirmed" : "unproved",
        };
    }
}
