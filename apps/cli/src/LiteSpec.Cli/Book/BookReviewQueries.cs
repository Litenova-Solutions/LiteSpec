using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The review readings over the index: the review batches, one record's tier
///     and risk, and the records serving a journey or its steps.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookReviewQueries
{
    /// <summary>Collects the review batches the queue shard shows.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="findingsFile">The findings file node, or null.</param>
    /// <param name="root">The consumer root.</param>
    /// <returns>The review bundle.</returns>
    public static ReviewBundle ReviewData(BookIndex index, JsonNode? findingsFile, string root) =>
        new(BookReviewQueue.Compute(index, root, findingsFile, DateOnly.FromDateTime(DateTime.UtcNow)));

    /// <summary>One record's tier: the tier the model states, or 1 when its record carries risk, else 3.</summary>
    /// <param name="record">The record.</param>
    /// <returns>The record's tier.</returns>
    public static int TierOf(JsonObject record)
    {
        if (record["tier"] is JsonValue scalar && scalar.TryGetValue<int>(out var tier) && tier is 1 or 3)
        {
            return tier;
        }

        return HasRisk(record) ? 1 : 3;
    }

    /// <summary>Whether a record carries any risk value.</summary>
    /// <param name="record">The record.</param>
    /// <returns>Whether the record carries risk.</returns>
    public static bool HasRisk(JsonObject record) =>
        BookRender.Fields(record)?["risk"] is JsonArray risks && risks.Count > 0;

    /// <summary>The use cases serving one journey step id, such as journey/name#s1.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="stepId">The journey step id.</param>
    /// <returns>The serving use cases, in title order.</returns>
    public static IReadOnlyList<JsonObject> ServingUseCases(BookIndex index, string stepId)
    {
        var serving = new List<JsonObject>();
        foreach (var record in index.ByType.TryGetValue("use-case", out var cases) ? cases : [])
        {
            var serves = BookRender.Links(record)?["serves"] as JsonArray ?? [];
            foreach (var target in serves)
            {
                if (target is JsonValue scalar && scalar.TryGetValue<string>(out var id) && id == stepId)
                {
                    serving.Add(record);
                    break;
                }
            }
        }

        return serving
            .OrderBy(record => BookModel.SortTitle(record), BookRender.OrdinalComparer)
            .ThenBy(record => BookRender.StrProp(record, "id") ?? "", StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Every record serving one journey step id, of any type: use cases, reactions, jobs, screens and the rest.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="stepId">The journey step id.</param>
    /// <returns>The serving records, in title order.</returns>
    public static IReadOnlyList<JsonObject> ServingRecords(BookIndex index, string stepId)
    {
        var serving = new List<JsonObject>();

        foreach (var list in index.ByType.Values)
        {
            foreach (var record in list)
            {
                var targets = new List<string>();
                targets.AddRange(BookModelQueries.LinksOf(record, "serves"));

                if (BookRender.Fields(record)?["serves"] is JsonArray fieldServes)
                {
                    targets.AddRange(fieldServes
                        .Where(target => target is JsonValue scalar && scalar.TryGetValue<string>(out _))
                        .Select(target => BookRender.Str(target)));
                }
                else if (BookRender.Fields(record)?["serves"] is JsonValue single)
                {
                    targets.Add(BookRender.Str(single));
                }

                if (targets.Contains(stepId))
                {
                    serving.Add(record);
                }
            }
        }

        return serving
            .OrderBy(record => BookModel.SortTitle(record), BookRender.OrdinalComparer)
            .ThenBy(record => BookRender.StrProp(record, "id") ?? "", StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The use cases serving any step of one journey.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="journey">The journey record.</param>
    /// <returns>The serving use cases, in title order.</returns>
    public static IReadOnlyList<JsonObject> JourneyUseCases(BookIndex index, JsonObject journey)
    {
        var journeyId = BookRender.StrProp(journey, "id") ?? "";
        var steps = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in BookRender.Fields(journey)?["steps"] as JsonArray ?? [])
        {
            if (step is JsonObject obj && BookRender.StrProp(obj, "id") is { } stepId)
            {
                steps.Add($"{journeyId}#{stepId}");
            }
        }

        var serving = new List<JsonObject>();
        foreach (var record in index.ByType.TryGetValue("use-case", out var cases) ? cases : [])
        {
            var serves = BookRender.Links(record)?["serves"] as JsonArray ?? [];
            if (serves.Any(target => target is JsonValue scalar && scalar.TryGetValue<string>(out var id) && id is not null && steps.Contains(id)))
            {
                serving.Add(record);
            }
        }

        return serving
            .OrderBy(record => BookModel.SortTitle(record), BookRender.OrdinalComparer)
            .ThenBy(record => BookRender.StrProp(record, "id") ?? "", StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The requirements one journey shows, in title order.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="journey">The journey record.</param>
    /// <returns>The journey's requirements, in title order.</returns>
    public static IReadOnlyList<JsonObject> JourneyRequirements(BookIndex index, JsonObject journey)
    {
        var ids = BookModelQueries.LinksOf(journey, "cites");
        return ids
            .Select(id => index.ById.GetValueOrDefault(id))
            .OfType<JsonObject>()
            .OrderBy(record => BookModel.SortTitle(record), BookRender.OrdinalComparer)
            .ThenBy(record => BookRender.StrProp(record, "id") ?? "", StringComparer.Ordinal)
            .ToList();
    }
}
