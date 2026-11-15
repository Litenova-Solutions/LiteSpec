using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Book.Output;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Model;

namespace LiteSpec.Cli.Book.Details;

/// <summary>
///     The record details the book shards carry: the mockup record shape plus every
///     computed field the client renders, built from the model only. Each computed
///     field is built by its own detail type; this type assembles them per record.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgements-in-the-book</follows>
static class BookRecordDetails
{
    /// <summary>Gets the identity fields the mockup record shape strips from the fields it carries.</summary>
    static string[] StrippedKeys { get; } =
    [
        "type", "id", "title", "owner", "status", "decider", "decideBy",
    ];

    /// <summary>Builds one detail object per record id from the model and the outline with every layer claimed.</summary>
    /// <param name="model">The parsed model.</param>
    /// <param name="outline">The built outline.</param>
    /// <returns>The detail object per record id.</returns>
    public static IReadOnlyDictionary<string, JsonObject> Build(JsonObject model, BookOutlineResult outline) =>
        Build(model, outline, LayerClaims.All);

    /// <summary>
    ///     Builds one detail object per record id from the model and the outline
    ///     under the layers the project claims. A claimed Implementation layer
    ///     adds code and its expectation, and a claimed Proof layer adds tests,
    ///     proof status and scenario proof. The expectation, the completion and
    ///     the missing marks stop at the claimed layers, so an unclaimed layer
    ///     never reads as missing. Every detail that is a requirement or a
    ///     flow carries its Intent coverage. Every written type is a member
    ///     of <see cref="RecordTypes"/> and every written layer is a member of
    ///     the outline layer keys or null.
    /// </summary>
    /// <param name="model">The parsed model.</param>
    /// <param name="outline">The built outline.</param>
    /// <param name="claims">The record layers the project claims.</param>
    /// <returns>The detail object per record id.</returns>
    public static IReadOnlyDictionary<string, JsonObject> Build(JsonObject model, BookOutlineResult outline, LayerClaims claims)
    {
        var implemented = claims.Implementation is not null;
        var proven = claims.Proof is not null;
        var coverage = BookIntentCoverage.Of(model);
        var coverageById = CoverageById(coverage);
        var stepsByFlow = StepsByFlow(coverage);
        var index = BookIndexBuilder.BuildIndex(model);
        var layers = BookDetailSources.LayerById(outline);
        var kept = BookDetailSources.KeptBy(index);
        var served = BookDetailSources.ServedBy(index);
        var tests = proven ? BookDetailSources.TestsByRecord(model, index) : new Dictionary<string, List<JsonObject>>(StringComparer.Ordinal);
        var findings = BookDetailSources.FindingsBySubject(model, index, claims);
        var routes = BookDetailSources.RoutesByScreen(model);
        var calls = BookDetailSources.CallsByScreen(model);
        var handlers = BookDetailSources.CodeByKey(model, "handlers", "useCase");
        var endpoints = BookDetailSources.CodeByKey(model, "endpoints", "useCase");
        var methods = BookDetailSources.CodeByKey(model, "aggregateMethods", "aggregate");
        var runnersByPath = BookDetailSources.CodeByPaths(model, ["reactions", "jobs"]);

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
            detail["parent"] = BookDetailValues.StrNode(BookDetailSources.ParentOf(outline, id));
            detail["layer"] = BookDetailValues.StrNode(layers.GetValueOrDefault(id));
            if (proven)
            {
                BookProofDetail.Add(detail, index, id, type);
                BookProofDetail.AddMachine(detail, index, id, type);
            }

            BookProofDetail.AddReachable(detail, index, id, type);
            BookProofDetail.AddKept(detail, kept, id, type);
            BookFlowDetail.AddSteps(detail, served, index, record, id, type);
            BookBlueprintDetail.AddWireframe(detail, record, type);
            BookBlueprintDetail.AddNavigationMap(detail, index, outline, id, type);
            BookBlueprintDetail.AddStoryboard(detail, served, record, id, type);
            if (!proven && type == "flow")
            {
                detail["exercised"] = new JsonArray();
            }

            AddIntentCoverage(detail, coverageById, stepsByFlow, id, type);
            if (type == "goal")
            {
                AddOutcome(detail, record);
            }
            JsonObject? routeEntry = null;
            if (type == "screen")
            {
                routes.TryGetValue(id, out routeEntry);
                BookFlowDetail.AddRoute(detail, routeEntry, calls);
            }

            var impl = implemented
                ? BookImplementationDetail.ImplFor(index, handlers, endpoints, methods, runnersByPath, routeEntry, id, type)
                : new JsonArray();
            detail["impl"] = impl;
            detail["tests"] = proven
                ? BookImplementationDetail.TestList(tests.TryGetValue(id, out var ownTests) ? ownTests : [])
                : new JsonArray();
            var expect = BookImplementationDetail.ExpectFor(record, type);
            expect["impl"] = implemented && BookRender.IsTrue(expect["impl"]);
            expect["proof"] = proven && BookRender.IsTrue(expect["proof"]);
            detail["expect"] = expect;
            detail["lacks"] = BookImplementationDetail.LacksFor(index, tests, impl, expect, id, type);
            detail["completion"] = BookImplementationDetail.CompletionFor(index, tests, impl, expect, id, type);
            detail["findings"] = FindingList(findings.TryGetValue(id, out var own) ? own : []);
            detail["sections"] = BookBodyDetail.Render(index, record);
            detail["fieldsHtml"] = BookProseFields.Render(index, record);
            BookRequiresDetail.Add(detail, index, id, type);
            order.Add(id);
            details[id] = detail;
        }

