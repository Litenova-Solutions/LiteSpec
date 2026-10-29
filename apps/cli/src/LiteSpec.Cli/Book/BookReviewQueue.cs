using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Book;

/// <summary>
///     The review batches the review page shows: every draft and every reopened
///     record from the book index, computed by the shared review queue.
/// </summary>
/// <follows>litespec/core/release#compute-the-review-queue-from-the-model</follows>
static class BookReviewQueue
{
    /// <summary>Computes the review batches from the book index, in review order.</summary>
    public static IReadOnlyList<ReviewBatch> Compute(BookIndex index, string root, JsonNode? findingsFile, DateOnly today)
    {
        var model = Rebuild(index, root);
        return ReviewQueue.ComputeBatches(
            model.Records,
            model.EffectiveStatus,
            model.Proves,
            model.Stale,
            FindingsBySubject(findingsFile),
            model.Config,
            today);
    }

    /// <summary>Rebuilds the project model the queue helpers read from the book index.</summary>
    static ProjectModel Rebuild(BookIndex index, string root)
    {
        ConsumerConfig config;

        try
        {
            config = ConsumerConfig.Load(root);
        }
        catch (Exception)
        {
            config = ConsumerConfig.Load(Path.GetTempPath());
        }

        var model = new ProjectModel { Config = config };

        foreach (var record in index.Records.OfType<JsonObject>())
        {
            model.Records.Add(FromJson(record, root));

            if (BookRender.StrProp(record, "id") is { Length: > 0 } id
                && BookRender.StrProp(record, "effectiveStatus") is { Length: > 0 } effective
                && !model.EffectiveStatus.ContainsKey(id))
            {
                model.EffectiveStatus[id] = effective;
            }
        }

        foreach (var (claim, links) in index.ProvesByClaim)
        {
            foreach (var link in links)
            {
                model.Proves.Add(new ProvesLink(
                    BookRender.StrProp(link, "scenario") ?? "",
                    claim,
                    BookRender.IsTrue(link["confirmed"]),
                    link["passing"] is JsonValue scalar && scalar.TryGetValue<bool>(out var passing) ? passing : (bool?)null,
                    BookRender.IsTrue(link["indirect"])));
            }
        }

        foreach (var id in index.StaleIds)
        {
            model.Stale.Add(id);
        }

        return model;
    }

    /// <summary>Rebuilds one record from its model.json entry: resolved values on top, raw fields below.</summary>
    static Record FromJson(JsonObject record, string root)
    {
        var fields = record["fields"] as JsonObject ?? new JsonObject();
        using var document = JsonDocument.Parse(fields.ToJsonString());
        var relative = BookRender.StrProp(record, "file") ?? "";

        return new Record
        {
            Type = BookRender.StrProp(record, "type") ?? "",
            Id = BookRender.StrProp(record, "id") ?? "",
            Title = BookRender.StrProp(record, "title") ?? "",
            Owner = BookRender.StrProp(record, "owner"),
            Status = BookRender.StrProp(record, "status"),
            Decider = BookRender.StrProp(record, "decider"),
            DecideBy = BookRender.StrProp(record, "decideBy"),
            ApprovedBy = BookRender.StrProp(fields, "approvedBy"),
            ApprovedOn = BookRender.StrProp(fields, "approvedOn"),
            ApprovedHash = BookRender.StrProp(fields, "approvedHash"),
            Derived = BookRender.IsTrue(record["derived"]),
            File = relative.Length > 0
                ? Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)))
                : "",
            Line = record["line"] is JsonValue scalar && scalar.TryGetValue<int>(out var line) ? line : 0,
            Raw = document.RootElement.Clone(),
        };
    }

    /// <summary>Counts the open findings of the findings file by subject record id.</summary>
    static IReadOnlyDictionary<string, int> FindingsBySubject(JsonNode? findingsFile)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var finding in (findingsFile as JsonObject)?["findings"] as JsonArray ?? [])
        {
            if (finding is JsonObject obj && BookRender.StrProp(obj, "subject") is { Length: > 0 } subject)
            {
                counts[subject] = counts.TryGetValue(subject, out var count) ? count + 1 : 1;
            }
        }

        return counts;
    }
}
