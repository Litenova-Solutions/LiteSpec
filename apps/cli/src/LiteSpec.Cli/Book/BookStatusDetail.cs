using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The status details of one record: its machine state, the reachable states
///     of an aggregate, and the fulfilling counts of a requirement.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookStatusDetail
{
    /// <summary>Adds the machine state of one record: verified use cases, met requirements, tested proof targets.</summary>
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
                ["implemented"] = BookRender.IsTrue(entry?["implemented"]),
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
            states.Add(name);
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
