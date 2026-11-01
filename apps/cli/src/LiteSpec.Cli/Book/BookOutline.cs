using System.Globalization;
using System.Text.Json.Nodes;

namespace LiteSpec.Book;



















/// <summary>
///     The outline over model.json: every placed record sits once under its parent, with layer and
///     type groups laid transparently on the path. The code implementing a record, the tests proving
///     it and the findings about it sit under it as leaves. A record no rule places, or whose parent
///     is itself unplaced, has no node and is only listed unplaced.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookOutline
{
    /// <summary>The layer keys in outline order.</summary>
    public static readonly IReadOnlyList<string> LayerKeys = ["intent", "behavior", "implementation", "proof", "governance", "glossary"];

    /// <summary>The record types that top the intent layer, in display order.</summary>
    static readonly IReadOnlyList<string> IntentTopOrder = ["goal", "problem", "topic", "constraint", "research", "decision", "actor"];
    /// <summary>The record types under a module, in display order.</summary>
    static readonly IReadOnlyList<string> ModuleChildOrder = ["aggregate", "rule", "use-case", "reaction", "job", "notification", "setting"];
    /// <summary>The record types under an aggregate, in display order.</summary>
    static readonly IReadOnlyList<string> AggregateChildOrder = ["rule", "use-case", "event"];
    /// <summary>The list record types in the behavior layer, in display order.</summary>
    static readonly IReadOnlyList<string> BehaviorListOrder = ["role", "permission", "entry-point", "external-system", "runbook", "integration-event"];
    /// <summary>The record types in the governance layer, in display order.</summary>
    static readonly IReadOnlyList<string> GovernanceOrder = ["exception"];

    /// <summary>Builds the outline over a parsed model.json object.</summary>
    /// <param name="model">The parsed model object that holds the records, code, tests and findings.</param>
    /// <returns>The built outline with the parent of each record, the layer trees and the unplaced identifiers.</returns>
    public static BookOutlineResult Build(JsonObject model)
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
            nodes[pair.Key] = new BookOutlineNode(
                pair.Key,
                "record",
                title is { Length: > 0 } ? title : pair.Key,
                StrProp(pair.Value, "type") ?? "",
                StrProp(pair.Value, "status"),
                []);
        }

        var layers = new Dictionary<string, BookOutlineNode>(StringComparer.Ordinal);
        foreach (var key in LayerKeys)
        {
            layers[key] = new BookOutlineNode(key, "group", LayerLabel(key), null, null, []);
        }

        var groups = new Dictionary<string, BookOutlineNode>(StringComparer.Ordinal);
        BookOutlineNode Group(string layer, string type)
        {
            var id = layer + "/" + type;
            if (!groups.TryGetValue(id, out var group))
            {
                group = new BookOutlineNode(id, "group", PluralOf(type), type, null, []);
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
                layers["intent"].Children.Add(nodes[id]);
                continue;
            }

            var parent = parents[id]!;
            var parentType = StrProp(byId[parent], "type") ?? "";
            var ownLayer = OwnLayer(record, type);
            if (string.Equals(parentType, "product", StringComparison.Ordinal) && !string.Equals(ownLayer, "intent", StringComparison.Ordinal))
            {
                PlaceTopLevel(layers, Group, ownLayer, type, nodes[id]);
                continue;
            }

            nodes[parent].Children.Add(nodes[id]);
        }

        AttachImplementation(model, byId, attached, nodes);
        AttachProof(model, byId, attached, nodes);
        AttachFindings(model, byId, attached, nodes);

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
                    "code",
                    FileName(path),
                    "code",
                    null,
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
                nodes[parent].Children.Add(new BookOutlineNode(id, "test", label, "test", null, []));
            }
        }
    }

    /// <summary>Attaches one finding node per open and known finding under its subject record.</summary>
    /// <param name="model">The parsed model object that holds the open and known findings.</param>
    /// <param name="byId">The record object for each record identifier.</param>
    /// <param name="attached">Whether each record attaches to the outline, keyed by record identifier.</param>
    /// <param name="nodes">The outline node for each record identifier, which receives the finding leaves.</param>
    static void AttachFindings(
        JsonObject model,
        Dictionary<string, JsonObject> byId,
        Dictionary<string, bool> attached,
        Dictionary<string, BookOutlineNode> nodes)
    {
        var entries = new List<JsonObject>();
        entries.AddRange(FindingEntries(model["findings"]));
        entries.AddRange(FindingEntries(model["known"]));

        var at = 0;
        foreach (var finding in entries)
        {
            at++;
            var subject = StrProp(finding, "subject");
            var target = subject is { Length: > 0 } ? BaseId(subject) : null;
            if (target is not { Length: > 0 } || !byId.ContainsKey(target))
            {
                continue;
            }

            if (attached.TryGetValue(target, out var placed) && placed)
            {
                var message = StrProp(finding, "message") ?? "";
                nodes[target].Children.Add(new BookOutlineNode(
                    "finding/" + at,
                    "finding",
                    message.Length > 110 ? message[..108] + "..." : message,
                    "finding",
                    null,
                    []));
            }
        }
    }

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

    /// <summary>Strips the step fragment of an id, such as journey/name#s1.</summary>
    /// <param name="id">The identifier, optionally with a step fragment after a hash.</param>
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
        if (string.Equals(layer, "behavior", StringComparison.Ordinal) && string.Equals(type, "module", StringComparison.Ordinal))
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
            layers["implementation"].Children.Add(node);
        }
        else
        {
            group(layer, type).Children.Add(node);
        }
    }

    /// <summary>Selects the layer a record tops when its parent is the product.</summary>
    /// <param name="record">The record object, whose file path marks a technical decision.</param>
    /// <param name="type">The record type.</param>
    /// <returns>The layer key for the record type, with technical decisions in implementation and unknown types in intent.</returns>
    static string OwnLayer(JsonObject record, string type) =>
        type switch
        {
            "term" => "glossary",
            "exception" => "governance",
            "decision" => IsTechnical(record) ? "implementation" : "intent",
            "product" or "goal" or "problem" or "feature" or "topic" or "requirement" or "constraint" or "research" or "journey" or "actor" => "intent",
            "module" or "aggregate" or "rule" or "use-case" or "result" or "scenario" or "event" or "integration-event" or "reaction" or "job" or "notification" or "setting" or "role" or "permission" or "entry-point" or "screen" or "external-system" or "runbook" => "behavior",
            _ => "intent",
        };

    /// <summary>Determines whether a decision record is a technical decision from its file path.</summary>
    /// <param name="record">The record object that holds the file path.</param>
    /// <returns>True when the file path contains the implementation decisions segment; otherwise false.</returns>
    static bool IsTechnical(JsonObject record)
    {
        var file = StrProp(record, "file");
        return file is not null && file.Contains("implementation/decisions", StringComparison.Ordinal);
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
            _ => key,
        };

    /// <summary>Gives the plural display label of one record type for its group node.</summary>
    /// <param name="type">The record type.</param>
    /// <returns>The plural label of the type, with screen handled as Screens.</returns>
    static string PluralOf(string type) =>
        string.Equals(type, "screen", StringComparison.Ordinal)
            ? "Screens"
            : LiteSpec.Book.BookModel.TypeLabel(type, plural: true);

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

        if (string.Equals(node.Kind, "record", StringComparison.Ordinal))
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
            "product" => IntentTopOrder,
            "module" => ModuleChildOrder,
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
            "record" => 0,
            "code" => 1,
            "test" => 2,
            "finding" => 3,
            _ => 4,
        };

    /// <summary>Compares two intent layer children, placing the product record first.</summary>
    /// <param name="left">The first node to compare.</param>
    /// <param name="right">The second node to compare.</param>
    /// <returns>A negative value when the first node sorts first, a positive value when it sorts last, and zero when they are equal.</returns>
    static int IntentLayerOrder(BookOutlineNode left, BookOutlineNode right)
    {
        var leftProduct = IsRecordOf(left, "product") ? 0 : 1;
        var rightProduct = IsRecordOf(right, "product") ? 0 : 1;
        if (leftProduct != rightProduct)
        {
            return leftProduct.CompareTo(rightProduct);
        }

        return leftProduct == 0
            ? CompareByLabel(left, right)
            : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }

    /// <summary>Compares two behavior layer children, placing module records first and list groups next in their order.</summary>
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
    /// <returns>0 for a module record, 1 for a list group in the behavior list order, and 2 for any other node.</returns>
    static int BehaviorRank(BookOutlineNode node)
    {
        if (IsRecordOf(node, "module"))
        {
            return 0;
        }

        if (string.Equals(node.Kind, "group", StringComparison.Ordinal)
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
        var leftDirect = string.Equals(left.Kind, "record", StringComparison.Ordinal) ? 0 : 1;
        var rightDirect = string.Equals(right.Kind, "record", StringComparison.Ordinal) ? 0 : 1;
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
        var leftDirect = string.Equals(left.Kind, "record", StringComparison.Ordinal) ? 0 : 1;
        var rightDirect = string.Equals(right.Kind, "record", StringComparison.Ordinal) ? 0 : 1;
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
        string.Equals(node.Kind, "record", StringComparison.Ordinal) && string.Equals(node.Type, type, StringComparison.Ordinal);

    /// <summary>Reads one string property of a JSON object.</summary>
    /// <param name="obj">The JSON object to read, or null.</param>
    /// <param name="name">The property name to read.</param>
    /// <returns>The string value of the property, or null when the object or property is missing or the value is not a string.</returns>
    static string? StrProp(JsonObject? obj, string name) => BookRender.StrProp(obj, name);
}