        var outs = BookEdges.OutEdges(index, details, calls, routes);
        var ins = BookEdges.InEdges(model, index, outs);
        BookDecisionDetail.AddDecidesEdges(model, index, outs, ins);
        foreach (var id in order)
        {
            details[id]["out"] = BookEdges.EdgeList(outs.TryGetValue(id, out var from) ? from : []);
            details[id]["in"] = BookEdges.EdgeList(ins.TryGetValue(id, out var to) ? to : []);
        }

        // A reach link reads as claimed until the proof of the record that makes it passes, then as verified.
        foreach (var detail in details.Values)
        {
            AddClaims(detail, details);
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
            ["folder"] = BookRender.StrProp(record, "folder") ?? "",
            ["module"] = BookRender.StrNode(BookRender.StrProp(record, "module")),
            ["fields"] = fields,
            ["src"] = new JsonObject
            {
                ["path"] = BookDetailValues.StrNode(BookRender.StrProp(record, "file")),
                ["line"] = BookDetailValues.IntNode(BookDetailValues.IntProp(record, "line")),
            },
        };

        // The computed facts stated beside the record: the screen captures and
        // the problem evidence strength from the model builder, and the decision
        // concerns from the book command. The book reads only these names; it
        // computes none of them itself. A carried capture state is always a
        // member of BookCaptureStates.
        foreach (var key in new[] { "captures", "captureState", "evidenceStrength", "concerns" })
        {
            if (record[key] is { } node)
            {
                detail[key] = node.DeepClone();
            }
        }

