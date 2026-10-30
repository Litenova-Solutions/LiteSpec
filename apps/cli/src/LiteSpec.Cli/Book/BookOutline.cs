using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>One outline node: a record or a group with its children.</summary>
/// <param name="Id">The record id, the layer key, or the layer and type.</param>
/// <param name="Kind">Either record or group.</param>
/// <param name="Label">The title, the type plural, or the layer label.</param>
/// <param name="Type">The record type, or null for a layer group.</param>
/// <param name="Status">The record status, or null for a group.</param>
/// <param name="Children">The nested nodes.</param>
sealed record BookOutlineNode(string Id, string Kind, string Label, string? Type, string? Status, List<BookOutlineNode> Children);

/// <summary>One built outline: the parent per record, the layer trees, and the unplaced ids.</summary>
/// <param name="ParentOf">The parent id per record id, or null when no rule places it.</param>
/// <param name="Layers">One root group node per layer key, in layer order.</param>
/// <param name="Unplaced">The ids with no parent, excluding the product, in identifier order.</param>
sealed record BookOutlineResult(
    IReadOnlyDictionary<string, string?> ParentOf,
    IReadOnlyList<BookOutlineNode> Layers,
    IReadOnlyList<string> Unplaced);

/// <summary>
///     The outline over model.json: every record placed once under its parent, with layer and
///     type groups laid transparently on the path. A record no rule places sits under its
///     layer type group and is counted unplaced.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookOutline
{
    /// <summary>The layer keys in outline order.</summary>
    public static readonly IReadOnlyList<string> LayerKeys = ["intent", "behavior", "implementation", "proof", "governance", "glossary"];

    static readonly IReadOnlyList<string> IntentTopOrder = ["goal", "problem", "constraint", "research", "decision", "actor"];
    static readonly IReadOnlyList<string> ModuleChildOrder = ["aggregate", "rule", "use-case", "reaction", "job", "notification", "setting"];
    static readonly IReadOnlyList<string> AggregateChildOrder = ["rule", "use-case", "event"];
    static readonly IReadOnlyList<string> BehaviorListOrder = ["role", "permission", "entry-point", "external-system", "runbook", "integration-event"];
    static readonly IReadOnlyList<string> GovernanceOrder = ["deviation", "known-gap"];

    /// <summary>Builds the outline over a parsed model.json object.</summary>
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

        var unplaced = new List<string>();
        foreach (var pair in byId)
        {
            var id = pair.Key;
            var record = pair.Value;
            var type = StrProp(record, "type") ?? "";
            if (string.Equals(type, "product", StringComparison.Ordinal))
            {
                layers["intent"].Children.Add(nodes[id]);
                continue;
            }

            var parent = parents.TryGetValue(id, out var found) ? found : null;
            if (parent is not { Length: > 0 } || !byId.ContainsKey(parent) || string.Equals(parent, id, StringComparison.Ordinal))
            {
                unplaced.Add(id);
                PlaceUnplaced(layers, Group, record, type, nodes[id]);
                continue;
            }

            var parentType = StrProp(byId[parent], "type") ?? "";
            var ownLayer = OwnLayer(record, type);
            if (string.Equals(parentType, "product", StringComparison.Ordinal) && !string.Equals(ownLayer, "intent", StringComparison.Ordinal))
            {
                PlaceTopLevel(layers, Group, ownLayer, type, nodes[id]);
                continue;
            }

            nodes[parent].Children.Add(nodes[id]);
        }

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

    static void PlaceUnplaced(
        Dictionary<string, BookOutlineNode> layers,
        Func<string, string, BookOutlineNode> group,
        JsonObject record,
        string type,
        BookOutlineNode node)
    {
        var layer = OwnLayer(record, type);
        if (string.Equals(layer, "glossary", StringComparison.Ordinal))
        {
            // One glossary, never split: terms sit directly under it.
            layers["glossary"].Children.Add(node);
        }
        else
        {
            group(layer, type).Children.Add(node);
        }
    }

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

    static string OwnLayer(JsonObject record, string type) =>
        type switch
        {
            "term" => "glossary",
            "deviation" or "known-gap" => "governance",
            "decision" => IsTechnical(record) ? "implementation" : "intent",
            "product" or "goal" or "problem" or "feature" or "requirement" or "constraint" or "research" or "journey" or "actor" => "intent",
            "module" or "aggregate" or "rule" or "use-case" or "result" or "scenario" or "event" or "integration-event" or "reaction" or "job" or "notification" or "setting" or "role" or "permission" or "entry-point" or "screen" or "external-system" or "runbook" => "behavior",
            _ => "intent",
        };

    static bool IsTechnical(JsonObject record)
    {
        var file = StrProp(record, "file");
        return file is not null && file.Contains("implementation/decisions", StringComparison.Ordinal);
    }

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

    static string PluralOf(string type) =>
        string.Equals(type, "screen", StringComparison.Ordinal)
            ? "Screens"
            : LiteSpec.Book.BookModel.TypeLabel(type, plural: true);

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

    static void SortRecordChildren(BookOutlineNode node)
    {
        var order = node.Type switch
        {
            "product" => IntentTopOrder,
            "module" => ModuleChildOrder,
            "aggregate" => AggregateChildOrder,
            _ => null,
        };

        if (order is null)
        {
            node.Children.Sort(CompareByLabel);
        }
        else
        {
            node.Children.Sort((left, right) =>
            {
                var rank = OrderIndex(order, left.Type).CompareTo(OrderIndex(order, right.Type));
                return rank != 0 ? rank : CompareByLabel(left, right);
            });
        }
    }

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

    static int CompareByLabel(BookOutlineNode left, BookOutlineNode right)
    {
        var label = string.Compare(left.Label, right.Label, StringComparison.Ordinal);
        return label != 0 ? label : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }

    static bool IsRecordOf(BookOutlineNode node, string type) =>
        string.Equals(node.Kind, "record", StringComparison.Ordinal) && string.Equals(node.Type, type, StringComparison.Ordinal);

    static string? StrProp(JsonObject? obj, string name) =>
        obj?[name] is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;
}
