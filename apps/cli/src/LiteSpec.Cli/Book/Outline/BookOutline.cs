using System.Globalization;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Output;
using LiteSpec.Model;

namespace LiteSpec.Cli.Book.Outline;



















/// <summary>
///     The outline over the model: every placed record sits in its layer, under its parent
///     or topping the layer under its record type group when the product parents it. A
///     feature solving problems sits under each one, and only there: the Features group
///     holds the features that solve no problem. The product holds no node. The code
///     implementing a record, the tests proving it and the findings about it sit under it
///     as leaves. A record no rule places, or whose parent is itself unplaced, has no node
///     and is only listed unplaced.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookOutline
{
    /// <summary>The layer keys in outline order.</summary>
    public static readonly IReadOnlyList<string> LayerKeys = ["intent", "behavior", "implementation", "proof", "governance", "glossary", "cast"];

    /// <summary>The record types that top the intent layer under their type groups, in display order.</summary>
    static readonly IReadOnlyList<string> IntentTopOrder = ["direction", "goal", "problem", "requirement", "flow", "constraint", "research", "decision", "actor", "role", "term"];
    /// <summary>The record types under a module, in display order: aggregates with their use cases first, then the module's other records by type.</summary>
    static readonly IReadOnlyList<string> ModuleChildOrder = ["aggregate", "use-case", "rule", "domain-event", "reaction", "job", "read-model", "reference-data", "notification", "setting", "permission"];
    /// <summary>The record types under a knowledge area, in display order: its reference-data tables.</summary>
    static readonly IReadOnlyList<string> KnowledgeAreaChildOrder = ["reference-data"];
    /// <summary>The record types under an aggregate, in display order.</summary>
    static readonly IReadOnlyList<string> AggregateChildOrder = ["rule", "use-case", "domain-event", "module-event", "integration-event"];
    /// <summary>The list record types in the behavior layer, in display order: entry points with their screens, then external systems.</summary>
    static readonly IReadOnlyList<string> BehaviorListOrder = ["entry-point", "external-system"];
    /// <summary>The record types in the governance layer, in display order.</summary>
    static readonly IReadOnlyList<string> GovernanceOrder = ["waiver"];

    /// <summary>Builds the outline over a parsed model object with every layer claimed.</summary>
    /// <param name="model">The parsed model object that holds the records, code, tests and findings.</param>
    /// <returns>The built outline with the parent of each record, the layer trees and the unplaced identifiers.</returns>
    public static BookOutlineResult Build(JsonObject model) => Build(model, LayerClaims.All);

    /// <summary>
    ///     Builds the outline over a parsed model object under the layers the
    ///     project claims. An unclaimed Implementation or Proof layer leaves
    ///     out its code or test leaves, and the level S findings leave out too
    ///     when the project claims neither layer.
    /// </summary>
    /// <param name="model">The parsed model object that holds the records, code, tests and findings.</param>
    /// <param name="claims">The record layers the project claims.</param>
    /// <returns>The built outline with the parent of each record, the layer trees and the unplaced identifiers.</returns>
    public static BookOutlineResult Build(JsonObject model, LayerClaims claims)
    {
        var entries = model["records"] is JsonArray records
            ? records.OfType<JsonObject>().ToList()
            : new List<JsonObject>();
        var byId = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var id = StrProp(entry, "id");
            if (id is { Length: > 0 } && !byId.ContainsKey(id))
            {
                byId[id] = entry;
            }
        }

        var parents = BookParents.ParentsOf(model);
        var nodes = new Dictionary<string, BookOutlineNode>(StringComparer.Ordinal);
        foreach (var pair in byId)
        {
            var title = StrProp(pair.Value, "title");
            var type = StrProp(pair.Value, "type") ?? "";
            nodes[pair.Key] = new BookOutlineNode(
                pair.Key,
                BookOutlineKinds.Record,
                title is { Length: > 0 } ? LiteSpec.Cli.Book.BookModel.DisplayTitle(type, title) : pair.Key,
                type,
                []);
        }

        var layers = new Dictionary<string, BookOutlineNode>(StringComparer.Ordinal);
        foreach (var key in LayerKeys)
        {
            layers[key] = new BookOutlineNode(key, BookOutlineKinds.Group, LayerLabel(key), null, []);
        }

        var groups = new Dictionary<string, BookOutlineNode>(StringComparer.Ordinal);
        BookOutlineNode Group(string layer, string type)
        {
            var id = layer + "/" + type;
            if (!groups.TryGetValue(id, out var group))
            {
                group = new BookOutlineNode(id, BookOutlineKinds.Group, GroupLabel(layer, type), type, []);
                groups[id] = group;
                layers[layer].Children.Add(group);
            }

            return group;
        }

        // A record attaches when its own parent resolves and its parent attaches:
        // anything else has no node and is only listed unplaced.
        var attached = new Dictionary<string, bool>(StringComparer.Ordinal);
        bool Attaches(string id)
        {
            if (attached.TryGetValue(id, out var known))
            {
                return known;
            }

            // Decided false before the walk, so a cycle detaches instead of looping.
            attached[id] = false;
            var record = byId[id];
            if (string.Equals(StrProp(record, "type"), "product", StringComparison.Ordinal))
            {
                return attached[id] = true;
            }

            var parent = parents.TryGetValue(id, out var found) ? found : null;
            var ok = parent is { Length: > 0 }
                && !string.Equals(parent, id, StringComparison.Ordinal)
                && byId.ContainsKey(parent)
                && Attaches(parent);
            return attached[id] = ok;
        }

        foreach (var id in byId.Keys)
        {
            Attaches(id);
        }

        var unplaced = new List<string>();
        foreach (var pair in byId)
        {
            var id = pair.Key;
            var record = pair.Value;
            var type = StrProp(record, "type") ?? "";
            if (!attached[id])
            {
                unplaced.Add(id);
                continue;
            }

            if (string.Equals(type, "product", StringComparison.Ordinal))
            {
                // The product holds no outline node: it tops no layer, and its children top their own.
                continue;
            }

            if (string.Equals(type, "scenario", StringComparison.Ordinal)
                && ShownResult(record, byId, attached) is { } shown)
            {
                // A use-case scenario groups under the result it shows; containment stays with its owner.
                nodes[shown].Children.Add(nodes[id]);
                continue;
            }

            var parent = parents[id]!;
            var parentType = StrProp(byId[parent], "type") ?? "";
            var ownLayer = OwnLayer(record, type);
            if (string.Equals(type, "decision", StringComparison.Ordinal))
            {
                // A decision tops its own layer at product and module level
                // alike, so each decision chapter holds every decision of its
                // kind in one place.
                PlaceTopLevel(layers, Group, ownLayer, type, nodes[id]);
                continue;
            }

            if (type is "runbook" or "waiver" or "external-system")
            {
                // A runbook, waiver or external system filed under a module
                // still tops its own layer, so a Behavior module holds only
                // the module's own records.
                PlaceTopLevel(layers, Group, ownLayer, type, nodes[id]);
                continue;
            }

            if (string.Equals(parentType, "product", StringComparison.Ordinal))
            {
                // Every product child tops its own layer under its type group, except a module, which tops Behavior directly.
                PlaceTopLevel(layers, Group, ownLayer, type, nodes[id]);
                continue;
            }

            nodes[parent].Children.Add(nodes[id]);
        }

        if (claims.Implementation is not null)
        {
            AttachImplementation(model, byId, attached, nodes);
        }

        if (claims.Proof is not null)
        {
            AttachProof(model, byId, attached, nodes);
        }

        AttachFindings(model, byId, attached, nodes, claims);

        SortTrees(layers);
        unplaced.Sort(StringComparer.Ordinal);

        var parentOf = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var id in byId.Keys)
        {
            parentOf[id] = parents.TryGetValue(id, out var parent) ? parent : null;
        }

        var ordered = new List<BookOutlineNode>();
        foreach (var key in LayerKeys)
        {
            ordered.Add(layers[key]);
        }

        return new BookOutlineResult(parentOf, ordered, unplaced);
    }

    /// <summary>
    ///     Attaches one code node per implementing file under the record its marks name: the first
    ///     target in identifier order when a file implements several. The rest stay visible in the
    ///     rail trace the details carry.
    /// </summary>
    /// <param name="model">The parsed model object that holds the code entries and implements links.</param>
    /// <param name="byId">The record object for each record identifier.</param>
    /// <param name="attached">Whether each record attaches to the outline, keyed by record identifier.</param>
    /// <param name="nodes">The outline node for each record identifier, which receives the code leaves.</param>
    static void AttachImplementation(
        JsonObject model,
        Dictionary<string, JsonObject> byId,
        Dictionary<string, bool> attached,
        Dictionary<string, BookOutlineNode> nodes)
    {
        var targets = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        void Claim(string? path, string? target)
        {
            if (path is not { Length: > 0 } || target is not { Length: > 0 } || !byId.ContainsKey(target))
            {
                return;
            }

            if (!targets.TryGetValue(path, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                targets[path] = set;
            }

            set.Add(target);
        }

        var code = model["code"] as JsonObject;
        foreach (var entry in code?["handlers"] as JsonArray ?? [])
        {
            if (entry is JsonObject handler)
            {
                Claim(StrProp(handler, "path"), StrProp(handler, "useCase"));
            }
        }

        foreach (var entry in code?["endpoints"] as JsonArray ?? [])
        {
            if (entry is JsonObject endpoint)
            {
                Claim(StrProp(endpoint, "path"), StrProp(endpoint, "useCase"));
            }
        }

        foreach (var entry in code?["aggregateMethods"] as JsonArray ?? [])
        {
            if (entry is JsonObject method)
            {
                Claim(StrProp(method, "path"), StrProp(method, "aggregate"));
            }
        }

        // Reaction and job entries name no record: their use case is the served use case,
        // not the reaction or job, so their files attach through their implements links below.
        var links = model["links"] as JsonObject;
        foreach (var entry in links?["implements"] as JsonArray ?? [])
        {
            if (entry is JsonObject link)
            {
                Claim(StrProp(link, "path"), StrProp(link, "to"));
            }
        }

        foreach (var path in targets.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            var parent = targets[path].OrderBy(id => id, StringComparer.Ordinal).First();
            if (attached.TryGetValue(parent, out var placed) && placed)
            {
                nodes[parent].Children.Add(new BookOutlineNode(
                    "code/" + path,
                    BookOutlineKinds.Code,
                    FileName(path),
                    BookOutlineKinds.Code,
                    []));
            }
        }
    }

    /// <summary>Attaches one test node per unit and browser test under the lowest record it proves: the first claim in identifier order.</summary>
    /// <param name="model">The parsed model object that holds the unit and browser tests.</param>
    /// <param name="byId">The record object for each record identifier.</param>
    /// <param name="attached">Whether each record attaches to the outline, keyed by record identifier.</param>
    /// <param name="nodes">The outline node for each record identifier, which receives the test leaves.</param>
    static void AttachProof(
        JsonObject model,
        Dictionary<string, JsonObject> byId,
        Dictionary<string, bool> attached,
        Dictionary<string, BookOutlineNode> nodes)
    {
        var tests = new List<(string Id, string Label, List<string> Claims)>();
        foreach (var entry in model["tests"] as JsonArray ?? [])
        {
            if (entry is not JsonObject test)
            {
                continue;
            }

            var scenario = StrProp(test, "scenario");
            if (scenario is not { Length: > 0 })
            {
                continue;
            }

            tests.Add(("test/" + scenario, TestName(scenario), ClaimsOf(test["claims"])));
        }

        foreach (var entry in model["browserTests"] as JsonArray ?? [])
        {
            if (entry is not JsonObject browser)
            {
                continue;
            }

            var file = StrProp(browser, "file");
            if (file is not { Length: > 0 })
            {
                continue;
            }

            var id = "test/" + file + ":" + (IntProp(browser, "line")?.ToString(CultureInfo.InvariantCulture) ?? "?");
            var title = StrProp(browser, "title");
            tests.Add((id, title is { Length: > 0 } ? title : id, ClaimsOf(browser["proves"])));
        }

        foreach (var (id, label, claims) in tests.OrderBy(test => test.Id, StringComparer.Ordinal))
        {
            var parent = claims
                .Select(BaseId)
                .Where(target => byId.ContainsKey(target))
                .OrderBy(target => target, StringComparer.Ordinal)
                .FirstOrDefault();
            if (parent is { Length: > 0 } && attached.TryGetValue(parent, out var placed) && placed)
            {
                nodes[parent].Children.Add(new BookOutlineNode(id, BookOutlineKinds.Test, label, BookOutlineKinds.Test, []));
            }
        }
    }

    /// <summary>Attaches one finding node per open and known finding under its subject record, leaving out level S findings when the project claims neither Implementation nor Proof. Each node id names its subject and its 1-based position among that subject's findings, in the same open-then-covered order the record shards carry, so the id opens the subject record at that finding.</summary>
    /// <param name="model">The parsed model object that holds the open and covered findings.</param>
    /// <param name="byId">The record object for each record identifier.</param>
    /// <param name="attached">Whether each record attaches to the outline, keyed by record identifier.</param>
    /// <param name="nodes">The outline node for each record identifier, which receives the finding leaves.</param>
    /// <param name="claims">The record layers the project claims.</param>
    static void AttachFindings(
        JsonObject model,
        Dictionary<string, JsonObject> byId,
        Dictionary<string, bool> attached,
        Dictionary<string, BookOutlineNode> nodes,
        LayerClaims claims)
    {
        var entries = new List<JsonObject>();
        entries.AddRange(FindingEntries(model["findings"]));
        entries.AddRange(FindingEntries(model["covered"]));

        var perSubject = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var finding in entries)
        {
            if (claims.Implementation is null && claims.Proof is null && IsLevelS(finding))
            {
                continue;
            }
            var subject = StrProp(finding, "subject");
            var target = subject is { Length: > 0 } ? BaseId(subject) : null;
            if (target is not { Length: > 0 } || !byId.ContainsKey(target))
            {
                continue;
            }

            if (attached.TryGetValue(target, out var placed) && placed)
            {
                perSubject.TryGetValue(target, out var seen);
                var position = seen + 1;
                perSubject[target] = position;
                var message = StrProp(finding, "message") ?? "";
                nodes[target].Children.Add(new BookOutlineNode(
                    target + "#finding-" + position.ToString(CultureInfo.InvariantCulture),
                    BookOutlineKinds.Finding,
                    message.Length > 110 ? message[..108] + "..." : message,
                    BookOutlineKinds.Finding,
                    []));
            }
        }
    }

    /// <summary>Whether one finding is a level S finding, by its tier or level field.</summary>
    /// <param name="finding">The finding object.</param>
    /// <returns>True when the finding tier or level is S.</returns>
    static bool IsLevelS(JsonObject finding) =>
        string.Equals(StrProp(finding, "tier") ?? StrProp(finding, "level"), BookCheckLevels.S, StringComparison.Ordinal)
        || string.Equals(StrProp(finding, "level") ?? StrProp(finding, "tier"), BookCheckLevels.S, StringComparison.Ordinal);

    /// <summary>The finding objects of a findings node, as an array or a wrapped file.</summary>
    /// <param name="node">The findings node, either an array of findings or an object wrapping one, or null.</param>
    /// <returns>The finding objects the node holds, empty when it holds none.</returns>
    static IEnumerable<JsonObject> FindingEntries(JsonNode? node)
    {
        if (node is JsonArray items)
        {
            foreach (var item in items.OfType<JsonObject>())
            {
                yield return item;
            }
        }
        else if (node is JsonObject wrapper && wrapper["findings"] is JsonArray inner)
        {
            foreach (var item in inner.OfType<JsonObject>())
            {
                yield return item;
            }
        }
    }

    /// <summary>The strings of a claims node, written as one value or a list.</summary>
    /// <param name="node">The claims node, either one string or an array of strings, or null.</param>
    /// <returns>The non-empty claim strings the node holds, in order.</returns>
    static List<string> ClaimsOf(JsonNode? node)
    {
        var claims = new List<string>();
        if (node is JsonValue scalar && scalar.TryGetValue<string>(out var one) && one is { Length: > 0 })
        {
            claims.Add(one);
        }
        else if (node is JsonArray items)
        {
            foreach (var item in items)
            {
                if (item is JsonValue element && element.TryGetValue<string>(out var text) && text is { Length: > 0 })
                {
                    claims.Add(text);
                }
            }
        }

        return claims;
    }

    /// <summary>The file name of a code path: the last segment after the slash.</summary>
    /// <param name="path">The code path with slash separators.</param>
    /// <returns>The last segment of the path after the final slash, or the whole path when it holds no slash.</returns>
    static string FileName(string path)
    {
        var slash = path.LastIndexOf('/');
        return slash < 0 ? path : path[(slash + 1)..];
    }

    /// <summary>One test name in words: the last segment with underscores as spaces.</summary>
    /// <param name="scenario">The scenario or test name with dot segments and underscores.</param>
    /// <returns>The last dot segment with underscores replaced by spaces.</returns>
    static string TestName(string scenario)
    {
        var tail = scenario;
        var dot = scenario.LastIndexOf('.');
        if (dot >= 0 && dot + 1 < scenario.Length)
        {
            tail = scenario[(dot + 1)..];
        }

        return tail.Replace('_', ' ');
    }

    /// <summary>Strips the hash fragment of an id, such as an external-system operation.</summary>
    /// <param name="id">The identifier, optionally with a fragment after a hash.</param>
    /// <returns>The identifier without its fragment, unchanged when it holds no hash.</returns>
    static string BaseId(string id)
    {
        var hash = id.IndexOf('#');
        return hash < 0 ? id : id[..hash];
    }

    /// <summary>Reads an integer property, or null when it is missing or not a number.</summary>
    /// <param name="obj">The JSON object to read, or null.</param>
    /// <param name="name">The property name to read.</param>
    /// <returns>The integer value of the property, or null when the object or property is missing, not a number, or outside the integer range.</returns>
    static int? IntProp(JsonObject? obj, string name)
    {
        if (obj?[name] is JsonValue scalar && scalar.TryGetValue<int>(out var value))
        {
            return value;
        }

        if (obj?[name] is JsonValue wide && wide.TryGetValue<long>(out var big) && big >= int.MinValue && big <= int.MaxValue)
        {
            return (int)big;
        }

        return null;
    }

    /// <summary>Places one top-level node in its layer, directly or under its type group.</summary>
    /// <param name="layers">The layer root nodes keyed by layer key.</param>
    /// <param name="group">The factory that returns the type group node for a layer and a type, creating it on first use.</param>
    /// <param name="layer">The layer key the node tops.</param>
    /// <param name="type">The record type of the node.</param>
    /// <param name="node">The outline node to place.</param>
    static void PlaceTopLevel(
        Dictionary<string, BookOutlineNode> layers,
        Func<string, string, BookOutlineNode> group,
        string layer,
        string type,
        BookOutlineNode node)
    {
        // A record whose parent is the product in another layer tops its own layer.
        if (string.Equals(layer, "behavior", StringComparison.Ordinal) && type is "module" or "knowledge-area")
        {
            layers["behavior"].Children.Add(node);
        }
        else if (string.Equals(layer, "behavior", StringComparison.Ordinal))
        {
            group("behavior", type).Children.Add(node);
        }
        else if (string.Equals(layer, "governance", StringComparison.Ordinal))
        {
            group("governance", type).Children.Add(node);
        }
        else if (string.Equals(layer, "glossary", StringComparison.Ordinal))
        {
            layers["glossary"].Children.Add(node);
        }
        else if (string.Equals(layer, "implementation", StringComparison.Ordinal))
        {
            group("implementation", type).Children.Add(node);
        }
        else
        {
            group(layer, type).Children.Add(node);
        }
    }

    /// <summary>Selects the layer a record tops when placement lifts it out of its parent.</summary>
    /// <param name="record">The record object, whose file path marks a technical decision and whose layer field marks a behavior term.</param>
    /// <param name="type">The record type.</param>
    /// <returns>The layer key for the record type: root for the product, with technical decisions and runbooks in implementation, waivers in governance, behavior terms in behavior, and unknown types in intent.</returns>
    static string OwnLayer(JsonObject record, string type) =>
        type switch
        {
            "product" => "root",
            "waiver" => "governance",
            "cast" or "party" or "person" or "occasion" or "figure" => "cast",
            "decision" => IsTechnical(record) ? "implementation" : "intent",
            "runbook" => "implementation",
            "term" => IsBehaviorTerm(record) ? "behavior" : "intent",
            "goal" or "direction" or "problem" or "feature" or "requirement" or "constraint" or "research" or "flow" or "step" or "actor" or "role" => "intent",
            "module" or "knowledge-area" or "aggregate" or "rule" or "use-case" or "result" or "scenario" or "domain-event" or "module-event" or "integration-event" or "reaction" or "job" or "notification" or "setting" or "read-model" or "reference-data" or "permission" or "entry-point" or "screen" or "external-system" => "behavior",
            _ => "intent",
        };

    /// <summary>Determines whether a term record belongs to the behavior layer from its written layer field.</summary>
    /// <param name="record">The record object that holds the layer field.</param>
    /// <returns>True when the record states layer behavior; otherwise false.</returns>
    static bool IsBehaviorTerm(JsonObject record) =>
        string.Equals(BookRender.StrProp(BookRender.Fields(record), "layer"), "behavior", StringComparison.Ordinal);

    /// <summary>Determines whether a decision record is a technical decision from its file path.</summary>
    /// <param name="record">The record object that holds the file path.</param>
    /// <returns>True when the file sits under an implementation decisions folder, at product level or under a module; otherwise false.</returns>
    static bool IsTechnical(JsonObject record)
    {
        var file = StrProp(record, "file");
        if (file is not { Length: > 0 })
        {
            return false;
        }

        var segments = file.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var at = 0; at < segments.Length; at++)
        {
            if (!string.Equals(segments[at], "implementation", StringComparison.Ordinal))
            {
                continue;
            }

            if (at + 1 < segments.Length && string.Equals(segments[at + 1], "decisions", StringComparison.Ordinal))
            {
                return true;
            }

            if (at + 3 < segments.Length
                && string.Equals(segments[at + 1], "modules", StringComparison.Ordinal)
                && string.Equals(segments[at + 3], "decisions", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Maps one layer key to its display label.</summary>
    /// <param name="key">The layer key.</param>
    /// <returns>The display label for the layer, or the key itself when it names no known layer.</returns>
    static string LayerLabel(string key) =>
        key switch
        {
            "intent" => "Intent",
            "behavior" => "Behavior",
            "implementation" => "Implementation",
            "proof" => "Proof",
            "governance" => "Governance",
            "glossary" => "Glossary",
            "cast" => "Cast",
            _ => key,
        };

    /// <summary>Gives the plural display label of one record type for its group node.</summary>
    /// <param name="type">The record type.</param>
    /// <returns>The plural label of the type, with screen handled as Screens.</returns>
    static string PluralOf(string type) =>
        string.Equals(type, "screen", StringComparison.Ordinal)
            ? "Screens"
            : LiteSpec.Cli.Book.BookModel.TypeLabel(type, plural: true);

    /// <summary>Gives the display label of one type group: the plural of its record type, with the intent requirements under their own name.</summary>
    /// <param name="layer">The layer key the group tops.</param>
    /// <param name="type">The record type the group holds.</param>
    /// <returns>The group label: Product requirements for the intent requirements group, else the plural label of the type.</returns>
    static string GroupLabel(string layer, string type) =>
        string.Equals(layer, "intent", StringComparison.Ordinal) && string.Equals(type, "requirement", StringComparison.Ordinal)
            ? "Product requirements"
            : PluralOf(type);

    /// <summary>Sorts every layer tree and then each layer root by its layer order.</summary>
    /// <param name="layers">The layer root nodes keyed by layer key.</param>
    static void SortTrees(Dictionary<string, BookOutlineNode> layers)
    {
        foreach (var layer in layers.Values)
        {
            SortNode(layer);
        }

        layers["intent"].Children.Sort(IntentLayerOrder);
        layers["behavior"].Children.Sort(BehaviorLayerOrder);
        layers["implementation"].Children.Sort(DirectFirstOrder);
        layers["governance"].Children.Sort(GovernanceLayerOrder);
        layers["glossary"].Children.Sort(DirectFirstOrder);
    }

    /// <summary>Sorts one node subtree, record children by record order and group children by label.</summary>
    /// <param name="node">The node whose subtree is sorted in place.</param>
    static void SortNode(BookOutlineNode node)
    {
        foreach (var child in node.Children)
        {
            SortNode(child);
        }

        if (string.Equals(node.Kind, BookOutlineKinds.Record, StringComparison.Ordinal))
        {
            SortRecordChildren(node);
        }
        else if (node.Type is { Length: > 0 })
        {
            node.Children.Sort(CompareByLabel);
        }
    }

    /// <summary>Sorts the children of one record node by leaf rank and then by its type order or label.</summary>
    /// <param name="node">The record node whose children are sorted in place.</param>
    static void SortRecordChildren(BookOutlineNode node)
    {
        var order = node.Type switch
        {
            "module" => ModuleChildOrder,
            "knowledge-area" => KnowledgeAreaChildOrder,
            "aggregate" => AggregateChildOrder,
            _ => null,
        };

        // Records keep their type order; the code, tests and findings under them follow as leaves.
        node.Children.Sort((left, right) =>
        {
            var rank = LeafRank(left).CompareTo(LeafRank(right));
            if (rank != 0)
            {
                return rank;
            }

            if (LeafRank(left) > 0)
            {
                var leaf = string.Compare(left.Label, right.Label, StringComparison.Ordinal);
                return leaf != 0 ? leaf : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
            }

            if (order is null)
            {
                return CompareByLabel(left, right);
            }

            var typed = OrderIndex(order, left.Type).CompareTo(OrderIndex(order, right.Type));
            return typed != 0 ? typed : CompareByLabel(left, right);
        });
    }

    /// <summary>The leaf rank of an outline node: records first, then code, tests and findings.</summary>
    /// <param name="node">The outline node whose kind is ranked.</param>
    /// <returns>The rank of the node kind: 0 for a record, 1 for code, 2 for a test, 3 for a finding and 4 for any other kind.</returns>
    static int LeafRank(BookOutlineNode node) =>
        node.Kind switch
        {
            BookOutlineKinds.Record => 0,
            BookOutlineKinds.Code => 1,
            BookOutlineKinds.Test => 2,
            BookOutlineKinds.Finding => 3,
            _ => 4,
        };

    /// <summary>Compares two intent layer children by record type order, then by label.</summary>
    /// <param name="left">The first node to compare.</param>
    /// <param name="right">The second node to compare.</param>
    /// <returns>A negative value when the first node sorts first, a positive value when it sorts last, and zero when they are equal.</returns>
    static int IntentLayerOrder(BookOutlineNode left, BookOutlineNode right)
    {
        var typed = OrderIndex(IntentTopOrder, left.Type).CompareTo(OrderIndex(IntentTopOrder, right.Type));
        return typed != 0 ? typed : CompareByLabel(left, right);
    }

    /// <summary>Compares two behavior layer children, placing module and knowledge area records first and list groups next in their order.</summary>
    /// <param name="left">The first node to compare.</param>
    /// <param name="right">The second node to compare.</param>
    /// <returns>A negative value when the first node sorts first, a positive value when it sorts last, and zero when they are equal.</returns>
    static int BehaviorLayerOrder(BookOutlineNode left, BookOutlineNode right)
    {
        var leftRank = BehaviorRank(left);
        var rightRank = BehaviorRank(right);
        if (leftRank != rightRank)
        {
            return leftRank.CompareTo(rightRank);
        }

        if (leftRank == 0)
        {
            return CompareByLabel(left, right);
        }

        if (leftRank == 1)
        {
            var order = OrderIndex(BehaviorListOrder, left.Type).CompareTo(OrderIndex(BehaviorListOrder, right.Type));
            return order != 0 ? order : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
        }

        return string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }

    /// <summary>Ranks one behavior layer child for the layer order.</summary>
    /// <param name="node">The outline node to rank.</param>
    /// <returns>0 for a module or knowledge area record, 1 for a list group in the behavior list order, and 2 for any other node.</returns>
    static int BehaviorRank(BookOutlineNode node)
    {
        if (IsRecordOf(node, "module") || IsRecordOf(node, "knowledge-area"))
        {
            return 0;
        }

        if (string.Equals(node.Kind, BookOutlineKinds.Group, StringComparison.Ordinal)
            && node.Type is { Length: > 0 }
            && BehaviorListOrder.Contains(node.Type, StringComparer.Ordinal))
        {
            return 1;
        }

        return 2;
    }

    /// <summary>Compares two governance layer children, placing direct records before type groups in their order.</summary>
    /// <param name="left">The first node to compare.</param>
    /// <param name="right">The second node to compare.</param>
    /// <returns>A negative value when the first node sorts first, a positive value when it sorts last, and zero when they are equal.</returns>
    static int GovernanceLayerOrder(BookOutlineNode left, BookOutlineNode right)
    {
        var leftDirect = string.Equals(left.Kind, BookOutlineKinds.Record, StringComparison.Ordinal) ? 0 : 1;
        var rightDirect = string.Equals(right.Kind, BookOutlineKinds.Record, StringComparison.Ordinal) ? 0 : 1;
        if (leftDirect != rightDirect)
        {
            return leftDirect.CompareTo(rightDirect);
        }

        if (leftDirect == 0)
        {
            return CompareByLabel(left, right);
        }

        var order = OrderIndex(GovernanceOrder, left.Type).CompareTo(OrderIndex(GovernanceOrder, right.Type));
        return order != 0 ? order : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }

    /// <summary>Compares two layer children, placing direct records before groups.</summary>
    /// <param name="left">The first node to compare.</param>
    /// <param name="right">The second node to compare.</param>
    /// <returns>A negative value when the first node sorts first, a positive value when it sorts last, and zero when they are equal.</returns>
    static int DirectFirstOrder(BookOutlineNode left, BookOutlineNode right)
    {
        var leftDirect = string.Equals(left.Kind, BookOutlineKinds.Record, StringComparison.Ordinal) ? 0 : 1;
        var rightDirect = string.Equals(right.Kind, BookOutlineKinds.Record, StringComparison.Ordinal) ? 0 : 1;
        if (leftDirect != rightDirect)
        {
            return leftDirect.CompareTo(rightDirect);
        }

        return leftDirect == 0
            ? CompareByLabel(left, right)
            : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }

    /// <summary>Finds the position of one record type in a display order.</summary>
    /// <param name="order">The record types in display order.</param>
    /// <param name="type">The record type to locate, or null.</param>
    /// <returns>The index of the type in the order, or the order length when the type is absent or null.</returns>
    static int OrderIndex(IReadOnlyList<string> order, string? type)
    {
        for (var index = 0; index < order.Count; index++)
        {
            if (string.Equals(order[index], type, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return order.Count;
    }

    /// <summary>Compares two nodes by label and then by identifier.</summary>
    /// <param name="left">The first node to compare.</param>
    /// <param name="right">The second node to compare.</param>
    /// <returns>A negative value when the first node sorts first, a positive value when it sorts last, and zero when they are equal.</returns>
    static int CompareByLabel(BookOutlineNode left, BookOutlineNode right)
    {
        var label = string.Compare(left.Label, right.Label, StringComparison.Ordinal);
        return label != 0 ? label : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }

    /// <summary>Determines whether a node is a record node of one record type.</summary>
    /// <param name="node">The outline node to test.</param>
    /// <param name="type">The record type to match.</param>
    /// <returns>True when the node is a record of the given type; otherwise false.</returns>
    static bool IsRecordOf(BookOutlineNode node, string type) =>
        string.Equals(node.Kind, BookOutlineKinds.Record, StringComparison.Ordinal) && string.Equals(node.Type, type, StringComparison.Ordinal);

    /// <summary>Reads one string property of a JSON object.</summary>
    /// <param name="obj">The JSON object to read, or null.</param>
    /// <param name="name">The property name to read.</param>
    /// <returns>The string value of the property, or null when the object or property is missing or the value is not a string.</returns>
    static string? StrProp(JsonObject? obj, string name) => BookRender.StrProp(obj, name);

    /// <summary>The result a scenario shows, when the outline places that result; the grouping under it is presentation only.</summary>
    /// <param name="record">The scenario record.</param>
    /// <param name="byId">The records by identifier.</param>
    /// <param name="attached">Whether each record attaches to the tree.</param>
    /// <returns>The attached result identifier, or null when the scenario shows none.</returns>
    static string? ShownResult(JsonObject record, Dictionary<string, JsonObject> byId, Dictionary<string, bool> attached)
    {
        var target = BookParents.FirstLink(record, "result");
        if (target is not { Length: > 0 }
            || !byId.TryGetValue(target, out var holder)
            || !string.Equals(StrProp(holder, "type"), "result", StringComparison.Ordinal)
            || !attached.TryGetValue(target, out var ok)
            || !ok)
        {
            return null;
        }

        return target;
    }
}