        return detail;
    }

    /// <summary>One coverage entry per item id, indexed once so no record scans the entries.</summary>
    /// <param name="coverage">The computed Intent coverage status.</param>
    /// <returns>The entry per item id.</returns>
    static Dictionary<string, IntentCoverage.Entry> CoverageById(IntentCoverage coverage)
    {
        var byId = new Dictionary<string, IntentCoverage.Entry>(StringComparer.Ordinal);
        foreach (var entry in coverage.Entries)
        {
            byId.TryAdd(entry.Id, entry);
        }

        return byId;
    }

    /// <summary>One step entry list per flow id, indexed once so no flow scans the entries.</summary>
    /// <param name="coverage">The computed Intent coverage status.</param>
    /// <returns>The step entries per flow id, in coverage order.</returns>
    static Dictionary<string, List<IntentCoverage.Entry>> StepsByFlow(IntentCoverage coverage)
    {
        var byFlow = new Dictionary<string, List<IntentCoverage.Entry>>(StringComparer.Ordinal);
        foreach (var entry in coverage.Entries)
        {
            if (entry.Kind != "step")
            {
                continue;
            }

            var hash = entry.Id.IndexOf('#');
            if (hash < 0)
            {
                continue;
            }

            var flow = entry.Id[..hash];
            if (!byFlow.TryGetValue(flow, out var list))
            {
                list = [];
                byFlow[flow] = list;
            }

            list.Add(entry);
        }

        return byFlow;
    }

    /// <summary>
    ///     Adds the Intent coverage of one requirement or flow: a
    ///     requirement carries whether a Behavior record reaches it and the
    ///     reaching records; a flow carries its reached and total step
    ///     counts and one entry per step.
    /// </summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="coverageById">The coverage entry per item id.</param>
    /// <param name="stepsByFlow">The step entries per flow id.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    static void AddIntentCoverage(JsonObject detail, Dictionary<string, IntentCoverage.Entry> coverageById, Dictionary<string, List<IntentCoverage.Entry>> stepsByFlow, string id, string type)
    {
        if (type == "requirement")
        {
            coverageById.TryGetValue(id, out var entry);
            if (entry is not null && entry.Kind != "requirement")
            {
                entry = null;
            }

            var reachedBy = new JsonArray();
            if (entry is not null)
            {
                foreach (var source in entry.ReachedBy)
                {
                    reachedBy.Add((JsonNode)source);
                }
            }

            detail["intentCoverage"] = new JsonObject
            {
                ["reached"] = entry?.Reached ?? false,
                ["reachedBy"] = reachedBy,
            };
        }
        else if (type == "flow")
        {
            var steps = new JsonArray();
            var reached = 0;
            foreach (var entry in stepsByFlow.TryGetValue(id, out var list) ? list : [])
            {
                var reachedBy = new JsonArray();
                foreach (var source in entry.ReachedBy)
                {
                    reachedBy.Add((JsonNode)source);
                }

                if (entry.Reached)
                {
                    reached++;
                }

                steps.Add((JsonNode)new JsonObject
                {
                    ["id"] = entry.Id,
                    ["kind"] = "step",
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
            list.Add((JsonNode)finding);
        }

        return list;
    }

    /// <summary>
    ///     Adds the outcome of one goal: "not measured" until the goal records an observation, then
    ///     "measured" with each observation as it was recorded. Delivery reads apart from this.
    /// </summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="record">The goal record.</param>
    static void AddOutcome(JsonObject detail, JsonObject record)
    {
        var observations = new JsonArray();
        foreach (var node in BookRender.Fields(record)?["observations"] as JsonArray ?? [])
        {
            if (node is JsonObject entry)
            {
                observations.Add((JsonNode)new JsonObject
                {
                    ["date"] = entry["date"]?.DeepClone(),
                    ["value"] = entry["value"]?.DeepClone(),
                    ["source"] = entry["source"]?.DeepClone(),
                });
            }
        }

        detail["outcome"] = new JsonObject
        {
            ["state"] = observations.Count > 0 ? "measured" : "not measured",
            ["observations"] = observations,
        };
    }

    /// <summary>
    ///     Adds each reaching record's claim to the requirement's reach and to each flow step's reach:
    ///     verified when that record's own proof is passing, claimed otherwise.
    /// </summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="details">The detail object per record id, complete before the claims are read.</param>
    static void AddClaims(JsonObject detail, IReadOnlyDictionary<string, JsonObject> details)
    {
        if (detail["intentCoverage"] is not JsonObject coverage)
        {
            return;
        }

        if (coverage["reachedBy"] is JsonArray reachedBy)
        {
            coverage["claims"] = ClaimsOf(reachedBy, details);
        }

        if (coverage["steps"] is JsonArray steps)
        {
            foreach (var step in steps.OfType<JsonObject>())
            {
                if (step["reachedBy"] is JsonArray stepBy)
                {
                    step["claims"] = ClaimsOf(stepBy, details);
                }
            }
        }
    }

    /// <summary>The claims of one reach list: each reaching record with its state, verified or claimed.</summary>
    /// <param name="reachedBy">The reaching record identifiers.</param>
    /// <param name="details">The detail object per record id.</param>
    /// <returns>The claims, in the reach list's order.</returns>
    static JsonArray ClaimsOf(JsonArray reachedBy, IReadOnlyDictionary<string, JsonObject> details)
    {
        var claims = new JsonArray();
        foreach (var source in reachedBy)
        {
            var id = source is JsonValue value && value.TryGetValue<string>(out var text) ? text : "";
            var verified = details.TryGetValue(id, out var target)
                && target["proof"] is JsonObject proof
                && BookRender.StrProp(proof, "state") == BookProofStates.Passing;
            claims.Add((JsonNode)new JsonObject
            {
                ["id"] = id,
                ["state"] = verified ? "verified" : "claimed",
            });
        }

        return claims;
    }
}
