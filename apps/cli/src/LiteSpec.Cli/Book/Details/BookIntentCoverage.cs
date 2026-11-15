using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Output;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Model;

namespace LiteSpec.Cli.Book.Details;

/// <summary>
///     The Intent coverage status in the book's JSON shape. The build computes it once with
///     IntentCoverage.Compute from the built model; this file writes its counts to the manifest,
///     its entries to the coverage shard, and reads it back. The status covers every item the
///     model computes, and the share reached.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgements-in-the-book</follows>
static class BookIntentCoverage
{
    /// <summary>The Intent coverage a store carries: the status the build computed from the model with IntentCoverage.Compute.</summary>
    /// <param name="store">The model object the build wrote the carried coverage into.</param>
    /// <returns>The Intent coverage status for the store.</returns>
    public static IntentCoverage Of(JsonObject store) =>
        store["intentCoverage"] is JsonObject carried
            ? FromJson(carried)
            : throw new InvalidOperationException("The model object carries no Intent coverage.");

    /// <summary>One Intent coverage status as its JSON object with counts, share and entries.</summary>
    /// <param name="coverage">The Intent coverage status.</param>
    /// <returns>The JSON object with the counts, the share and one entry per item.</returns>
    public static JsonObject ToJson(IntentCoverage coverage)
    {
        var json = SummaryJson(coverage);
        json["entries"] = EntriesJson(coverage);
        return json;
    }

    /// <summary>One Intent coverage status as the manifest shard holds it: counts and share, without entries.</summary>
    /// <param name="coverage">The Intent coverage status.</param>
    /// <returns>The JSON object with the counts and the share.</returns>
    public static JsonObject SummaryJson(IntentCoverage coverage) =>
        new JsonObject
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
            ["problems"] = new JsonObject
            {
                ["reached"] = coverage.ProblemsReached,
                ["total"] = coverage.ProblemsTotal,
            },
            ["constraints"] = new JsonObject
            {
                ["reached"] = coverage.ConstraintsReached,
                ["total"] = coverage.ConstraintsTotal,
            },
        };

    /// <summary>One Intent coverage status as the coverage shard holds it: the bare entries list.</summary>
    /// <param name="coverage">The Intent coverage status.</param>
    /// <returns>The entries, one per requirement, flow step, problem and constraint.</returns>
    public static JsonArray EntriesJson(IntentCoverage coverage)
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

        return entries;
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
