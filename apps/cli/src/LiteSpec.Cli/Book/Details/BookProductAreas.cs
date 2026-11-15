using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Book.Output;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Book.Details;

/// <summary>
///     The Intent product-area grouping: for each Behavior module, the features whose
///     requirements the module's use cases, reactions, jobs or screens fulfill, and the
///     flows whose steps they serve. A screen counts toward the modules of the use cases
///     it offers. A feature or flow several modules reach is listed under each; anything
///     no module reaches is listed unfulfilled. The book computes the grouping from the
///     records, so Intent carries no module field for it.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookProductAreas
{
    /// <summary>Gets the record types whose fulfills and serves links place Intent under a product area.</summary>
    static HashSet<string> ReachingTypes { get; } = new(StringComparer.Ordinal)
    {
        "use-case", "reaction", "job", "screen",
    };

    /// <summary>Builds the product-area grouping over a parsed model object.</summary>
    /// <param name="store">The parsed model object that holds the records array.</param>
    /// <returns>The grouping: one entry per Behavior module in identifier order, with the unfulfilled features and flows.</returns>
    public static ProductAreas Build(JsonObject store)
    {
        var records = (store["records"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        var byId = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            var id = BookRender.StrProp(record, "id");
            if (id is { Length: > 0 } && !byId.ContainsKey(id))
            {
                byId[id] = record;
            }
        }

        var modules = byId.Values
            .Where(record => string.Equals(BookRender.StrProp(record, "type"), "module", StringComparison.Ordinal))
            .Select(record => BookRender.StrProp(record, "id")!)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        var moduleIds = new HashSet<string>(modules, StringComparer.Ordinal);

        var fulfilled = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var served = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            var type = BookRender.StrProp(record, "type");
            if (type is null || !ReachingTypes.Contains(type))
            {
                continue;
            }

            var reached = ModulesOf(record, byId, moduleIds);
            if (reached.Count == 0)
            {
                continue;
            }

            var fields = BookRender.Fields(record);
            foreach (var target in BookDetailValues.StringsOf(fields?["fulfills"]))
            {
                Add(fulfilled, target, reached);
            }

            foreach (var target in BookDetailValues.StringsOf(fields?["serves"]))
            {
                if (target.Contains('#'))
                {
                    Add(served, target, reached);
                }
            }
        }

        var parents = BookParents.ParentsOf(store);
        var requirementsOf = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            if (!string.Equals(BookRender.StrProp(record, "type"), "requirement", StringComparison.Ordinal))
            {
                continue;
            }

            var id = BookRender.StrProp(record, "id");
            if (id is not { Length: > 0 }
                || !parents.TryGetValue(id, out var parent)
                || parent is not { Length: > 0 })
            {
                continue;
            }

            if (!requirementsOf.TryGetValue(parent, out var list))
            {
                list = [];
                requirementsOf[parent] = list;
            }

            list.Add(id);
        }

        var features = IdsOf(byId, "feature");
        var flows = IdsOf(byId, "flow");
        var featureModules = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var feature in features)
        {
            var reached = new HashSet<string>(StringComparer.Ordinal);
            foreach (var requirement in requirementsOf.TryGetValue(feature, out var owned) ? owned : [])
            {
                if (fulfilled.TryGetValue(requirement, out var sources))
                {
                    reached.UnionWith(sources);
                }
            }

            featureModules[feature] = reached;
        }

        var flowModules = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var flow in flows)
        {
            var reached = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in StepIds(byId[flow], flow))
            {
                if (served.TryGetValue(step, out var sources))
                {
                    reached.UnionWith(sources);
                }
            }

            flowModules[flow] = reached;
        }

        var areas = new List<ProductArea>();
        foreach (var module in modules)
        {
            var title = BookRender.StrProp(byId[module], "title");
            areas.Add(new ProductArea(
                module,
                title is { Length: > 0 } ? title : module,
                features.Where(feature => featureModules[feature].Contains(module)).ToList(),
                flows.Where(flow => flowModules[flow].Contains(module)).ToList()));
        }

        return new ProductAreas(
            areas,
            new UnfulfilledArea(
                features.Where(feature => featureModules[feature].Count == 0).ToList(),
                flows.Where(flow => flowModules[flow].Count == 0).ToList()));
    }

    /// <summary>One product-area grouping as the product-areas shard holds it.</summary>
    /// <param name="areas">The product-area grouping.</param>
    /// <returns>The JSON object with one entry per module and the unfulfilled features and flows.</returns>
    public static JsonObject ToJson(ProductAreas areas)
    {
        var modules = new JsonArray();
        foreach (var area in areas.Modules)
        {
            modules.Add((JsonNode)new JsonObject
            {
                ["id"] = area.Id,
                ["title"] = area.Title,
                ["features"] = new JsonArray([.. area.Features.Select(feature => (JsonNode?)feature)]),
                ["flows"] = new JsonArray([.. area.Flows.Select(flow => (JsonNode?)flow)]),
            });
        }

        return new JsonObject
        {
            ["modules"] = modules,
            ["unfulfilled"] = new JsonObject
            {
                ["features"] = new JsonArray([.. areas.Unfulfilled.Features.Select(feature => (JsonNode?)feature)]),
                ["flows"] = new JsonArray([.. areas.Unfulfilled.Flows.Select(flow => (JsonNode?)flow)]),
            },
        };
    }

    /// <summary>The record ids of one record type in identifier order.</summary>
    /// <param name="byId">The record object for each record identifier.</param>
    /// <param name="type">The record type to list.</param>
    /// <returns>The identifiers of the records of that type, in identifier order.</returns>
    static List<string> IdsOf(Dictionary<string, JsonObject> byId, string type) =>
        byId.Values
            .Where(record => string.Equals(BookRender.StrProp(record, "type"), type, StringComparison.Ordinal))
            .Select(record => BookRender.StrProp(record, "id")!)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

    /// <summary>The step ids of one flow in step order, each as flow id, hash and step id.</summary>
    /// <param name="flow">The flow record object that holds the steps.</param>
    /// <param name="flowId">The flow identifier the step ids start from.</param>
    /// <returns>The step identifiers in the order the flow lists its steps.</returns>
    static List<string> StepIds(JsonObject flow, string flowId)
    {
        var steps = new List<string>();
        foreach (var step in BookRender.Fields(flow)?["steps"] as JsonArray ?? [])
        {
            if (step is JsonObject item && BookRender.StrProp(item, "id") is { Length: > 0 } name)
            {
                steps.Add(flowId + "#" + name);
            }
        }

        return steps;
    }

    /// <summary>Adds reaching modules to one target: the fulfilling or serving modules of one requirement or flow step.</summary>
    /// <param name="map">The reaching module ids per target id.</param>
    /// <param name="target">The requirement or flow step identifier.</param>
    /// <param name="reached">The module identifiers the reaching record counts toward.</param>
    static void Add(Dictionary<string, HashSet<string>> map, string target, IReadOnlyList<string> reached)
    {
        if (!map.TryGetValue(target, out var sources))
        {
            sources = new HashSet<string>(StringComparer.Ordinal);
            map[target] = sources;
        }

        foreach (var module in reached)
        {
            sources.Add(module);
        }
    }

    /// <summary>The modules one reaching record counts toward: its own module, or for a screen the modules of the use cases it offers.</summary>
    /// <param name="record">The reaching record object.</param>
    /// <param name="byId">The record object for each record identifier.</param>
    /// <param name="moduleIds">The module identifiers the store declares.</param>
    /// <returns>The module identifiers in identifier order, empty when the record sits in no declared module.</returns>
    static List<string> ModulesOf(JsonObject record, Dictionary<string, JsonObject> byId, HashSet<string> moduleIds)
    {
        if (!string.Equals(BookRender.StrProp(record, "type"), "screen", StringComparison.Ordinal))
        {
            return DirectModuleOf(record, moduleIds) is { } own ? [own] : [];
        }

        var offered = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var target in BookDetailValues.StringsOf(BookRender.Fields(record)?["offers"]))
        {
            if (byId.TryGetValue(target, out var useCase)
                && ReachingTypes.Contains(BookRender.StrProp(useCase, "type") ?? "")
                && !string.Equals(BookRender.StrProp(useCase, "type"), "screen", StringComparison.Ordinal)
                && DirectModuleOf(useCase, moduleIds) is { } module)
            {
                offered.Add(module);
            }
        }

        return offered.ToList();
    }

    /// <summary>The module one record sits in: its stated module, its file's module folder, or its identifier home.</summary>
    /// <param name="record">The record object that holds the module, the file and the identifier.</param>
    /// <param name="moduleIds">The module identifiers the store declares.</param>
    /// <returns>The module identifier, or null when neither the module, the file nor the home names a declared module.</returns>
    static string? DirectModuleOf(JsonObject record, HashSet<string> moduleIds)
    {
        var declared = BookRender.StrProp(record, "module");
        if (declared is { Length: > 0 } && moduleIds.Contains("module/" + declared))
        {
            return "module/" + declared;
        }

        if (ModuleFromFile(BookRender.StrProp(record, "file")) is { } filed && moduleIds.Contains(filed))
        {
            return filed;
        }

        var id = BookRender.StrProp(record, "id");
        if (id is { Length: > 0 }
            && Identifiers.TrySplit(id, out _, out var home, out _)
            && home is { Length: > 0 }
            && moduleIds.Contains("module/" + home))
        {
            return "module/" + home;
        }

        return null;
    }

    /// <summary>The module folder one record file sits under.</summary>
    /// <param name="file">The record file path with any separator, or null.</param>
    /// <returns>The module identifier for the first modules folder above the file, or null when no modules folder holds it.</returns>
    static string? ModuleFromFile(string? file)
    {
        if (file is not { Length: > 0 })
        {
            return null;
        }

        var segments = file.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var at = 0; at + 1 < segments.Length; at++)
        {
            if (string.Equals(segments[at], "modules", StringComparison.Ordinal))
            {
                return "module/" + segments[at + 1];
            }
        }

        return null;
    }
}
