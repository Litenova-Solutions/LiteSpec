using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Book;

/// <summary>
///     The Intent coverage status in the book's JSON shape: computed from the
///     model the book renders when the store carries none, and read back from
///     the store when the command computed it from the built model. The status
///     covers each requirement and each journey step, and the share reached.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgements-in-the-book</follows>
static class BookIntentCoverage
{
    /// <summary>Computes the Intent coverage from a parsed model object.</summary>
    /// <param name="store">The parsed model object that holds the records array.</param>
    /// <returns>The computed Intent coverage status.</returns>
    public static IntentCoverage Compute(JsonObject store)
    {
        var fulfilledBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var servedBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        void Add(Dictionary<string, List<string>> map, string target, string source)
        {
            if (target.Length == 0 || source.Length == 0)
            {
                return;
            }

            if (!map.TryGetValue(target, out var list))
            {
                list = [];
                map[target] = list;
            }

            if (!list.Contains(source, StringComparer.Ordinal))
            {
                list.Add(source);
            }
        }

        var records = (store["records"] as JsonArray ?? []).OfType<JsonObject>().ToList();

        foreach (var record in records)
        {
            var type = BookRender.StrProp(record, "type") ?? "";
            var id = BookRender.StrProp(record, "id") ?? "";

            if (RecordTypes.FulfillingTypes.Contains(type, StringComparer.Ordinal))
            {
                foreach (var target in TargetsOf(record, "fulfills"))
                {
                    Add(fulfilledBy, target, id);
                }
            }

            if (RecordTypes.StepServingTypes.Contains(type, StringComparer.Ordinal))
            {
                foreach (var target in TargetsOf(record, "serves"))
                {
                    Add(servedBy, target, id);
                }
            }
        }

        var requirements = records
            .Where(record => BookRender.StrProp(record, "type") == "requirement" && !BookRender.IsTrue(record["derived"]))
            .Select(record => BookRender.StrProp(record, "id") ?? "")
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var steps = new List<string>();
        foreach (var journey in records
            .Where(record => BookRender.StrProp(record, "type") == "journey" && !BookRender.IsTrue(record["derived"]))
            .OrderBy(record => BookRender.StrProp(record, "id") ?? "", StringComparer.Ordinal))
        {
            var journeyId = BookRender.StrProp(journey, "id") ?? "";
            if (journeyId.Length == 0)
            {
                continue;
            }

            foreach (var step in BookRender.Fields(journey)?["steps"] as JsonArray ?? [])
            {
                if (step is JsonObject entry && BookRender.StrProp(entry, "id") is { Length: > 0 } stepId)
                {
                    steps.Add(journeyId + "#" + stepId);
                }
            }
        }

        return IntentCoverage.Build(requirements, steps, fulfilledBy, servedBy);
    }

    /// <summary>The Intent coverage a store carries, computed from its records when it carries none.</summary>
    /// <param name="store">The parsed model object that holds the records array and may hold the coverage.</param>
    /// <returns>The Intent coverage status for the store.</returns>
    public static IntentCoverage Of(JsonObject store) =>
        store["intentCoverage"] is JsonObject carried ? FromJson(carried) : Compute(store);

    /// <summary>One Intent coverage status as its JSON object for the manifest shard.</summary>
    /// <param name="coverage">The Intent coverage status.</param>
    /// <returns>The JSON object with the counts, the share and one entry per item.</returns>
    public static JsonObject ToJson(IntentCoverage coverage)
    {
        var entries = new JsonArray();
        foreach (var entry in coverage.Entries)
        {
            var reachedBy = new JsonArray();
            foreach (var source in entry.ReachedBy)
            {
                reachedBy.Add((JsonNode)source);
            }

            entries.Add((JsonNode)new JsonObject
            {
                ["id"] = entry.Id,
                ["kind"] = entry.Kind,
                ["reached"] = entry.Reached,
                ["reachedBy"] = reachedBy,
            });
        }

        return new JsonObject
        {
            ["reached"] = coverage.Reached,
            ["total"] = coverage.Total,
            ["share"] = JsonValue.Create(coverage.Share),
            ["requirements"] = new JsonObject
            {
                ["reached"] = coverage.RequirementsReached,
                ["total"] = coverage.RequirementsTotal,
            },
            ["steps"] = new JsonObject
            {
                ["reached"] = coverage.StepsReached,
                ["total"] = coverage.StepsTotal,
            },
            ["entries"] = entries,
        };
    }

    /// <summary>Reads one Intent coverage status from its JSON object.</summary>
    /// <param name="json">The JSON object the status was written as.</param>
    /// <returns>The Intent coverage status the object holds.</returns>
    public static IntentCoverage FromJson(JsonObject json)
    {
        var entries = new List<IntentCoverage.Entry>();
        foreach (var entry in json["entries"] as JsonArray ?? [])
        {
            if (entry is not JsonObject item || BookRender.StrProp(item, "id") is not { Length: > 0 } id)
            {
                continue;
            }

            var reachedBy = new List<string>();
            foreach (var source in item["reachedBy"] as JsonArray ?? [])
            {
                if (source is JsonValue scalar && scalar.TryGetValue<string>(out var text) && text.Length > 0)
                {
                    reachedBy.Add(text);
                }
            }

            entries.Add(new IntentCoverage.Entry(
                id,
                BookRender.StrProp(item, "kind") ?? "requirement",
                BookRender.IsTrue(item["reached"]),
                reachedBy));
        }

        return new IntentCoverage(entries);
    }

    /// <summary>One coverage share as a whole percent, for the no-script summary.</summary>
    /// <param name="coverage">The Intent coverage status.</param>
    /// <returns>The share reached as a whole percent.</returns>
    public static int PercentOf(IntentCoverage coverage) =>
        (int)Math.Round(coverage.Share * 100, MidpointRounding.AwayFromZero);

    /// <summary>The link targets of one field, read from the fields and the resolved links.</summary>
    /// <param name="record">The record object.</param>
    /// <param name="field">The field name: fulfills or serves.</param>
    /// <returns>The target ids the field names.</returns>
    static List<string> TargetsOf(JsonObject record, string field)
    {
        var targets = new List<string>();
        void Collect(JsonNode? node)
        {
            foreach (var target in BookDetailValues.StringsOf(node))
            {
                if (!targets.Contains(target, StringComparer.Ordinal))
                {
                    targets.Add(target);
                }
            }
        }

        Collect(BookRender.Fields(record)?[field]);
        Collect(BookRender.Links(record)?[field]);
        return targets;
    }
}
