using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Book;







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
    /// <param name="model">The parsed model object that holds the records.</param>
    /// <param name="recordId">The identifier of the record whose parent is sought.</param>
    /// <returns>The parent identifier, or null when the record is unknown or no rule places it.</returns>
    public static string? ParentOf(JsonObject model, string recordId)
    {
        var index = BuildIndex(model);
        return index.ById.TryGetValue(recordId, out var record) ? ParentFor(index, record) : null;
    }

    /// <summary>The parent id per record id, built in one pass; a null value means no rule places the record.</summary>
    /// <param name="model">The parsed model object that holds the records.</param>
    /// <returns>The parent identifier for each record identifier, with null for a record no rule places.</returns>
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

    /// <summary>The lookup tables for parent resolution: records by identifier, the product identifier, and record identifiers by name segment per parent-bearing type.</summary>
    sealed class ParentIndex
    {
        /// <summary>Gets the record object for each record identifier, first record winning on a duplicate.</summary>
        public Dictionary<string, JsonObject> ById { get; } = new(StringComparer.Ordinal);
        /// <summary>Gets or sets the identifier of the product record, or null when the model holds no product.</summary>
        public string? ProductId { get; set; }
        /// <summary>Gets the use-case identifier for each use-case name segment, first record winning.</summary>
        public Dictionary<string, string> UseCasesByName { get; } = new(StringComparer.Ordinal);
        /// <summary>Gets the aggregate identifier for each aggregate name segment, first record winning.</summary>
        public Dictionary<string, string> AggregatesByName { get; } = new(StringComparer.Ordinal);
        /// <summary>Gets the module identifier for each module name segment, first record winning.</summary>
        public Dictionary<string, string> ModulesByName { get; } = new(StringComparer.Ordinal);
        /// <summary>Gets the entry-point identifier for each entry-point name segment, first record winning.</summary>
        public Dictionary<string, string> EntryPointsByName { get; } = new(StringComparer.Ordinal);
        /// <summary>Gets the topic identifier for each topic name segment, first record winning.</summary>
        public Dictionary<string, string> TopicsByName { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>Builds the parent lookup tables from the model records.</summary>
    /// <param name="model">The parsed model object that holds the records.</param>
    /// <returns>The populated lookup tables, empty when the model holds no records array.</returns>
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

    /// <summary>Finds the parent of one record by the rule for its type.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="record">The record object whose parent is sought.</param>
    /// <returns>The parent identifier, or null when the record lacks an identifier or type, its type has no parent rule, or the rule finds no parent.</returns>
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

    /// <summary>Resolves a link target to a parent when the target names a known record.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="target">The link target identifier, or null.</param>
    /// <returns>The target identifier when it is non-empty and known, otherwise null.</returns>
    static string? LinkParent(ParentIndex index, string? target) =>
        target is { Length: > 0 } && index.ById.ContainsKey(target) ? target : null;

    /// <summary>Finds the topic parent of a record from its identifier home.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="id">The record identifier whose home names the topic.</param>
    /// <returns>The topic identifier for the home, or null when the identifier has no home or no topic carries that name.</returns>
    static string? TopicParent(ParentIndex index, string id)
    {
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return index.TopicsByName.TryGetValue(home, out var topic) ? topic : null;
    }

    /// <summary>Finds the module parent of a record from its identifier home.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="id">The record identifier whose home names the module.</param>
    /// <returns>The module identifier for the home, or null when the identifier has no home or no module carries that name.</returns>
    static string? ModuleParent(ParentIndex index, string id)
    {
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return index.ModulesByName.TryGetValue(home, out var module) ? module : null;
    }

    /// <summary>Finds the parent of a use case: its stated aggregate when known, else its module from the identifier home.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="record">The use-case record object that may state its aggregate.</param>
    /// <param name="id">The use-case identifier whose home names the fallback module.</param>
    /// <returns>The aggregate or module identifier, or null when neither resolves.</returns>
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

    /// <summary>Finds the use-case parent of a record from its identifier home.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="id">The record identifier whose home names the use case.</param>
    /// <returns>The use-case identifier for the home, or null when the identifier has no home or no use case carries that name.</returns>
    static string? UseCaseNameParent(ParentIndex index, string id)
    {
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return index.UseCasesByName.TryGetValue(home, out var useCase) ? useCase : null;
    }

    /// <summary>Finds the parent of a scenario from its result link.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="record">The scenario record object that holds the result link.</param>
    /// <returns>The result identifier when the link names a known record, otherwise null.</returns>
    static string? ScenarioParent(ParentIndex index, JsonObject record)
    {
        // The shown result is the only parent; a missing one is reported, never defaulted.
        if (FirstLink(record, "result") is { } result && index.ById.ContainsKey(result))
        {
            return result;
        }

        return null;
    }

    /// <summary>Finds the parent of a rule from its identifier home, an aggregate before a module.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="id">The rule identifier whose home names the aggregate or module.</param>
    /// <returns>The aggregate identifier when one carries the home name, else the module identifier, or null when neither does.</returns>
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

    /// <summary>Finds the aggregate parent of a record from its identifier home.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="id">The record identifier whose home names the aggregate.</param>
    /// <returns>The aggregate identifier for the home, or null when the identifier has no home or no aggregate carries that name.</returns>
    static string? AggregateNameParent(ParentIndex index, string id)
    {
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return index.AggregatesByName.TryGetValue(home, out var aggregate) ? aggregate : null;
    }

    /// <summary>Finds the entry-point parent of a screen when its entry-point link agrees with its identifier home.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="record">The screen record object that holds the entry-point link.</param>
    /// <param name="id">The screen identifier whose home must agree with the link.</param>
    /// <returns>The entry-point identifier when the link is known and agrees with the home, otherwise null.</returns>
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

    /// <summary>Reads the first link value among the named fields, from links before fields.</summary>
    /// <param name="record">The record object that holds the links and fields.</param>
    /// <param name="names">The field names to try, in order.</param>
    /// <returns>The first non-empty link string among the named fields, or null when none holds one.</returns>
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

    /// <summary>Reads the first non-empty string from a node that holds one string or an array of strings.</summary>
    /// <param name="node">The node to read, or null.</param>
    /// <returns>The first non-empty string the node holds, or null when it holds none.</returns>
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

    /// <summary>Reads one string property of a JSON object.</summary>
    /// <param name="obj">The JSON object to read, or null.</param>
    /// <param name="name">The property name to read.</param>
    /// <returns>The string value of the property, or null when the object or property is missing or the value is not a string.</returns>
    static string? StrProp(JsonObject? obj, string name) => BookRender.StrProp(obj, name);
}
