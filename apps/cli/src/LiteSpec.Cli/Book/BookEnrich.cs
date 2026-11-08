using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Book;

/// <summary>
///     The record details the book shards carry: the mockup record shape plus every
///     computed field the client renders, built from the model only. Each computed
///     field is built by its own detail type; this type assembles them per record.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgements-in-the-book</follows>
static class BookEnrich
{
    /// <summary>Gets the identity fields the mockup record shape strips from the fields it carries.</summary>
    static string[] StrippedKeys { get; } =
    [
        "type", "id", "title", "owner", "status", "decider", "decideBy",
    ];

    /// <summary>Builds one detail object per record id from the model and the outline at the full conformance level.</summary>
    /// <param name="model">The parsed model.</param>
    /// <param name="outline">The built outline.</param>
    /// <returns>The detail object per record id.</returns>
    public static IReadOnlyDictionary<string, JsonObject> Build(JsonObject model, BookOutlineResult outline) =>
        Build(model, outline, ConformanceLevel.Full);

    /// <summary>
    ///     Builds one detail object per record id from the model and the outline
    ///     at one conformance level. At <see cref="ConformanceLevel.Records" />
    ///     a detail carries no code, no proof status and no level S finding,
    ///     and its completion holds no code or test judgement. Every detail
    ///     at every level that is a requirement or a journey carries its
    ///     Intent coverage.
    /// </summary>
    /// <param name="model">The parsed model.</param>
    /// <param name="outline">The built outline.</param>
    /// <param name="level">The conformance level the project claims.</param>
    /// <returns>The detail object per record id.</returns>
    public static IReadOnlyDictionary<string, JsonObject> Build(JsonObject model, BookOutlineResult outline, ConformanceLevel level)
    {
        var traced = level != ConformanceLevel.Records;
        var coverage = BookIntentCoverage.Of(model);
        var index = BookIndexBuilder.BuildIndex(model);
        var layers = BookEnrichSources.LayerById(outline);
        var kept = BookEnrichSources.KeptBy(index);
        var served = BookEnrichSources.ServedBy(index);
        var tests = traced ? BookEnrichSources.TestsByRecord(model, index) : new Dictionary<string, List<JsonObject>>(StringComparer.Ordinal);
        var findings = BookEnrichSources.FindingsBySubject(model, index, level);
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
            if (traced)
            {
                BookProofDetail.Add(detail, index, id, type);
                BookProofDetail.AddMachine(detail, index, id, type);
            }

            BookProofDetail.AddReachable(detail, index, id, type);
            BookProofDetail.AddKept(detail, kept, id, type);
            BookJourneyDetail.AddSteps(detail, served, index, record, id, type);
            if (!traced && type == "journey")
            {
                detail["exercised"] = new JsonArray();
            }

            AddIntentCoverage(detail, coverage, id, type);
            JsonObject? routeEntry = null;
            if (type == "screen")
            {
                routes.TryGetValue(id, out routeEntry);
                BookJourneyDetail.AddRoute(detail, routeEntry, calls);
            }

            var impl = traced
                ? BookImplementationDetail.ImplFor(index, handlers, endpoints, methods, runnersByPath, routeEntry, id, type)
                : new JsonArray();
            detail["impl"] = impl;
            detail["tests"] = traced
                ? BookImplementationDetail.TestList(tests.TryGetValue(id, out var ownTests) ? ownTests : [])
                : new JsonArray();
            var expect = traced
                ? BookImplementationDetail.ExpectFor(record, type)
                : new JsonObject { ["impl"] = false, ["proof"] = false };
            detail["expect"] = expect;
            detail["lacks"] = traced
                ? BookImplementationDetail.LacksFor(index, tests, impl, expect, id, type)
                : new JsonArray();
            detail["completion"] = traced
                ? BookImplementationDetail.CompletionFor(index, tests, impl, expect, id, type)
                : new JsonObject { ["hasCode"] = null, ["hasTests"] = null, ["scenarios"] = null };
            detail["findings"] = FindingList(findings.TryGetValue(id, out var own) ? own : []);
            var body = BookBodyDetail.Render(index, record);
            detail["bodyHtml"] = body.Html;
            detail["sections"] = body.Sections;
            detail["fieldsHtml"] = BookProseFields.Render(index, record);
            BookRequiresDetail.Add(detail, index, id, type);
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

    /// <summary>The mockup record shape: identity, stripped fields and the source.</summary>
    /// <param name="record">The record.</param>
    /// <param name="type">The record type.</param>
    /// <returns>The mockup record shape.</returns>
    static JsonObject BaseRecord(JsonObject record, string type)
    {
        var fields = (BookRender.Fields(record)?.DeepClone() as JsonObject) ?? new JsonObject();
        foreach (var key in StrippedKeys)
        {
            fields.Remove(key);
        }

        var detail = new JsonObject
        {
            ["type"] = type,
            ["title"] = BookRender.StrProp(record, "title") ?? "",
            ["qualifiedId"] = BookRender.StrProp(record, "qualifiedId") ?? BookRender.StrProp(record, "id") ?? "",
            ["fields"] = fields,
            ["src"] = new JsonObject
            {
                ["path"] = BookDetailValues.StrNode(BookRender.StrProp(record, "file")),
                ["line"] = BookDetailValues.IntNode(BookDetailValues.IntProp(record, "line")),
            },
        };

        // The computed facts the model builder states beside the record: the
        // screen captures and the problem evidence strength. The book reads only
        // these names; it computes none of them itself.
        foreach (var key in new[] { "captures", "captureState", "evidenceStrength" })
        {
            if (record[key] is { } node)
            {
                detail[key] = node.DeepClone();
            }
        }

        return detail;
    }

    /// <summary>
    ///     Adds the Intent coverage of one requirement or journey: a
    ///     requirement carries whether a Behavior record reaches it and the
    ///     reaching records; a journey carries its reached and total step
    ///     counts and one entry per step.
    /// </summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="coverage">The computed Intent coverage status.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    static void AddIntentCoverage(JsonObject detail, IntentCoverage coverage, string id, string type)
    {
        if (type == "requirement")
        {
            var entry = coverage.Entries.FirstOrDefault(candidate => candidate.Kind == "requirement" && candidate.Id == id);
            var reachedBy = new JsonArray();
            if (entry is not null)
            {
                foreach (var source in entry.ReachedBy)
                {
                    reachedBy.Add(source);
                }
            }

            detail["intentCoverage"] = new JsonObject
            {
                ["reached"] = entry?.Reached ?? false,
                ["reachedBy"] = reachedBy,
            };
        }
        else if (type == "journey")
        {
            var steps = new JsonArray();
            var reached = 0;
            foreach (var entry in coverage.Entries.Where(candidate => candidate.Kind == "step" && candidate.Id.StartsWith(id + "#", StringComparison.Ordinal)))
            {
                var reachedBy = new JsonArray();
                foreach (var source in entry.ReachedBy)
                {
                    reachedBy.Add(source);
                }

                if (entry.Reached)
                {
                    reached++;
                }

                steps.Add(new JsonObject
                {
                    ["id"] = entry.Id,
                    ["reached"] = entry.Reached,
                    ["reachedBy"] = reachedBy,
                });
            }

            detail["intentCoverage"] = new JsonObject
            {
                ["reached"] = reached,
                ["total"] = steps.Count,
                ["steps"] = steps,
            };
        }
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
