using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Book;

/// <summary>
///     The review batches the queue shard shows: the batches the model carries,
///     or every draft and every reopened record from the book index, computed by
///     the shared review queue while the model holds none.
/// </summary>
/// <follows>litespec/core/release#compute-the-review-queue-from-the-model</follows>
static class BookReviewQueue
{
    /// <summary>Reads the review batches from the book index, in review order.</summary>
    /// <param name="index">The book index that holds the stored batches or the records to compute from.</param>
    /// <param name="root">The absolute consumer root, used to rebuild the project model when no batches are stored.</param>
    /// <param name="findingsFile">The findings node that holds the open findings by subject, or null.</param>
    /// <param name="today">The current date, against which decide-by dates are judged.</param>
    /// <returns>The review batches in review order, stored batches when the model carries them and computed batches otherwise.</returns>
    public static IReadOnlyList<BookBatch> Compute(BookIndex index, string root, JsonNode? findingsFile, DateOnly today)
    {
        if (index.StoredBatches is { } stored)
        {
            return StoredOf(stored);
        }

        var model = Rebuild(index, root);
        return ReviewQueue.ComputeBatches(
            model.Records,
            model.EffectiveStatus,
            model.Proves,
            model.Stale,
            FindingsBySubject(findingsFile),
            model.Config,
            today).Select(ToBatch).ToList();
    }

    /// <summary>One computed batch as a queue-shard batch: the selector, kind, tier and queued record ids.</summary>
    /// <param name="batch">The computed review batch from the shared review queue.</param>
    /// <returns>The queue-shard batch with the record identifiers of the computed batch records.</returns>
    static BookBatch ToBatch(ReviewBatch batch) =>
        new(
            batch.Selector,
            batch.Kind,
            batch.Tier,
            batch.Deciders,
            batch.DecideBy,
            batch.Awaiting,
            batch.Records.Select(entry => entry.Record.Id).ToList());

    /// <summary>The stored batches of a model carrying them, skipping entries that name no selector.</summary>
    /// <param name="stored">The stored batch entries from the model.</param>
    /// <returns>The stored batches in model order, skipping entries that name no selector.</returns>
    static List<BookBatch> StoredOf(JsonArray stored)
    {
        var batches = new List<BookBatch>();
        foreach (var entry in stored.OfType<JsonObject>())
        {
            if (BookRender.StrProp(entry, "selector") is not { Length: > 0 } selector)
            {
                continue;
            }

            var deciders = new List<string>();
            foreach (var decider in entry["deciders"] as JsonArray ?? [])
            {
                if (decider is JsonValue scalar && scalar.TryGetValue<string>(out var name) && name is { Length: > 0 })
                {
                    deciders.Add(name);
                }
            }

            var records = new List<string>();
            foreach (var record in entry["records"] as JsonArray ?? [])
            {
                if (record is JsonValue id && id.TryGetValue<string>(out var name) && name is { Length: > 0 })
                {
                    records.Add(name);
                }
            }

            batches.Add(new BookBatch(
                selector,
                BookRender.StrProp(entry, "kind") ?? "",
                entry["tier"] is JsonValue tier && tier.TryGetValue<int>(out var level) ? level : 3,
                deciders,
                BookRender.StrProp(entry, "decideBy"),
                entry["awaiting"] is JsonValue waiting && waiting.TryGetValue<int>(out var count) ? count : 0,
                records));
        }

        return batches;
    }

    /// <summary>Rebuilds the project model the queue helpers read from the book index.</summary>
    /// <param name="index">The book index that holds the records, proves links and stale identifiers.</param>
    /// <param name="root">The absolute consumer root the rebuilt records resolve their files against.</param>
    /// <returns>The rebuilt project model with its configuration, records, effective statuses, proves links and stale identifiers.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Loading the consumer configuration must not stop the queue rebuild; any failure falls back to a configuration loaded from the temp folder so the batches still compute.")]
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
    /// <param name="record">The model entry of the record, holding its resolved values and raw fields.</param>
    /// <param name="root">The absolute consumer root the record file path resolves against.</param>
    /// <returns>The rebuilt record with its file resolved against the root and its raw fields cloned.</returns>
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

    /// <summary>Counts the open findings recorded in the model by subject record id.</summary>
    /// <param name="findingsFile">The findings node that holds the findings array, or null.</param>
    /// <returns>The open finding count for each subject record identifier, empty when the node holds no findings.</returns>
    static Dictionary<string, int> FindingsBySubject(JsonNode? findingsFile)
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
