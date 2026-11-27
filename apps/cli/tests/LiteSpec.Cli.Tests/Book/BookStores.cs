using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>
///     Gives a hand-written book store the Intent coverage the build carries beside its records. The build
///     computes the coverage from the built model; a store written by hand has no model, so this computes the
///     same status from the store's own records, with the same reach rules as <see cref="IntentCoverage" />.
/// </summary>
static class BookStores
{
    /// <summary>Adds the Intent coverage to a store that holds none, and returns the store.</summary>
    /// <param name="store">The store object that holds the records array.</param>
    /// <returns>The same store, with its Intent coverage set.</returns>
    public static JsonObject WithIntentCoverage(JsonObject store)
    {
        if (store["intentCoverage"] is not null)
        {
            return store;
        }

        var records = (store["records"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        var fulfilledBy = Reach();
        var servedBy = Reach();
        var answeredBy = Reach();

        foreach (var record in records)
        {
            var type = Text(record, "type");
            var id = Text(record, "id");
            if (RecordTypes.FulfillingTypes.Contains(type, StringComparer.Ordinal))
            {
                Add(fulfilledBy, IdsOf(record, "fulfills"), id);
                Add(fulfilledBy, ServedFulfills(record), id);
            }

            if (RecordTypes.StepServingTypes.Contains(type, StringComparer.Ordinal))
            {
                Add(servedBy, IdsOf(record, "serves"), id);
            }

            if (type == RecordTypes.Feature
                && RecordPlacement.ProblemOf(Text(record, "file")) is { Length: > 0 } problem)
            {
                Add(answeredBy, [$"problem/{problem}"], id);
            }

            if (type == RecordTypes.Flow)
            {
                Add(answeredBy, IdsOf(record, "solves"), id);
                Add(answeredBy, IdsOf(record, "basedOn"), id);
            }
        }

        var entries = new List<IntentCoverage.Entry>();
        foreach (var id in Ids(records, RecordTypes.Requirement))
        {
            entries.Add(Entry(id, "requirement", fulfilledBy));
        }

        foreach (var id in Ids(records, RecordTypes.Problem))
        {
            entries.Add(Entry(id, "problem", answeredBy));
        }

        foreach (var id in Ids(records, RecordTypes.Constraint))
        {
            entries.Add(Entry(id, "constraint", answeredBy));
        }

        foreach (var flow in records
            .Where(record => Text(record, "type") == RecordTypes.Flow)
            .OrderBy(record => Text(record, "id"), StringComparer.Ordinal))
        {
            foreach (var step in (flow["fields"]?["steps"] as JsonArray ?? []).OfType<JsonObject>())
            {
                entries.Add(Entry(Text(step, "id"), "step", servedBy));
            }
        }

        store["intentCoverage"] = BookIntentCoverage.ToJson(new IntentCoverage(entries));
        return store;
    }

    static SortedDictionary<string, SortedSet<string>> Reach() => new(StringComparer.Ordinal);

    static void Add(SortedDictionary<string, SortedSet<string>> map, IEnumerable<string> targets, string source)
    {
        foreach (var target in targets)
        {
            if (!map.TryGetValue(target, out var sources))
            {
                sources = new SortedSet<string>(StringComparer.Ordinal);
                map[target] = sources;
            }

            sources.Add(source);
        }
    }

    static IntentCoverage.Entry Entry(string id, string kind, SortedDictionary<string, SortedSet<string>> map)
    {
        var by = map.TryGetValue(id, out var sources) ? sources.ToList() : [];
        return new IntentCoverage.Entry(id, kind, by.Count > 0, by);
    }

    static List<string> Ids(IEnumerable<JsonObject> records, string type) =>
        records
            .Where(record => Text(record, "type") == type && Text(record, "id").Length > 0)
            .Select(record => Text(record, "id"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

    static List<string> IdsOf(JsonObject record, string name) => StringsOf(record["fields"]?[name]);

    static List<string> ServedFulfills(JsonObject record)
    {
        var targets = new List<string>();
        if (!string.Equals(Text(record, "type"), RecordTypes.EntryPoint, StringComparison.Ordinal))
        {
            return targets;
        }

        foreach (var entry in (record["fields"]?["servedContent"] as JsonArray ?? []).OfType<JsonObject>())
        {
            targets.AddRange(StringsOf(entry["fulfills"]));
        }

        return targets;
    }

    static List<string> StringsOf(JsonNode? value)
    {
        if (value is JsonValue scalar && scalar.TryGetValue<string>(out var text))
        {
            return text.Length > 0 ? [text] : [];
        }

        return (value as JsonArray ?? [])
            .OfType<JsonValue>()
            .Select(item => item.TryGetValue<string>(out var id) ? id : "")
            .Where(id => id.Length > 0)
            .ToList();
    }

    static string Text(JsonObject record, string name) =>
        record[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : "";
}
