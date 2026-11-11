using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Book;

/// <summary>
///     The proof details of one record: the state of a scenario or rule from its
///     proves links and its proving test, the machine facts from the status maps,
///     the reachable states of an aggregate, and the fulfilling counts of a
///     requirement.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgements-in-the-book</follows>
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

    /// <summary>Adds the machine facts of one record: coded and verified use cases, met requirements, tested proof targets.</summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="index">The model index.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    public static void AddMachine(JsonObject detail, BookIndex index, string id, string type)
    {
        if (type == "use-case")
        {
            var entry = index.Status.TryGetPropertyValue(id, out var node) ? node as JsonObject : null;
            detail["machine"] = new JsonObject
            {
                ["coded"] = BookRender.IsTrue(entry?["coded"]),
                ["verified"] = BookRender.IsTrue(entry?["verified"]),
            };
        }
        else if (type == "requirement")
        {
            var entry = index.RequirementStatus.TryGetValue(id, out var found) ? found : null;
            var measure = entry?["measurePassed"];
            var passed = measure is JsonValue scalar && scalar.TryGetValue<bool>(out var flag) ? flag : (bool?)null;
            JsonNode? measureNode = passed.HasValue ? JsonValue.Create(passed.Value) : null;
            var machine = new JsonObject
            {
                ["met"] = BookRender.IsTrue(entry?["verified"]),
                ["measurePassed"] = measureNode,
            };
            if (index.Tested.TryGetValue(id, out var tested))
            {
                machine["tested"] = tested;
            }

            detail["machine"] = machine;
        }
        else if (type is "decision" or "external-system" or "integration-event"
            && index.Tested.TryGetValue(id, out var tested))
        {
            detail["machine"] = new JsonObject
            {
                ["tested"] = tested,
            };
        }
    }

    /// <summary>Adds the reachable states of one aggregate, in identifier order.</summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="index">The model index.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    public static void AddReachable(JsonObject detail, BookIndex index, string id, string type)
    {
        if (type != "aggregate")
        {
            return;
        }

        var states = new JsonArray();
        var names = index.ReachableStates.TryGetValue(id, out var found)
            ? found.OrderBy(name => name, StringComparer.Ordinal).ToList()
            : new List<string>();
        foreach (var name in names)
        {
            states.Add((JsonNode)name);
        }

        detail["reachable"] = states;
    }

    /// <summary>Adds the fulfilling type counts of one requirement.</summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="kept">The fulfilling counts per requirement id and record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    public static void AddKept(JsonObject detail, Dictionary<string, Dictionary<string, int>> kept, string id, string type)
    {
        if (type != "requirement")
        {
            return;
        }

        var counts = new JsonObject();
        if (kept.TryGetValue(id, out var byType))
        {
            foreach (var pair in byType.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                counts[pair.Key] = pair.Value;
            }
        }

        detail["keptBy"] = counts;
    }
}
