using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The implementation detail of one record: the code implementing it, the tests
///     proving it, what it expects, and what it lacks.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookImplementationDetail
{
    /// <summary>Gets the record types that expect code.</summary>
    static HashSet<string> ImplExpected { get; } = new(StringComparer.Ordinal)
    {
        "aggregate", "reaction", "job", "external-system", "use-case", "screen",
    };

    /// <summary>Gets the record types that expect proof.</summary>
    static HashSet<string> ProofExpected { get; } = new(StringComparer.Ordinal)
    {
        "scenario", "rule", "use-case",
    };

    /// <summary>
    ///     The code implementing one record: handler, endpoint and aggregate method
    ///     entries with their model lines, reaction and job entries with theirs, and
    ///     every other implements link with a null line at the file head.
    /// </summary>
    /// <param name="index">The model index.</param>
    /// <param name="handlers">The handler entries per record id.</param>
    /// <param name="endpoints">The endpoint entries per record id.</param>
    /// <param name="methods">The aggregate method entries per record id.</param>
    /// <param name="runnersByPath">The runner entry per class path.</param>
    /// <param name="routeEntry">The screen's screens entry, or null.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    /// <returns>The implementing code entries.</returns>
    public static JsonArray ImplFor(
        BookIndex index,
        Dictionary<string, List<JsonObject>> handlers,
        Dictionary<string, List<JsonObject>> endpoints,
        Dictionary<string, List<JsonObject>> methods,
        Dictionary<string, JsonObject> runnersByPath,
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
                ["line"] = BookDetailValues.IntNode(line),
            });
        }

        if (handlers.TryGetValue(id, out var ownHandlers))
        {
            foreach (var handler in ownHandlers)
            {
                Add("handler", BookRender.StrProp(handler, "path"), BookDetailValues.IntProp(handler, "line"));
            }
        }

        if (endpoints.TryGetValue(id, out var ownEndpoints))
        {
            foreach (var endpoint in ownEndpoints)
            {
                Add("endpoint", BookRender.StrProp(endpoint, "path"), BookDetailValues.IntProp(endpoint, "line"));
            }
        }

        if (methods.TryGetValue(id, out var ownMethods))
        {
            foreach (var method in ownMethods)
            {
                Add("aggregate-method", BookRender.StrProp(method, "path"), BookDetailValues.IntProp(method, "line"));
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

            if (kind is "automation" or "reaction" or "job" && runnersByPath.TryGetValue(path, out var runner))
            {
                Add(kind, path, BookDetailValues.IntProp(runner, "line"));
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
    /// <param name="tests">The record's tests.</param>
    /// <returns>The test list.</returns>
    public static JsonArray TestList(List<JsonObject> tests)
    {
        var list = new JsonArray();
        foreach (var test in tests)
        {
            list.Add(test);
        }

        return list;
    }

    /// <summary>Whether one record expects code, a test, or both.</summary>
    /// <param name="record">The record.</param>
    /// <param name="type">The record type.</param>
    /// <returns>The expectation.</returns>
    public static JsonObject ExpectFor(JsonObject record, string type)
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

    /// <summary>The missing marks of one record: not implemented, not tested, or scenarios not tested.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="tests">The tests per record id.</param>
    /// <param name="impl">The implementing code entries.</param>
    /// <param name="expect">The expectation.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    /// <returns>The missing marks.</returns>
    public static JsonArray LacksFor(BookIndex index, Dictionary<string, List<JsonObject>> tests, JsonArray impl, JsonObject expect, string id, string type)
    {
        var lacks = new JsonArray();
        if (BookRender.IsTrue(expect["impl"]) && impl.Count == 0)
        {
            lacks.Add("not implemented");
        }

        if (!BookRender.IsTrue(expect["proof"]))
        {
            return lacks;
        }

        if (type == "use-case")
        {
            var children = BookModelQueries.UseCaseChildren(index, id);
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
                lacks.Add("scenarios not tested");
            }
        }
        else if (!tests.TryGetValue(id, out var own) || own.Count == 0)
        {
            lacks.Add("not tested");
        }

        return lacks;
    }
}
