using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     Builds the index over model.json for the book generator. Pure data shaping, no HTML.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookIndexBuilder
{
    /// <summary>Builds the index over a parsed model.json object.</summary>
    /// <param name="model">The parsed model.</param>
    /// <returns>The built index.</returns>
    public static BookIndex BuildIndex(JsonObject model)
    {
        var records = model["records"] as JsonArray ?? [];
        var index = new BookIndex { Records = records.ToList() };

        foreach (var record in records.OfType<JsonObject>())
        {
            if (BookRender.StrProp(record, "id") is { } id && !index.ById.ContainsKey(id))
            {
                index.ById[id] = record;
            }
        }

        foreach (var record in records.OfType<JsonObject>())
        {
            if (BookRender.StrProp(record, "type") is not { } type)
            {
                continue;
            }

            if (!index.ByType.TryGetValue(type, out var list))
            {
                list = [];
                index.ByType[type] = list;
            }

            list.Add(record);
        }

        // Ordinal title order with the id as tie-break, so the order is the same on every machine.
        foreach (var type in index.ByType.Keys.ToList())
        {
            index.ByType[type] = index.ByType[type]
                .OrderBy(record => BookModel.SortTitle(record), BookRender.OrdinalComparer)
                .ThenBy(record => BookRender.StrProp(record, "id") ?? "", StringComparer.Ordinal)
                .ToList();
        }

        foreach (var record in records.OfType<JsonObject>())
        {
            // Links arrive as a field map or as a list; a list reads by position.
            IEnumerable<(string Field, JsonNode? Targets)> entries = record["links"] switch
            {
                JsonObject linkMap => linkMap.Select(pair => (pair.Key, pair.Value)),
                JsonArray linkList => linkList.Select((targets, position) => (position.ToString(), targets)),
                _ => [],
            };
            foreach (var (field, targets) in entries)
            {
                if (targets is not JsonArray ids)
                {
                    continue;
                }

                foreach (var target in ids)
                {
                    if (target is JsonValue scalar && scalar.TryGetValue<string>(out var id) && id is { Length: > 0 })
                    {
                        if (!index.Backlinks.TryGetValue(id, out var from))
                        {
                            from = [];
                            index.Backlinks[id] = from;
                        }

                        from.Add(new Backlink(BookRender.StrProp(record, "id") ?? "", field));

                        // A step or operation target also links its carrying record, so the
                        // journey and system pages list what serves and calls them.
                        var hash = id.IndexOf('#');
                        if (hash > 0 && id[..hash] is { Length: > 0 } baseId && baseId != id)
                        {
                            if (!index.Backlinks.TryGetValue(baseId, out var baseFrom))
                            {
                                baseFrom = [];
                                index.Backlinks[baseId] = baseFrom;
                            }

                            baseFrom.Add(new Backlink(BookRender.StrProp(record, "id") ?? "", field));
                        }
                    }
                }
            }
        }

        foreach (var record in records.OfType<JsonObject>())
        {
            var type = BookRender.StrProp(record, "type");
            if (type != "result" && type != "scenario")
            {
                continue;
            }

            if (BookRender.StrProp(record, "id") is not { } id)
            {
                continue;
            }

            // A derived generic result carries its use case as home.name before the
            // forbidden or invalid-input suffix, so it files under the use case name.
            var key = BookModel.SplitId(id).Home;
            if (type == "result" && BookRender.IsTrue(record["derived"]) && DerivedHolder(id) is { } holder)
            {
                key = holder;
            }

            if (key is not { Length: > 0 })
            {
                continue;
            }

            var map = type == "result" ? index.ResultsOf : index.ScenariosOf;
            if (!map.TryGetValue(key, out var list))
            {
                list = [];
                map[key] = list;
            }

            list.Add(record);
        }

        var links = model["links"] as JsonObject ?? new JsonObject();
        foreach (var link in links["implements"] as JsonArray ?? [])
        {
            if (link is JsonObject obj && BookRender.StrProp(obj, "to") is { } to)
            {
                if (!index.ImplementsByTarget.TryGetValue(to, out var list))
                {
                    list = [];
                    index.ImplementsByTarget[to] = list;
                }

                list.Add(obj);
            }
        }

        foreach (var link in links["calls"] as JsonArray ?? [])
        {
            if (link is JsonObject obj && BookRender.StrProp(obj, "to") is { } to)
            {
                if (!index.CallsByTarget.TryGetValue(to, out var list))
                {
                    list = [];
                    index.CallsByTarget[to] = list;
                }

                list.Add(obj);
            }
        }

        foreach (var link in links["proves"] as JsonArray ?? [])
        {
            if (link is JsonObject obj && BookRender.StrProp(obj, "claim") is { } claim)
            {
                if (!index.ProvesByClaim.TryGetValue(claim, out var list))
                {
                    list = [];
                    index.ProvesByClaim[claim] = list;
                }

                list.Add(obj);
            }
        }

        index.Tests = (model["tests"] as JsonArray ?? []).ToList();
        foreach (var test in index.Tests.OfType<JsonObject>())
        {
            if (BookRender.StrProp(test, "scenario") is { } scenario && !index.TestsByName.ContainsKey(scenario))
            {
                index.TestsByName[scenario] = test;
            }
        }

        index.Screens = (model["screens"] as JsonArray ?? []).ToList();
        foreach (var screen in index.Screens.OfType<JsonObject>())
        {
            if (BookRender.StrProp(screen, "frontend") is { } frontend && BookRender.StrProp(screen, "route") is { } route)
            {
                index.ScreensByKey[$"{frontend} {route}"] = screen;
            }
        }

        index.Status = model["status"] as JsonObject ?? new JsonObject();
        foreach (var pair in model["requirementStatus"] as JsonObject ?? new JsonObject())
        {
            if (pair.Value is JsonObject entry)
            {
                index.RequirementStatus[pair.Key] = entry;
            }
        }

        foreach (var pair in model["reachableStates"] as JsonObject ?? new JsonObject())
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in pair.Value as JsonArray ?? [])
            {
                if (name is JsonValue scalar && scalar.TryGetValue<string>(out var state) && state is not null)
                {
                    names.Add(state);
                }
            }

            index.ReachableStates[pair.Key] = names;
        }

        foreach (var stale in model["stale"] as JsonArray ?? [])
        {
            if (stale is JsonValue scalar && scalar.TryGetValue<string>(out var id) && id is not null)
            {
                index.StaleIds.Add(id);
            }
        }

        index.TestRun = model["testRun"] is { } run && BookRender.IsTruthy(run) && run is JsonObject or JsonArray ? run : null;

        foreach (var step in model["exercisedSteps"] as JsonArray ?? [])
        {
            if (step is JsonValue scalar && scalar.TryGetValue<string>(out var id) && id is { Length: > 0 })
            {
                index.ExercisedSteps.Add(id);
            }
        }

        foreach (var pair in model["tested"] as JsonObject ?? new JsonObject())
        {
            if (pair.Value is JsonValue scalar && scalar.TryGetValue<bool>(out var tested))
            {
                index.Tested[pair.Key] = tested;
            }
        }

        foreach (var pair in model["scenarioStatus"] as JsonObject ?? new JsonObject())
        {
            if (pair.Value is JsonValue state
                && state.TryGetValue<string>(out var words)
                && words is { Length: > 0 }
                && !index.ScenarioStatus.ContainsKey(pair.Key))
            {
                index.ScenarioStatus[pair.Key] = words;
            }
        }

        index.StoredBatches = model["reviewBatches"] as JsonArray;
        index.SecurityReviewer = BookRender.StrProp(model["review"] as JsonObject, "securityReviewer");

        index.HasFindings = model["findings"] is JsonArray or JsonObject || model["known"] is JsonArray;
        index.VetoWindowDays = VetoWindowOf(model);

        return index;
    }

    /// <summary>The veto window in days: the model's review value, or 7 when absent.</summary>
    /// <param name="model">The parsed model.</param>
    /// <returns>The veto window in days.</returns>
    public static int VetoWindowOf(JsonObject model)
    {
        JsonNode? value = (model["review"] as JsonObject)?["vetoWindowDays"] ?? model["vetoWindowDays"];
        if (value is JsonValue scalar && scalar.TryGetValue<int>(out var days) && days > 0)
        {
            return days;
        }

        if (value is JsonValue text && text.TryGetValue<string>(out var words)
            && int.TryParse(words, out var parsed) && parsed > 0)
        {
            return parsed;
        }

        return 7;
    }

    /// <summary>The use-case name holding a derived generic result, or null when the id is not derived.</summary>
    /// <param name="id">The record id.</param>
    /// <returns>The holding use-case name, or null.</returns>
    public static string? DerivedHolder(string id)
    {
        const string prefix = "result/";
        if (!id.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var rest = id[prefix.Length..];
        foreach (var suffix in new[] { ".forbidden", ".invalid-input" })
        {
            if (rest.EndsWith(suffix, StringComparison.Ordinal))
            {
                var holder = rest[..^suffix.Length];
                return holder.Length > 0 && !holder.Contains('.') ? holder : null;
            }
        }

        return null;
    }
}
