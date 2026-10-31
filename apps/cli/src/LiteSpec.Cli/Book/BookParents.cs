using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Book;

/// <summary>One parent rule: the record type, the parent types it may sit under, and how the parent is found.</summary>
/// <param name="Type">The record type the rule covers.</param>
/// <param name="ParentTypes">The parent record types in precedence order; empty for the product root.</param>
/// <param name="FoundThrough">The phrase the Parents table uses, copied exactly.</param>
sealed record BookParentRule(string Type, IReadOnlyList<string> ParentTypes, string FoundThrough);

/// <summary>
///     The parent of every record: the product parents each root type, a feature follows its
///     first solves entry, a requirement follows its topic home, and containment follows
///     the identifier home. No product field is written.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookParents
{
    /// <summary>The parent rule per record type, keyed by type.</summary>
    public static readonly IReadOnlyDictionary<string, BookParentRule> Rules =
        new Dictionary<string, BookParentRule>(StringComparer.Ordinal)
        {
            ["product"] = new("product", [], "the project that declares it"),
            ["goal"] = new("goal", ["product"], "the product"),
            ["problem"] = new("problem", ["product"], "the product"),
            ["constraint"] = new("constraint", ["product"], "the product"),
            ["research"] = new("research", ["product"], "the product"),
            ["decision"] = new("decision", ["product"], "the product"),
            ["actor"] = new("actor", ["product"], "the product"),
            ["term"] = new("term", ["product"], "the product"),
            ["module"] = new("module", ["product"], "the product"),
            ["role"] = new("role", ["product"], "the product"),
            ["permission"] = new("permission", ["product"], "the product"),
            ["entry-point"] = new("entry-point", ["product"], "the product"),
            ["external-system"] = new("external-system", ["product"], "the product"),
            ["runbook"] = new("runbook", ["product"], "the product"),
            ["integration-event"] = new("integration-event", ["product"], "the product"),
            ["exception"] = new("exception", ["product"], "the product"),
            ["feature"] = new("feature", ["problem"], "`solves`, first entry"),
            ["topic"] = new("topic", ["product"], "the product"),
            ["requirement"] = new("requirement", ["topic"], "identifier home"),
            ["journey"] = new("journey", ["goal"], "`serves`"),
            ["aggregate"] = new("aggregate", ["module"], "identifier home"),
            ["use-case"] = new("use-case", ["aggregate", "module"], "the aggregate holding its page; else identifier home"),
            ["result"] = new("result", ["use-case"], "identifier home"),
            ["scenario"] = new("scenario", ["result"], "`result`"),
            ["rule"] = new("rule", ["aggregate", "module"], "identifier home, an aggregate before a module"),
            ["event"] = new("event", ["aggregate"], "identifier home"),
            ["reaction"] = new("reaction", ["module"], "identifier home"),
            ["job"] = new("job", ["module"], "identifier home"),
            ["notification"] = new("notification", ["module"], "identifier home"),
            ["setting"] = new("setting", ["module"], "identifier home"),
            ["screen"] = new("screen", ["entry-point"], "`entryPoint`, agreeing with the identifier home"),
        };

    /// <summary>The parent id of one record, or null when no rule places it.</summary>
    public static string? ParentOf(JsonObject model, string recordId)
    {
        var index = BuildIndex(model);
        return index.ById.TryGetValue(recordId, out var record) ? ParentFor(index, record) : null;
    }

    /// <summary>The parent id per record id, built in one pass; a null value means no rule places the record.</summary>
    public static IReadOnlyDictionary<string, string?> ParentsOf(JsonObject model)
    {
        var index = BuildIndex(model);
        var parents = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var pair in index.ById)
        {
            parents[pair.Key] = ParentFor(index, pair.Value);
        }

        return parents;
    }

    sealed class ParentIndex
    {
        public Dictionary<string, JsonObject> ById { get; } = new(StringComparer.Ordinal);
        public string? ProductId { get; set; }
        public Dictionary<string, string> UseCasesByName { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> AggregatesByName { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> ModulesByName { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> EntryPointsByName { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> TopicsByName { get; } = new(StringComparer.Ordinal);
    }

    static ParentIndex BuildIndex(JsonObject model)
    {
        var index = new ParentIndex();
        if (model["records"] is not JsonArray records)
        {
            return index;
        }

        foreach (var entry in records.OfType<JsonObject>())
        {
            var id = StrProp(entry, "id");
            if (id is not { Length: > 0 } || index.ById.ContainsKey(id))
            {
                continue;
            }

            index.ById[id] = entry;
            if (index.ProductId is null && string.Equals(StrProp(entry, "type"), "product", StringComparison.Ordinal))
            {
                index.ProductId = id;
            }
        }

        foreach (var pair in index.ById)
        {
            var type = StrProp(pair.Value, "type");
            if (!Identifiers.TrySplit(pair.Key, out _, out _, out var name) || name is not { Length: > 0 })
            {
                continue;
            }

            if (string.Equals(type, "use-case", StringComparison.Ordinal))
            {
                index.UseCasesByName.TryAdd(name, pair.Key);
            }
            else if (string.Equals(type, "aggregate", StringComparison.Ordinal))
            {
                index.AggregatesByName.TryAdd(name, pair.Key);
            }
            else if (string.Equals(type, "module", StringComparison.Ordinal))
            {
                index.ModulesByName.TryAdd(name, pair.Key);
            }
            else if (string.Equals(type, "entry-point", StringComparison.Ordinal))
            {
                index.EntryPointsByName.TryAdd(name, pair.Key);
            }
            else if (string.Equals(type, "topic", StringComparison.Ordinal))
            {
                index.TopicsByName.TryAdd(name, pair.Key);
            }
        }

        return index;
    }

    static string? ParentFor(ParentIndex index, JsonObject record)
    {
        var id = StrProp(record, "id");
        var type = StrProp(record, "type");
        if (id is not { Length: > 0 } || type is not { Length: > 0 })
        {
            return null;
        }

        string? parent = type switch
        {
            "product" => null,
            "goal" or "problem" or "constraint" or "research" or "decision" or "topic" or "actor" or "term" or "module" or "role" or "permission" or "entry-point" or "external-system" or "runbook" or "integration-event" or "exception" => index.ProductId,
            "feature" => LinkParent(index, FirstLink(record, "solves")),
            "requirement" => TopicParent(index, id),
            "journey" => LinkParent(index, FirstLink(record, "serves")),
            "aggregate" => ModuleParent(index, id),
            "use-case" => UseCaseParent(index, record, id),
            "result" => UseCaseNameParent(index, id),
            "scenario" => ScenarioParent(index, record),
            "rule" => RuleParent(index, id),
            "event" => AggregateNameParent(index, id),
            "reaction" or "job" or "notification" or "setting" => ModuleParent(index, id),
            "screen" => ScreenParent(index, record, id),
            _ => null,
        };

        // A record never parents itself.
        return string.Equals(parent, id, StringComparison.Ordinal) ? null : parent;
    }

    static string? LinkParent(ParentIndex index, string? target) =>
        target is { Length: > 0 } && index.ById.ContainsKey(target) ? target : null;

    static string? TopicParent(ParentIndex index, string id)
    {
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return index.TopicsByName.TryGetValue(home, out var topic) ? topic : null;
    }

    static string? ModuleParent(ParentIndex index, string id)
    {
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return index.ModulesByName.TryGetValue(home, out var module) ? module : null;
    }

    static string? UseCaseParent(ParentIndex index, JsonObject record, string id)
    {
        // The computed aggregate field wins; the identifier home is the module fallback.
        var aggregate = StrProp(record, "aggregate");
        if (aggregate is { Length: > 0 } && index.ById.ContainsKey(aggregate))
        {
            return aggregate;
        }

        return ModuleParent(index, id);
    }

    static string? UseCaseNameParent(ParentIndex index, string id)
    {
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return index.UseCasesByName.TryGetValue(home, out var useCase) ? useCase : null;
    }

    static string? ScenarioParent(ParentIndex index, JsonObject record)
    {
        // The shown result is the only parent; a missing one is reported, never defaulted.
        if (FirstLink(record, "result") is { } result && index.ById.ContainsKey(result))
        {
            return result;
        }

        return null;
    }

    static string? RuleParent(ParentIndex index, string id)
    {
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        // An aggregate home wins over a module home.
        if (index.AggregatesByName.TryGetValue(home, out var aggregate))
        {
            return aggregate;
        }

        return index.ModulesByName.TryGetValue(home, out var module) ? module : null;
    }

    static string? AggregateNameParent(ParentIndex index, string id)
    {
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return index.AggregatesByName.TryGetValue(home, out var aggregate) ? aggregate : null;
    }

    static string? ScreenParent(ParentIndex index, JsonObject record, string id)
    {
        var entryPoint = FirstLink(record, "entryPoint");
        if (entryPoint is not { Length: > 0 } || !index.ById.ContainsKey(entryPoint))
        {
            return null;
        }

        // The field and the identifier home must agree on one entry point.
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return string.Equals(entryPoint, "entry-point/" + home, StringComparison.Ordinal) ? entryPoint : null;
    }

    static string? FirstLink(JsonObject record, params string[] names)
    {
        var links = record["links"] as JsonObject;
        var fields = record["fields"] as JsonObject;
        foreach (var name in names)
        {
            if (FirstString(links?[name]) is { } fromLinks)
            {
                return fromLinks;
            }

            if (FirstString(fields?[name]) is { } fromFields)
            {
                return fromFields;
            }
        }

        return null;
    }

    static string? FirstString(JsonNode? node)
    {
        if (node is JsonValue scalar && scalar.TryGetValue<string>(out var text) && text is { Length: > 0 })
        {
            return text;
        }

        if (node is JsonArray items)
        {
            foreach (var item in items)
            {
                if (item is JsonValue element && element.TryGetValue<string>(out var value) && value is { Length: > 0 })
                {
                    return value;
                }
            }
        }

        return null;
    }

    static string? StrProp(JsonObject? obj, string name) => BookRender.StrProp(obj, name);
}
