using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Book;







/// <summary>
///     The parent of every record, following the Placement Table on the model page:
///     the container is the parent, so the folder node wins and the identifier
///     home is the fallback, never a written link. A record file's parent is its
///     folder node: the product, a module, an aggregate or an entry point. An
///     entry's parent is the record file that holds it, except a scenario, whose
///     parent is its result.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookParents
{
    /// <summary>The parent rule per record type, keyed by type, with the Placement Table's Parent cell copied exactly.</summary>
    public static readonly IReadOnlyDictionary<string, BookParentRule> Rules =
        new Dictionary<string, BookParentRule>(StringComparer.Ordinal)
        {
            ["product"] = new("product", [], "none"),
            ["goal"] = new("goal", ["product", "module"], "product or module"),
            ["problem"] = new("problem", ["product", "module"], "product or module"),
            ["constraint"] = new("constraint", ["product", "module"], "product or module"),
            ["research"] = new("research", ["product", "module"], "product or module"),
            ["decision"] = new("decision", ["product", "module"], "product or module"),
            ["actor"] = new("actor", ["product"], "product"),
            ["term"] = new("term", ["product"], "product"),
            ["module"] = new("module", ["product"], "product"),
            ["role"] = new("role", ["product"], "product"),
            ["journey"] = new("journey", ["product"], "product"),
            ["entry-point"] = new("entry-point", ["product"], "product"),
            ["feature"] = new("feature", ["module"], "module"),
            ["permission"] = new("permission", ["module"], "module"),
            ["integration-event"] = new("integration-event", ["module"], "module"),
            ["external-system"] = new("external-system", ["product", "module"], "its folder node"),
            ["runbook"] = new("runbook", ["product", "module"], "product or module"),
            ["departure"] = new("departure", ["product", "module"], "product or module"),
            ["requirement"] = new("requirement", ["feature"], "feature"),
            ["aggregate"] = new("aggregate", ["module"], "module"),
            ["use-case"] = new("use-case", ["aggregate", "module"], "aggregate or module"),
            ["result"] = new("result", ["use-case"], "use case"),
            ["scenario"] = new("scenario", ["result"], "result"),
            ["rule"] = new("rule", ["aggregate", "module", "product"], "its folder node"),
            ["event"] = new("event", ["aggregate"], "aggregate"),
            ["reaction"] = new("reaction", ["module"], "module"),
            ["job"] = new("job", ["module"], "module"),
            ["notification"] = new("notification", ["module"], "module"),
            ["setting"] = new("setting", ["module"], "module"),
            ["screen"] = new("screen", ["entry-point"], "entry point"),
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
        /// <summary>Gets the feature identifier for each feature name segment, first record winning.</summary>
        public Dictionary<string, string> FeaturesByName { get; } = new(StringComparer.Ordinal);
        /// <summary>Gets the aggregate identifier for each aggregate file directory, first record winning.</summary>
        public Dictionary<string, string> AggregatesByDir { get; } = new(StringComparer.Ordinal);
        /// <summary>Gets the entry-point identifier for each entry-point file directory, first record winning.</summary>
        public Dictionary<string, string> EntryPointsByDir { get; } = new(StringComparer.Ordinal);
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
            if (string.Equals(type, "aggregate", StringComparison.Ordinal)
                && DirOf(StrProp(pair.Value, "file")) is { } aggregateDir)
            {
                index.AggregatesByDir.TryAdd(aggregateDir, pair.Key);
            }
            else if (string.Equals(type, "entry-point", StringComparison.Ordinal)
                && DirOf(StrProp(pair.Value, "file")) is { } entryDir)
            {
                index.EntryPointsByDir.TryAdd(entryDir, pair.Key);
            }

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
            else if (string.Equals(type, "feature", StringComparison.Ordinal))
            {
                index.FeaturesByName.TryAdd(name, pair.Key);
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
            "goal" or "problem" or "constraint" or "research" or "decision" or "runbook" or "departure" or "external-system" => ModuleFromFile(index, record) ?? index.ProductId,
            "module" or "role" or "journey" or "actor" or "term" or "entry-point" => index.ProductId,
            "feature" or "permission" or "integration-event" => ModuleFromFile(index, record),
            "requirement" => FeatureParent(index, id),
            "aggregate" => ModuleFromFile(index, record) ?? ModuleParent(index, id),
            "use-case" => UseCaseParent(index, record, id),
            "result" => UseCaseNameParent(index, id),
            "scenario" => ScenarioParent(index, record),
            "rule" => RuleParent(index, record, id),
            "event" => AggregateFromFile(index, record) ?? AggregateNameParent(index, id),
            "reaction" or "job" or "notification" or "setting" => ModuleFromFile(index, record) ?? ModuleParent(index, id),
            "screen" => EntryPointFromFile(index, record) ?? ScreenParent(index, id),
            _ => null,
        };

        // A record never parents itself.
        return string.Equals(parent, id, StringComparison.Ordinal) ? null : parent;
    }

    /// <summary>Finds the module parent of a record from its file: the module its folder sits under.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="record">The record object that holds the file path.</param>
    /// <returns>The module identifier for the folder, or null when the file names no known module.</returns>
    static string? ModuleFromFile(ParentIndex index, JsonObject record)
    {
        var file = StrProp(record, "file");
        if (file is not { Length: > 0 })
        {
            return null;
        }

        var segments = file.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var at = 0; at + 1 < segments.Length; at++)
        {
            if (string.Equals(segments[at], "modules", StringComparison.Ordinal)
                && index.ModulesByName.TryGetValue(segments[at + 1], out var module))
            {
                return module;
            }
        }

        return null;
    }

    /// <summary>Finds the aggregate parent of a record from its file: the aggregate whose folder holds it.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="record">The record object that holds the file path.</param>
    /// <returns>The aggregate identifier for the folder, or null when the file sits in no aggregate folder.</returns>
    static string? AggregateFromFile(ParentIndex index, JsonObject record)
    {
        var file = StrProp(record, "file");
        if (file is not { Length: > 0 } || !UnderBehaviorModules(file))
        {
            return null;
        }

        // A use case or an event sits beside aggregate.yaml; a rule sits one
        // level down in rules/, so its parent folder holds the aggregate.
        // The folders match, never their names, so a plural folder still holds
        // the aggregate its aggregate.yaml names.
        var dir = DirOf(file);
        if (dir is not null && index.AggregatesByDir.TryGetValue(dir, out var same))
        {
            return same;
        }

        var parent = dir is null ? null : ParentDirOf(dir);
        return parent is not null && index.AggregatesByDir.TryGetValue(parent, out var up) ? up : null;
    }

    /// <summary>Finds the entry-point parent of a record from its file: the entry point whose folder holds it.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="record">The record object that holds the file path.</param>
    /// <returns>The entry-point identifier for the folder, or null when the file sits in no entry-point folder.</returns>
    static string? EntryPointFromFile(ParentIndex index, JsonObject record)
    {
        var file = StrProp(record, "file");
        if (file is not { Length: > 0 })
        {
            return null;
        }

        // A screen sits in screens/ beside its entry-point file, so its parent
        // folder holds the entry point; the folder name is the fallback.
        var dir = DirOf(file);
        if (dir is not null && index.EntryPointsByDir.TryGetValue(dir, out var same))
        {
            return same;
        }

        var parent = dir is null ? null : ParentDirOf(dir);
        if (parent is not null && index.EntryPointsByDir.TryGetValue(parent, out var holder))
        {
            return holder;
        }

        var segments = file.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var at = 0; at + 1 < segments.Length; at++)
        {
            if (string.Equals(segments[at], "entry-points", StringComparison.Ordinal)
                && index.EntryPointsByName.TryGetValue(segments[at + 1], out var named))
            {
                return named;
            }
        }

        return null;
    }

    /// <summary>Whether the file sits under a behavior modules tree, where aggregates live.</summary>
    /// <param name="file">The record file path with any separator.</param>
    /// <returns>True when one path segment is behavior and the next is modules.</returns>
    static bool UnderBehaviorModules(string file)
    {
        var segments = file.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var at = 0; at + 1 < segments.Length; at++)
        {
            if (string.Equals(segments[at], "behavior", StringComparison.Ordinal)
                && string.Equals(segments[at + 1], "modules", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the file sits under a behavior tree.</summary>
    /// <param name="file">The record file path with any separator.</param>
    /// <returns>True when one path segment is behavior.</returns>
    static bool IsBehaviorFile(string file) =>
        file.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Contains("behavior", StringComparer.Ordinal);

    /// <summary>The directory of a record file path, with forward slashes, or null when the file is missing.</summary>
    /// <param name="file">The record file path with any separator, or null.</param>
    /// <returns>The directory part before the last slash, the empty string when the file holds no slash, or null when the file is missing.</returns>
    static string? DirOf(string? file)
    {
        if (file is not { Length: > 0 })
        {
            return null;
        }

        var path = file.Replace('\\', '/');
        var slash = path.LastIndexOf('/');
        return slash < 0 ? "" : path[..slash];
    }

    /// <summary>The parent directory of a directory path, or null at the root.</summary>
    /// <param name="dir">The directory path with forward slashes.</param>
    /// <returns>The directory part before the last slash, or null when the directory holds no slash.</returns>
    static string? ParentDirOf(string dir)
    {
        var slash = dir.LastIndexOf('/');
        return slash < 0 ? null : dir[..slash];
    }

    /// <summary>Finds the feature parent of a record from its identifier home.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="id">The record identifier whose home names the feature.</param>
    /// <returns>The feature identifier for the home, or null when the identifier has no home or no feature carries that name.</returns>
    static string? FeatureParent(ParentIndex index, string id)
    {
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return index.FeaturesByName.TryGetValue(home, out var feature) ? feature : null;
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

    /// <summary>Finds the parent of a use case: its folder node, else its module from the identifier home.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="record">The use-case record object that may state its aggregate and holds its file path.</param>
    /// <param name="id">The use-case identifier whose home names the fallback module.</param>
    /// <returns>The aggregate or module identifier, or null when neither resolves.</returns>
    static string? UseCaseParent(ParentIndex index, JsonObject record, string id)
    {
        // The computed aggregate field wins; it names the aggregate whose
        // folder holds the use case page. The file is next, then the home.
        var aggregate = StrProp(record, "aggregate");
        if (aggregate is { Length: > 0 } && index.ById.ContainsKey(aggregate))
        {
            return aggregate;
        }

        return AggregateFromFile(index, record)
            ?? ModuleFromFile(index, record)
            ?? ModuleParent(index, id);
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

    /// <summary>Finds the parent of a rule from its folder node, else from its identifier home: an aggregate before a module, a product-level invariant under the product.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="record">The rule record object that holds the file path.</param>
    /// <param name="id">The rule identifier whose home names the fallback aggregate or module.</param>
    /// <returns>The folder node identifier, else the aggregate or module identifier for the home, else the product, or null when neither the folder nor the home places it.</returns>
    static string? RuleParent(ParentIndex index, JsonObject record, string id)
    {
        // The folder node wins, so the entries of a rule-set file take the
        // file's node; a rule under behavior but in no module or aggregate
        // folder is a product-level invariant. The home is the fallback.
        if (AggregateFromFile(index, record) is { } aggregate)
        {
            return aggregate;
        }

        if (ModuleFromFile(index, record) is { } module)
        {
            return module;
        }

        var file = StrProp(record, "file");
        if (file is { Length: > 0 } && IsBehaviorFile(file))
        {
            return index.ProductId;
        }

        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        // An aggregate home wins over a module home; a home naming neither is
        // a product-level invariant, the one stated exception.
        if (index.AggregatesByName.TryGetValue(home, out var named))
        {
            return named;
        }

        return index.ModulesByName.TryGetValue(home, out var holder) ? holder : index.ProductId;
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

    /// <summary>Finds the entry-point parent of a screen from its identifier home.</summary>
    /// <param name="index">The parent lookup tables for the model.</param>
    /// <param name="id">The screen identifier whose home names the entry point.</param>
    /// <returns>The entry-point identifier for the home, or null when the identifier has no home or no entry point carries that name.</returns>
    static string? ScreenParent(ParentIndex index, string id)
    {
        if (!Identifiers.TrySplit(id, out _, out var home, out _) || home is not { Length: > 0 })
        {
            return null;
        }

        return index.EntryPointsByName.TryGetValue(home, out var entryPoint) ? entryPoint : null;
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
