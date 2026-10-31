using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The record details the book shards carry: the mockup record shape plus every
///     computed field the client renders, built from the model only. Each computed
///     field is built by its own detail type; this type assembles them per record.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookEnrich
{
    /// <summary>Gets the identity fields the mockup record shape strips from the fields it carries.</summary>
    static string[] StrippedKeys { get; } =
    [
        "type", "id", "title", "owner", "status", "decider", "decideBy",
    ];

    /// <summary>Builds one detail object per record id from the model and the outline.</summary>
    /// <param name="model">The parsed model.</param>
    /// <param name="outline">The built outline.</param>
    /// <returns>The detail object per record id.</returns>
    public static IReadOnlyDictionary<string, JsonObject> Build(JsonObject model, BookOutlineResult outline)
    {
        var index = BookIndexBuilder.BuildIndex(model);
        var layers = BookEnrichSources.LayerById(outline);
        var kept = BookEnrichSources.KeptBy(index);
        var served = BookEnrichSources.ServedBy(index);
        var tests = BookEnrichSources.TestsByRecord(model, index);
        var findings = BookEnrichSources.FindingsBySubject(model, index);
        var decisions = BookEnrichSources.DecisionsByTarget(index);
        var effective = BookEnrichSources.EffectiveById(index);
        var routes = BookEnrichSources.RoutesByScreen(model);
        var calls = BookEnrichSources.CallsByScreen(model);
        var handlers = BookEnrichSources.CodeByKey(model, "handlers", "useCase");
        var endpoints = BookEnrichSources.CodeByKey(model, "endpoints", "useCase");
        var methods = BookEnrichSources.CodeByKey(model, "aggregateMethods", "aggregate");
        var runnersByPath = BookEnrichSources.CodeByPaths(model, ["automations", "reactions", "jobs"]);

        var details = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var record in index.Records.OfType<JsonObject>())
        {
            var id = BookRender.StrProp(record, "id");
            if (string.IsNullOrEmpty(id) || details.ContainsKey(id))
            {
                continue;
            }

            var type = BookRender.StrProp(record, "type") ?? "";
            var detail = BaseRecord(record, type);
            detail["parent"] = BookDetailValues.StrNode(BookEnrichSources.ParentOf(outline, id));
            detail["layer"] = BookDetailValues.StrNode(layers.GetValueOrDefault(id));
            BookProofDetail.Add(detail, index, id, type);
            BookStatusDetail.AddMachine(detail, index, id, type);
            BookStatusDetail.AddReachable(detail, index, id, type);
            BookStatusDetail.AddKept(detail, kept, id, type);
            BookJourneyDetail.AddSteps(detail, served, index, record, id, type);
            JsonObject? routeEntry = null;
            if (type == "screen")
            {
                routes.TryGetValue(id, out routeEntry);
                BookJourneyDetail.AddRoute(detail, routeEntry, calls);
            }

            var impl = BookImplementationDetail.ImplFor(index, handlers, endpoints, methods, runnersByPath, routeEntry, id, type);
            detail["impl"] = impl;
            detail["tests"] = BookImplementationDetail.TestList(tests.TryGetValue(id, out var ownTests) ? ownTests : []);
            var expect = BookImplementationDetail.ExpectFor(record, type);
            detail["expect"] = expect;
            detail["lacks"] = BookImplementationDetail.LacksFor(index, tests, impl, expect, id, type);
            detail["heldBy"] = BookHeldByDetail.HeldBy(outline, decisions, effective, record, id);
            detail["findings"] = FindingList(findings.TryGetValue(id, out var own) ? own : []);
            var body = BookBodyDetail.Render(index, record);
            detail["bodyHtml"] = body.Html;
            detail["sections"] = body.Sections;
            order.Add(id);
            details[id] = detail;
        }

        var outs = BookEdges.OutEdges(index, details, calls, routes);
        var ins = BookEdges.InEdges(model, index, outs);
        foreach (var id in order)
        {
            details[id]["out"] = BookEdges.EdgeList(outs.TryGetValue(id, out var from) ? from : []);
            details[id]["in"] = BookEdges.EdgeList(ins.TryGetValue(id, out var to) ? to : []);
        }

        return details;
    }

    /// <summary>The mockup record shape: identity, status, stripped fields and the source.</summary>
    /// <param name="record">The record.</param>
    /// <param name="type">The record type.</param>
    /// <returns>The mockup record shape.</returns>
    static JsonObject BaseRecord(JsonObject record, string type)
    {
        var status = BookRender.StrProp(record, "status");
        if (string.IsNullOrEmpty(status))
        {
            status = "draft";
        }

        var effective = BookRender.StrProp(record, "effectiveStatus");
        if (string.IsNullOrEmpty(effective))
        {
            effective = status;
        }

        var fields = (BookRender.Fields(record)?.DeepClone() as JsonObject) ?? new JsonObject();
        foreach (var key in StrippedKeys)
        {
            fields.Remove(key);
        }

        return new JsonObject
        {
            ["type"] = type,
            ["title"] = BookRender.StrProp(record, "title") ?? "",
            ["owner"] = BookDetailValues.StrNode(BookRender.StrProp(record, "owner")),
            ["status"] = status,
            ["effective"] = effective,
            ["decider"] = BookDetailValues.StrNode(BookRender.StrProp(record, "decider")),
            ["decideBy"] = BookDetailValues.StrNode(BookRender.StrProp(record, "decideBy")),
            ["fields"] = fields,
            ["src"] = new JsonObject
            {
                ["path"] = BookDetailValues.StrNode(BookRender.StrProp(record, "file")),
                ["line"] = BookDetailValues.IntNode(BookDetailValues.IntProp(record, "line")),
            },
        };
    }

    /// <summary>The findings of one record, in model order.</summary>
    /// <param name="findings">The record's findings.</param>
    /// <returns>The finding list.</returns>
    static JsonArray FindingList(List<JsonObject> findings)
    {
        var list = new JsonArray();
        foreach (var finding in findings)
        {
            list.Add(finding);
        }

        return list;
    }
}
