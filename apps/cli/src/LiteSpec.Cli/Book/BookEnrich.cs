using System.Text.Json.Nodes;
using LiteSpec.Book;

namespace LiteSpec.Book;

/// <summary>
///     The record details the book shards carry: the mockup record shape plus every
///     computed field the client renders, built from the model only.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookEnrich
{
    /// <summary>One outgoing or incoming edge: the other end and the field key.</summary>
    /// <param name="Id">The other record id, or a code or test id for computed edges.</param>
    /// <param name="Key">The field key, such as use-case.effects.calls, or a computed kind.</param>
    sealed record Edge(string Id, string Key);

    static readonly HashSet<string> KnownLayers = new(StringComparer.Ordinal)
    {
        "intent", "behavior", "implementation", "proof", "governance", "glossary",
    };

    static readonly HashSet<string> StepServerTypes = new(StringComparer.Ordinal)
    {
        "use-case", "automation", "notification", "screen",
    };

    static readonly HashSet<string> ImplExpected = new(StringComparer.Ordinal)
    {
        "aggregate", "automation", "external-system", "use-case", "screen",
    };

    static readonly HashSet<string> ProofExpected = new(StringComparer.Ordinal)
    {
        "scenario", "rule", "use-case",
    };

    static readonly string[] StrippedKeys =
    [
        "type", "id", "title", "owner", "status", "decider", "decideBy",
    ];

    /// <summary>Builds one detail object per record id from the model and the outline.</summary>
    public static IReadOnlyDictionary<string, JsonObject> Build(JsonObject model, BookOutlineResult outline)
    {
        var index = BookModel.BuildIndex(model);
        var layers = LayerById(outline);
        var kept = KeptBy(index);
        var served = ServedBy(index);
        var tests = TestsByRecord(model, index);
        var findings = FindingsBySubject(model, index);
        var decisions = DecisionsByTarget(index);
        var effective = EffectiveById(index);
        var routes = RoutesByScreen(model);
        var calls = CallsByScreen(model);
        var handlers = CodeByKey(model, "handlers", "useCase");
        var endpoints = CodeByKey(model, "endpoints", "useCase");
        var methods = CodeByKey(model, "aggregateMethods", "aggregate");
        var automationsByPath = CodeByPath(model, "automations");

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
            detail["parent"] = StrNode(ParentOf(outline, id));
            detail["layer"] = StrNode(layers.GetValueOrDefault(id));
            AddProof(detail, index, id, type);
            AddMachine(detail, index, id, type);
            AddReachable(detail, index, id, type);
            AddKept(detail, kept, id, type);
            AddSteps(detail, served, index, record, id, type);
            JsonObject? routeEntry = null;
            if (type == "screen")
            {
                routes.TryGetValue(id, out routeEntry);
                AddRoute(detail, routeEntry, calls);
            }

            var impl = ImplFor(index, handlers, endpoints, methods, automationsByPath, routeEntry, id, type);
            detail["impl"] = impl;
            detail["tests"] = TestList(tests.TryGetValue(id, out var ownTests) ? ownTests : []);
            var expect = ExpectFor(record, type);
            detail["expect"] = expect;
            detail["lacks"] = LacksFor(index, tests, impl, expect, id, type);
            detail["heldBy"] = HeldBy(outline, decisions, effective, record, id);
            detail["findings"] = FindingList(findings.TryGetValue(id, out var own) ? own : []);
            var body = RenderBody(index, record);
            detail["bodyHtml"] = body.Html;
            detail["sections"] = body.Sections;
            order.Add(id);
            details[id] = detail;
        }

        var outs = OutEdges(index, details, calls, routes);
        var ins = InEdges(model, index, outs);
        foreach (var id in order)
        {
            details[id]["out"] = EdgeList(outs.TryGetValue(id, out var from) ? from : []);
            details[id]["in"] = EdgeList(ins.TryGetValue(id, out var to) ? to : []);
        }

        return details;
    }

    /// <summary>The mockup record shape: identity, status, stripped fields and the source.</summary>
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
            ["owner"] = StrNode(BookRender.StrProp(record, "owner")),
            ["status"] = status,
            ["effective"] = effective,
            ["decider"] = StrNode(BookRender.StrProp(record, "decider")),
            ["decideBy"] = StrNode(BookRender.StrProp(record, "decideBy")),
            ["fields"] = fields,
            ["src"] = new JsonObject
            {
                ["path"] = StrNode(BookRender.StrProp(record, "file")),
                ["line"] = IntNode(IntProp(record, "line")),
            },
        };
    }

    /// <summary>The outline parent id, or null for a root.</summary>
    static string? ParentOf(BookOutlineResult outline, string id) =>
        outline.ParentOf.TryGetValue(id, out var parent) ? parent : null;

    /// <summary>Maps every outline node id to its layer key.</summary>
    static Dictionary<string, string> LayerById(BookOutlineResult outline)
    {
        var layers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var root in outline.Layers)
        {
            var key = LayerKeyOf(root);
            var stack = new Stack<BookOutlineNode>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (!string.IsNullOrEmpty(node.Id) && !layers.ContainsKey(node.Id))
                {
                    layers[node.Id] = key;
                }

                foreach (var child in node.Children)
                {
                    stack.Push(child);
                }
            }
        }

        return layers;
    }

    /// <summary>The layer key of one outline root, from its id or label.</summary>
    static string LayerKeyOf(BookOutlineNode root)
    {
        var id = root.Id ?? "";
        if (id.StartsWith("layer/", StringComparison.Ordinal))
        {
            return id["layer/".Length..];
        }

        if (KnownLayers.Contains(id))
        {
            return id;
        }

        var slash = id.IndexOf('/');
        if (slash > 0 && KnownLayers.Contains(id[..slash]))
        {
            return id[..slash];
        }

        var label = (root.Label ?? "").ToLowerInvariant();
        if (KnownLayers.Contains(label))
        {
            return label;
        }

        return id;
    }

    /// <summary>Counts, per fulfilled requirement, how many records of each type fulfill it.</summary>
    static Dictionary<string, Dictionary<string, int>> KeptBy(BookIndex index)
    {
        var kept = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        foreach (var record in index.Records.OfType<JsonObject>())
        {
            var type = BookRender.StrProp(record, "type") ?? "";
            foreach (var target in StringsOf(BookRender.Fields(record)?["fulfills"]))
            {
                if (!kept.TryGetValue(target, out var counts))
                {
                    counts = new Dictionary<string, int>(StringComparer.Ordinal);
                    kept[target] = counts;
                }

                counts[type] = counts.TryGetValue(type, out var count) ? count + 1 : 1;
            }
        }

        return kept;
    }

    /// <summary>The records serving each journey step id, such as journey/name#s1.</summary>
    static Dictionary<string, List<string>> ServedBy(BookIndex index)
    {
        var served = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var record in index.Records.OfType<JsonObject>())
        {
            var type = BookRender.StrProp(record, "type");
            if (type is null || !StepServerTypes.Contains(type))
            {
                continue;
            }

            var id = BookRender.StrProp(record, "id");
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            foreach (var target in StringsOf(BookRender.Fields(record)?["serves"]))
            {
                if (!target.Contains('#'))
                {
                    continue;
                }

                if (!served.TryGetValue(target, out var list))
                {
                    list = [];
                    served[target] = list;
                }

                list.Add(id);
            }
        }

        return served;
    }

    /// <summary>Groups unit and browser tests by the record id their claim names.</summary>
    static Dictionary<string, List<JsonObject>> TestsByRecord(JsonObject model, BookIndex index)
    {
        var byRecord = new Dictionary<string, List<JsonObject>>(StringComparer.Ordinal);
        void Add(string claim, JsonObject entry)
        {
            if (!byRecord.TryGetValue(claim, out var list))
            {
                list = [];
                byRecord[claim] = list;
            }

            list.Add(entry);
        }

        foreach (var test in model["tests"] as JsonArray ?? [])
        {
            if (test is not JsonObject entry)
            {
                continue;
            }

            var scenario = BookRender.StrProp(entry, "scenario") ?? "";
            var name = HumanTestName(scenario);
            var file = BookRender.StrProp(entry, "file");
            var line = IntProp(entry, "line");
            foreach (var claim in StringsOf(entry["claims"]))
            {
                var target = BaseId(claim);
                var confirmed = index.ProvesByClaim.TryGetValue(target, out var links)
                    && links.Any(link => BookRender.StrProp(link, "scenario") == scenario && BookRender.IsTrue(link["confirmed"]));
                Add(target, new JsonObject
                {
                    ["name"] = name,
                    ["path"] = StrNode(file),
                    ["line"] = IntNode(line),
                    ["confirmed"] = confirmed,
                    ["passing"] = null,
                });
            }
        }

        foreach (var browser in model["browserTests"] as JsonArray ?? [])
        {
            if (browser is not JsonObject entry)
            {
                continue;
            }

            var title = BookRender.StrProp(entry, "title") ?? "";
            var file = BookRender.StrProp(entry, "file");
            var line = IntProp(entry, "line");
            foreach (var claim in StringsOf(entry["proves"]))
            {
                Add(BaseId(claim), new JsonObject
                {
                    ["name"] = title,
                    ["path"] = StrNode(file),
                    ["line"] = IntNode(line),
                    ["confirmed"] = true,
                    ["passing"] = null,
                    ["browser"] = true,
                });
            }
        }

        return byRecord;
    }

    /// <summary>One test name in words: the last segment with underscores as spaces.</summary>
    static string HumanTestName(string scenario)
    {
        var tail = scenario;
        var dot = scenario.LastIndexOf('.');
        if (dot >= 0 && dot + 1 < scenario.Length)
        {
            tail = scenario[(dot + 1)..];
        }

        return tail.Replace('_', ' ');
    }

    /// <summary>Groups open and known findings by their subject record id.</summary>
    static Dictionary<string, List<JsonObject>> FindingsBySubject(JsonObject model, BookIndex index)
    {
        var bySubject = new Dictionary<string, List<JsonObject>>(StringComparer.Ordinal);
        void Add(JsonObject finding, bool known)
        {
            var subject = BookRender.StrProp(finding, "subject");
            if (string.IsNullOrEmpty(subject))
            {
                return;
            }

            var target = BaseId(subject);
            if (!index.ById.ContainsKey(target))
            {
                return;
            }

            if (!bySubject.TryGetValue(target, out var list))
            {
                list = [];
                bySubject[target] = list;
            }

            list.Add(new JsonObject
            {
                ["code"] = StrNode(BookRender.StrProp(finding, "code")),
                ["tier"] = StrNode(BookRender.StrProp(finding, "tier")),
                ["message"] = BookRender.StrProp(finding, "message") ?? "",
                ["known"] = known,
                ["outcome"] = finding["outcome"]?.DeepClone(),
                ["reason"] = finding["reason"]?.DeepClone(),
                ["path"] = finding["path"]?.DeepClone(),
                ["line"] = finding["line"]?.DeepClone(),
            });
        }

        foreach (var finding in FindingEntries(model["findings"]))
        {
            Add(finding, false);
        }

        foreach (var known in FindingEntries(model["known"]))
        {
            Add(known, true);
        }

        return bySubject;
    }

    /// <summary>The finding objects of a findings node, as an array or a wrapped file.</summary>
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

    /// <summary>Maps each decided record id to the decisions naming it, in identifier order.</summary>
    static Dictionary<string, List<string>> DecisionsByTarget(BookIndex index)
    {
        var byTarget = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var record in index.Records.OfType<JsonObject>())
        {
            if (BookRender.StrProp(record, "type") != "decision")
            {
                continue;
            }

            var id = BookRender.StrProp(record, "id");
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            foreach (var target in StringsOf(BookRender.Fields(record)?["decides"]))
            {
                if (!byTarget.TryGetValue(target, out var list))
                {
                    list = [];
                    byTarget[target] = list;
                }

                list.Add(id);
            }
        }

        foreach (var key in byTarget.Keys.ToList())
        {
            byTarget[key] = byTarget[key].OrderBy(id => id, StringComparer.Ordinal).ToList();
        }

        return byTarget;
    }

    /// <summary>The effective status of every record, defaulting to draft.</summary>
    static Dictionary<string, string> EffectiveById(BookIndex index)
    {
        var effective = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var record in index.Records.OfType<JsonObject>())
        {
            var id = BookRender.StrProp(record, "id");
            if (string.IsNullOrEmpty(id) || effective.ContainsKey(id))
            {
                continue;
            }

            var state = BookRender.StrProp(record, "effectiveStatus");
            if (string.IsNullOrEmpty(state))
            {
                state = BookRender.StrProp(record, "status");
            }

            effective[id] = string.IsNullOrEmpty(state) ? "draft" : state;
        }

        return effective;
    }

    /// <summary>The strings of a link field, written as one value or a list.</summary>
    static IEnumerable<string> StringsOf(JsonNode? node)
    {
        if (node is JsonArray items)
        {
            foreach (var item in items)
            {
                if (item is JsonValue scalar && scalar.TryGetValue<string>(out var text) && !string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }
        }
        else if (node is JsonValue single && single.TryGetValue<string>(out var one) && !string.IsNullOrEmpty(one))
        {
            yield return one;
        }
    }

    /// <summary>Maps each screen id to its screens entry, the last one winning.</summary>
    static Dictionary<string, JsonObject> RoutesByScreen(JsonObject model)
    {
        var routes = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var screen in model["screens"] as JsonArray ?? [])
        {
            if (screen is JsonObject entry && !string.IsNullOrEmpty(BookRender.StrProp(entry, "screen")))
            {
                routes[BookRender.StrProp(entry, "screen")!] = entry;
            }
        }

        return routes;
    }

    /// <summary>Groups computed calls links by their screen key, such as frontend plus route.</summary>
    static Dictionary<string, List<string>> CallsByScreen(JsonObject model)
    {
        var calls = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var links = model["links"] as JsonObject;
        foreach (var call in links?["calls"] as JsonArray ?? [])
        {
            if (call is not JsonObject entry)
            {
                continue;
            }

            var screen = BookRender.StrProp(entry, "screen");
            var to = BookRender.StrProp(entry, "to");
            if (string.IsNullOrEmpty(screen) || string.IsNullOrEmpty(to))
            {
                continue;
            }

            if (!calls.TryGetValue(screen, out var list))
            {
                list = [];
                calls[screen] = list;
            }

            list.Add(to);
        }

        return calls;
    }

    /// <summary>Groups one code array by the record id its key names.</summary>
    static Dictionary<string, List<JsonObject>> CodeByKey(JsonObject model, string array, string key)
    {
        var byKey = new Dictionary<string, List<JsonObject>>(StringComparer.Ordinal);
        var code = model["code"] as JsonObject;
        foreach (var entry in code?[array] as JsonArray ?? [])
        {
            if (entry is not JsonObject obj)
            {
                continue;
            }

            var id = BookRender.StrProp(obj, key);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (!byKey.TryGetValue(id, out var list))
            {
                list = [];
                byKey[id] = list;
            }

            list.Add(obj);
        }

        return byKey;
    }

    /// <summary>Maps each automation path to its code entry, the first one winning.</summary>
    static Dictionary<string, JsonObject> CodeByPath(JsonObject model, string array)
    {
        var byPath = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        var code = model["code"] as JsonObject;
        foreach (var entry in code?[array] as JsonArray ?? [])
        {
            if (entry is JsonObject obj && !string.IsNullOrEmpty(BookRender.StrProp(obj, "path")))
            {
                byPath.TryAdd(BookRender.StrProp(obj, "path")!, obj);
            }
        }

        return byPath;
    }

    /// <summary>Adds the proof state of one scenario or rule from its proves links.</summary>
    static void AddProof(JsonObject detail, BookIndex index, string id, string type)
    {
        if (type != "scenario" && type != "rule")
        {
            return;
        }

        var links = index.ProvesByClaim.TryGetValue(id, out var found) ? found : [];
        JsonObject? test = null;
        var scenario = BookRender.StrProp(links.FirstOrDefault(), "scenario");
        if (!string.IsNullOrEmpty(scenario) && index.TestsByName.TryGetValue(scenario, out var match))
        {
            test = new JsonObject
            {
                ["name"] = HumanTestName(scenario),
                ["path"] = StrNode(BookRender.StrProp(match, "file")),
                ["line"] = IntNode(IntProp(match, "line")),
            };
        }

        detail["proof"] = new JsonObject
        {
            ["state"] = ProofState(index, id),
            ["tests"] = links.Count,
            ["test"] = test,
        };
    }

    /// <summary>One scenario or rule proof state: the stored state, or the proves links while the model holds none.</summary>
    static string ProofState(BookIndex index, string id)
    {
        if (index.ScenarioStatus.TryGetValue(id, out var stored) && stored is { Length: > 0 })
        {
            return stored;
        }

        if (!BookModel.HasTestRun(index))
        {
            return "norun";
        }

        if (index.StaleIds.Contains(id))
        {
            return "stale";
        }

        var proof = BookModel.ScenarioProof(index, id);
        return proof.State switch
        {
            "proved-passing" => "passing",
            "proved-failing" => "failing",
            "proved-indirect" => "indirect",
            "awaiting-run" => "awaiting",
            "norun" => "norun",
            _ => proof.Claimed ? "unconfirmed" : "unproved",
        };
    }

    /// <summary>Adds the machine state of one record: verified use cases, met requirements, tested proof targets.</summary>
    static void AddMachine(JsonObject detail, BookIndex index, string id, string type)
    {
        if (type == "use-case")
        {
            var entry = index.Status.TryGetPropertyValue(id, out var node) ? node as JsonObject : null;
            detail["machine"] = new JsonObject
            {
                ["implemented"] = BookRender.IsTrue(entry?["implemented"]),
                ["verified"] = BookRender.IsTrue(entry?["verified"]),
            };
        }
        else if (type == "requirement")
        {
            var entry = index.RequirementStatus.TryGetValue(id, out var found) ? found : null;
            var measure = entry?["measurePassed"];
            var passed = measure is JsonValue scalar && scalar.TryGetValue<bool>(out var flag) ? flag : (bool?)null;
            JsonNode? measureNode = passed.HasValue ? JsonValue.Create(passed.Value) : null;
            var machine = new JsonObject
            {
                ["met"] = BookRender.IsTrue(entry?["verified"]),
                ["measurePassed"] = measureNode,
            };
            if (index.Tested.TryGetValue(id, out var tested))
            {
                machine["tested"] = tested;
            }

            detail["machine"] = machine;
        }
        else if (type is "decision" or "external-system" or "integration-event"
            && index.Tested.TryGetValue(id, out var tested))
        {
            detail["machine"] = new JsonObject
            {
                ["tested"] = tested,
            };
        }
    }

    /// <summary>Adds the reachable states of one aggregate, in identifier order.</summary>
    static void AddReachable(JsonObject detail, BookIndex index, string id, string type)
    {
        if (type != "aggregate")
        {
            return;
        }

        var states = new JsonArray();
        var names = index.ReachableStates.TryGetValue(id, out var found)
            ? found.OrderBy(name => name, StringComparer.Ordinal).ToList()
            : new List<string>();
        foreach (var name in names)
        {
            states.Add(name);
        }

        detail["reachable"] = states;
    }

    /// <summary>Adds the fulfilling type counts of one requirement.</summary>
    static void AddKept(JsonObject detail, Dictionary<string, Dictionary<string, int>> kept, string id, string type)
    {
        if (type != "requirement")
        {
            return;
        }

        var counts = new JsonObject();
        if (kept.TryGetValue(id, out var byType))
        {
            foreach (var pair in byType.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                counts[pair.Key] = pair.Value;
            }
        }

        detail["keptBy"] = counts;
    }

    /// <summary>Adds the step servers, step screens and exercised steps of one journey.</summary>
    static void AddSteps(JsonObject detail, Dictionary<string, List<string>> served, BookIndex index, JsonObject record, string id, string type)
    {
        if (type != "journey")
        {
            return;
        }

        var servers = new JsonObject();
        var screens = new JsonObject();
        var exercised = new JsonArray();
        foreach (var step in BookRender.Fields(record)?["steps"] as JsonArray ?? [])
        {
            if (step is not JsonObject entry)
            {
                continue;
            }

            var stepId = BookRender.StrProp(entry, "id");
            if (string.IsNullOrEmpty(stepId))
            {
                continue;
            }

            var full = id + "#" + stepId;
            var all = served.TryGetValue(full, out var list) ? list : new List<string>();
            var use = new JsonArray();
            var onScreen = new JsonArray();
            foreach (var server in all)
            {
                if (server.StartsWith("screen/", StringComparison.Ordinal))
                {
                    onScreen.Add(server);
                }
                else
                {
                    use.Add(server);
                }
            }

            servers[stepId] = use;
            screens[stepId] = onScreen;
            if (index.ExercisedSteps.Contains(full))
            {
                exercised.Add(stepId);
            }
        }

        detail["stepServers"] = servers;
        detail["stepScreens"] = screens;
        detail["exercised"] = exercised;
    }

    /// <summary>Adds the route and called use cases of one screen.</summary>
    static void AddRoute(JsonObject detail, JsonObject? routeEntry, Dictionary<string, List<string>> calls)
    {
        if (routeEntry is null)
        {
            detail["route"] = null;
            detail["calls"] = new JsonArray();
            return;
        }

        var frontend = BookRender.StrProp(routeEntry, "frontend");
        var route = BookRender.StrProp(routeEntry, "route");
        detail["route"] = new JsonObject
        {
            ["frontend"] = StrNode(frontend),
            ["route"] = StrNode(route),
            ["file"] = StrNode(BookRender.StrProp(routeEntry, "file")),
            ["reader"] = StrNode(BookRender.StrProp(routeEntry, "reader")),
        };

        var called = new JsonArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (calls.TryGetValue((frontend ?? "") + " " + (route ?? ""), out var list))
        {
            foreach (var target in list)
            {
                if (seen.Add(target))
                {
                    called.Add(target);
                }
            }
        }

        detail["calls"] = called;
    }

    /// <summary>
    ///     The code implementing one record: handler, endpoint and aggregate method
    ///     entries with their model lines, automation entries with theirs, and
    ///     every other implements link with a null line at the file head.
    /// </summary>
    static JsonArray ImplFor(
        BookIndex index,
        Dictionary<string, List<JsonObject>> handlers,
        Dictionary<string, List<JsonObject>> endpoints,
        Dictionary<string, List<JsonObject>> methods,
        Dictionary<string, JsonObject> automationsByPath,
        JsonObject? routeEntry,
        string id,
        string type)
    {
        var impl = new JsonArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        void Add(string kind, string? path, int? line)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!seen.Add(kind + "\0" + path + "\0" + (line?.ToString() ?? "null")))
            {
                return;
            }

            paths.Add(path);
            impl.Add(new JsonObject
            {
                ["kind"] = kind,
                ["path"] = path,
                ["line"] = IntNode(line),
            });
        }

        if (handlers.TryGetValue(id, out var ownHandlers))
        {
            foreach (var handler in ownHandlers)
            {
                Add("handler", BookRender.StrProp(handler, "path"), IntProp(handler, "line"));
            }
        }

        if (endpoints.TryGetValue(id, out var ownEndpoints))
        {
            foreach (var endpoint in ownEndpoints)
            {
                Add("endpoint", BookRender.StrProp(endpoint, "path"), IntProp(endpoint, "line"));
            }
        }

        if (methods.TryGetValue(id, out var ownMethods))
        {
            foreach (var method in ownMethods)
            {
                Add("aggregate-method", BookRender.StrProp(method, "path"), IntProp(method, "line"));
            }
        }

        var links = index.ImplementsByTarget.TryGetValue(id, out var found) ? found : new List<JsonObject>();
        foreach (var link in links)
        {
            var path = BookRender.StrProp(link, "path");
            var kind = BookRender.StrProp(link, "kind") ?? "";
            if (string.IsNullOrEmpty(path) || paths.Contains(path))
            {
                continue;
            }

            if (kind == "automation" && automationsByPath.TryGetValue(path, out var automation))
            {
                Add(kind, path, IntProp(automation, "line"));
            }
            else
            {
                Add(kind.Length > 0 ? kind : "handler", path, null);
            }
        }

        if (type == "screen" && routeEntry is not null)
        {
            var file = BookRender.StrProp(routeEntry, "file");
            if (!string.IsNullOrEmpty(file) && !paths.Contains(file))
            {
                Add("route", file, 1);
            }
        }

        return impl;
    }

    /// <summary>The tests proving one record, in model order.</summary>
    static JsonArray TestList(List<JsonObject> tests)
    {
        var list = new JsonArray();
        foreach (var test in tests)
        {
            list.Add(test);
        }

        return list;
    }

    /// <summary>Whether one record expects code, a test, or both.</summary>
    static JsonObject ExpectFor(JsonObject record, string type)
    {
        var impl = ImplExpected.Contains(type);
        if (type == "use-case")
        {
            var fields = BookRender.Fields(record);
            var links = BookRender.Links(record);
            impl = (fields?["reachedThrough"] as JsonArray)?.Count > 0
                || (links?["reachedThrough"] as JsonArray)?.Count > 0;
        }

        return new JsonObject
        {
            ["impl"] = impl,
            ["proof"] = ProofExpected.Contains(type),
        };
    }

    /// <summary>The missing marks of one record: no code, no test, or untested scenarios.</summary>
    static JsonArray LacksFor(BookIndex index, Dictionary<string, List<JsonObject>> tests, JsonArray impl, JsonObject expect, string id, string type)
    {
        var lacks = new JsonArray();
        if (BookRender.IsTrue(expect["impl"]) && impl.Count == 0)
        {
            lacks.Add("no code");
        }

        if (!BookRender.IsTrue(expect["proof"]))
        {
            return lacks;
        }

        if (type == "use-case")
        {
            var children = BookModel.UseCaseChildren(index, id);
            var missing = children.Scenarios.Count == 0;
            if (!missing)
            {
                foreach (var scenario in children.Scenarios)
                {
                    var scenarioId = BookRender.StrProp(scenario, "id");
                    if (string.IsNullOrEmpty(scenarioId) || !tests.TryGetValue(scenarioId, out var proved) || proved.Count == 0)
                    {
                        missing = true;
                        break;
                    }
                }
            }

            if (missing)
            {
                lacks.Add("scenarios without a test");
            }
        }
        else if (!tests.TryGetValue(id, out var own) || own.Count == 0)
        {
            lacks.Add("no test");
        }

        return lacks;
    }

    /// <summary>The draft up-links holding one settled record at effective draft.</summary>
    static JsonArray HeldBy(BookOutlineResult outline, Dictionary<string, List<string>> decisions, Dictionary<string, string> effective, JsonObject record, string id)
    {
        var held = new JsonArray();
        var status = BookRender.StrProp(record, "status");
        if (string.IsNullOrEmpty(status))
        {
            status = "draft";
        }

        if (status == "draft" || (effective.TryGetValue(id, out var current) ? current : status) != "draft")
        {
            return held;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();
        void TryAdd(string? candidate)
        {
            if (string.IsNullOrEmpty(candidate))
            {
                return;
            }

            var target = BaseId(candidate);
            if (target == id || !effective.TryGetValue(target, out var state) || state != "draft")
            {
                return;
            }

            if (seen.Add(target))
            {
                ordered.Add(target);
            }
        }

        TryAdd(outline.ParentOf.TryGetValue(id, out var parent) ? parent : null);
        if (decisions.TryGetValue(id, out var deciding))
        {
            foreach (var decision in deciding)
            {
                TryAdd(decision);
            }
        }

        foreach (var target in StringsOf(BookRender.Fields(record)?["fulfills"]))
        {
            TryAdd(target);
        }

        foreach (var target in ordered.Take(6))
        {
            held.Add(target);
        }

        return held;
    }

    /// <summary>The findings of one record, in model order.</summary>
    static JsonArray FindingList(List<JsonObject> findings)
    {
        var list = new JsonArray();
        foreach (var finding in findings)
        {
            list.Add(finding);
        }

        return list;
    }

    /// <summary>Renders one record body to HTML with hash record links, split by section.</summary>
    static (string Html, JsonObject Sections) RenderBody(BookIndex index, JsonObject record)
    {
        var body = BookRender.StrProp(record, "body") ?? "";
        (string Href, string Title)? LinkRecord(string target)
        {
            var targetBase = BaseId(target);
            if (!index.ById.TryGetValue(targetBase, out var found))
            {
                return null;
            }

            return ("#" + target, BookRender.StrProp(found, "title") ?? targetBase);
        }

        var split = SplitSections(body);
        var items = new JsonArray();
        foreach (var section in split.Items)
        {
            items.Add(new JsonObject
            {
                ["h"] = section.Heading,
                ["html"] = BookMarkdown.Render(section.Body, LinkRecord),
            });
        }

        return (BookMarkdown.Render(body, LinkRecord), new JsonObject
        {
            ["intro"] = BookMarkdown.Render(split.Intro, LinkRecord),
            ["list"] = items,
        });
    }

    /// <summary>Splits one body on its level-two headings into an intro and sections.</summary>
    static (string Intro, List<(string Heading, string Body)> Items) SplitSections(string body)
    {
        var normalized = body.Replace("\r\n", "\n").Replace('\r', '\n');
        var intro = new List<string>();
        var items = new List<(string Heading, string Body)>();
        string? heading = null;
        var current = new List<string>();
        foreach (var line in normalized.Split('\n'))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal) && line.Length > 3)
            {
                if (heading is not null)
                {
                    items.Add((heading, string.Join("\n", current)));
                }

                heading = line[3..].Trim();
                current = new List<string>();
                continue;
            }

            if (heading is null)
            {
                intro.Add(line);
            }
            else
            {
                current.Add(line);
            }
        }

        if (heading is not null)
        {
            items.Add((heading, string.Join("\n", current)));
        }

        return (string.Join("\n", intro), items);
    }

    /// <summary>The outgoing edges of every record from its fields and computed calls.</summary>
    static Dictionary<string, List<Edge>> OutEdges(BookIndex index, Dictionary<string, JsonObject> details, Dictionary<string, List<string>> calls, Dictionary<string, JsonObject> routes)
    {
        var outs = new Dictionary<string, List<Edge>>(StringComparer.Ordinal);
        foreach (var pair in details)
        {
            var id = pair.Key;
            var type = BookRender.StrProp(index.ById.GetValueOrDefault(id), "type")
                ?? BookRender.StrProp(pair.Value, "type")
                ?? "";
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<Edge>();
            outs[id] = list;
            void Add(string to, string key)
            {
                if (to == id || !seen.Add(to + "\0" + key))
                {
                    return;
                }

                list.Add(new Edge(to, key));
            }

            if (!string.IsNullOrEmpty(type))
            {
                ScanFields(pair.Value["fields"], type, id, index, Add);
            }

            if (type == "screen" && routes.TryGetValue(id, out var route))
            {
                var key = (BookRender.StrProp(route, "frontend") ?? "") + " " + (BookRender.StrProp(route, "route") ?? "");
                if (calls.TryGetValue(key, out var called))
                {
                    foreach (var target in called)
                    {
                        if (index.ById.ContainsKey(target))
                        {
                            Add(target, "calls");
                        }
                    }
                }
            }
        }

        return outs;
    }

    /// <summary>Walks one fields node, adding an edge per resolving record id.</summary>
    static void ScanFields(JsonNode? node, string path, string from, BookIndex index, Action<string, string> add)
    {
        if (node is JsonValue scalar && scalar.TryGetValue<string>(out var text) && !string.IsNullOrEmpty(text))
        {
            if (LooksLikeRecordId(text))
            {
                var target = BaseId(text);
                if (target != from && index.ById.ContainsKey(target))
                {
                    add(target, path);
                }
            }
        }
        else if (node is JsonArray items)
        {
            foreach (var item in items)
            {
                ScanFields(item, path, from, index, add);
            }
        }
        else if (node is JsonObject obj)
        {
            foreach (var pair in obj)
            {
                ScanFields(pair.Value, path + "." + pair.Key, from, index, add);
            }
        }
    }

    /// <summary>The incoming edges of every record, inverting outgoing and adding computed ones.</summary>
    static Dictionary<string, List<Edge>> InEdges(JsonObject model, BookIndex index, Dictionary<string, List<Edge>> outs)
    {
        var ins = new Dictionary<string, List<Edge>>(StringComparer.Ordinal);
        foreach (var id in index.ById.Keys)
        {
            ins[id] = new List<Edge>();
        }

        foreach (var pair in outs)
        {
            foreach (var edge in pair.Value)
            {
                if (!ins.TryGetValue(edge.Id, out var list))
                {
                    list = new List<Edge>();
                    ins[edge.Id] = list;
                }

                list.Add(new Edge(pair.Key, edge.Key));
            }
        }

        foreach (var key in ins.Keys.ToList())
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unique = new List<Edge>();
            foreach (var edge in ins[key])
            {
                if (seen.Add(edge.Id + "\0" + edge.Key))
                {
                    unique.Add(edge);
                }
            }

            ins[key] = unique;
        }

        var links = model["links"] as JsonObject ?? new JsonObject();
        foreach (var link in links["implements"] as JsonArray ?? [])
        {
            if (link is not JsonObject entry)
            {
                continue;
            }

            var to = BookRender.StrProp(entry, "to");
            var path = BookRender.StrProp(entry, "path");
            if (!string.IsNullOrEmpty(to) && !string.IsNullOrEmpty(path) && ins.TryGetValue(to, out var list))
            {
                list.Add(new Edge("code/" + path, "implements"));
            }
        }

        foreach (var link in links["proves"] as JsonArray ?? [])
        {
            if (link is not JsonObject entry)
            {
                continue;
            }

            var claim = BookRender.StrProp(entry, "claim");
            var scenario = BookRender.StrProp(entry, "scenario");
            if (!string.IsNullOrEmpty(claim) && !string.IsNullOrEmpty(scenario) && ins.TryGetValue(BaseId(claim), out var list))
            {
                list.Add(new Edge("test/" + scenario, "proves"));
            }
        }

        foreach (var link in links["follows"] as JsonArray ?? [])
        {
            if (link is not JsonObject entry)
            {
                continue;
            }

            var to = BookRender.StrProp(entry, "to");
            var path = BookRender.StrProp(entry, "path");
            if (!string.IsNullOrEmpty(to) && !string.IsNullOrEmpty(path) && ins.TryGetValue(to, out var list))
            {
                list.Add(new Edge("code/" + path, "follows"));
            }
        }

        return ins;
    }

    /// <summary>One edge list as id and key objects.</summary>
    static JsonArray EdgeList(List<Edge> edges)
    {
        var list = new JsonArray();
        foreach (var edge in edges)
        {
            list.Add(new JsonObject
            {
                ["id"] = edge.Id,
                ["key"] = edge.Key,
            });
        }

        return list;
    }

    /// <summary>Strips the step fragment of an id, such as journey/name#s1.</summary>
    static string BaseId(string id)
    {
        var hash = id.IndexOf('#');
        return hash < 0 ? id : id[..hash];
    }

    /// <summary>Whether a string has the shape of a record id, such as type/name.</summary>
    static bool LooksLikeRecordId(string value)
    {
        var target = BaseId(value);
        var slash = target.IndexOf('/');
        if (slash <= 0 || slash + 1 >= target.Length)
        {
            return false;
        }

        foreach (var ch in target[..slash])
        {
            if (ch != '-' && (ch < 'a' || ch > 'z'))
            {
                return false;
            }
        }

        return char.IsLetterOrDigit(target[slash + 1]);
    }

    /// <summary>One nullable string as a JSON node, null staying null.</summary>
    static JsonNode? StrNode(string? value) => value is null ? null : JsonValue.Create(value);

    /// <summary>One nullable line number as a JSON node, null staying null.</summary>
    static JsonNode? IntNode(int? value) => value.HasValue ? JsonValue.Create(value.Value) : null;

    /// <summary>Reads an integer property, or null when it is missing or not a number.</summary>
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
}
