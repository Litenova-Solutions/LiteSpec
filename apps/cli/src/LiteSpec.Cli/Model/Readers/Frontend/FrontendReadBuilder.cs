using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Screens;
using LiteSpec.Cli.Model.Tests;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Readers.Frontend;

/// <summary>
///     Turns one frontend's facts into screens, mark elements and browser tests:
///     each route's calls map through the endpoints' <c>.WithName</c> operations
///     to their use cases, and each route's full-page navigations map through
///     the endpoints' routes to the same use cases, keeping each operation paired
///     with the use case it reaches, an operation or a path two endpoints map to
///     different use cases is a <c>behavior-unrecorded</c> diagnostic, and an
///     operation no endpoint names stays on the screen as the unmatched-call fact.
///     A call or navigation a layout reaches counts for the same use cases, and
///     is recorded as layout-only when the route's own files reach no call to it.
/// </summary>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
static class FrontendReadBuilder
{
    /// <summary>Builds the screen read for one frontend from its facts.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <param name="frontend">The frontend whose facts are built into a screen read.</param>
    /// <param name="facts">The frontend facts the result is built from.</param>
    /// <param name="code">The code model the screens are checked against.</param>
    /// <returns>The screen read built from the frontend facts.</returns>
    public static ScreenRead Build(ConsumerConfig config, FrontendConfig frontend, FrontendFacts facts, CodeFacts code)
    {
        code.Elements.AddRange(ModuleElements(config, frontend, facts));
        code.Elements.AddRange(RouteElements(config, frontend, facts));

        var byOperation = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var endpoint in code.Endpoints)
        {
            if (endpoint.Operation.Length == 0)
            {
                continue;
            }

            if (!byOperation.TryGetValue(Naming.OperationKey(endpoint.Operation), out var useCases))
            {
                useCases = new SortedSet<string>(StringComparer.Ordinal);
                byOperation[Naming.OperationKey(endpoint.Operation)] = useCases;
            }

            useCases.Add(endpoint.UseCaseId);
        }

        var byRoute = new Dictionary<string, List<EndpointRow>>(StringComparer.Ordinal);
        var prefixed = new List<(string Prefix, EndpointRow Row)>();

        foreach (var endpoint in code.Endpoints)
        {
            if (endpoint.Route.Length == 0 || endpoint.UseCaseId.Length == 0)
            {
                continue;
            }

            var path = NormalizePath(endpoint.Route);

            if (path.Length == 0)
            {
                continue;
            }

            if (!byRoute.TryGetValue(path, out var rows))
            {
                rows = [];
                byRoute[path] = rows;
            }

            var row = new EndpointRow(endpoint.Operation, endpoint.UseCaseId, endpoint.Method, endpoint.Route, endpoint.File);
            rows.Add(row);

            // A parameterized route is also matched by its fixed prefix, so a
            // navigation carrying a concrete value or a template head reaches it.
            if (FixedPrefix(path) is { } prefix)
            {
                prefixed.Add((prefix, row));
            }
        }

        var screens = new List<CodeScreen>();
        var findings = new List<Finding>();

        foreach (var route in facts.Routes)
        {
            var file = Absolute(frontend, route.File);
            var useCases = new SortedSet<string>(StringComparer.Ordinal);
            var ownUseCases = new SortedSet<string>(StringComparer.Ordinal);
            var operations = new SortedSet<string>(StringComparer.Ordinal);
            var navigated = new HashSet<GeneratedOperation>();
            var paired = new List<ScreenCall>();
            var pairedSet = new HashSet<(string Operation, string UseCase)>();

            foreach (var call in route.Calls)
            {
                operations.Add(call.Operation);

                if (!byOperation.TryGetValue(Naming.OperationKey(call.Operation), out var mapped))
                {
                    continue;
                }

                if (mapped.Count > 1)
                {
                    findings.Add(new Finding(
                        "behavior-unrecorded",
                        $"Route '{frontend.Name} {route.Address}' calls '{call.Operation}', which endpoints map to different use cases ('{string.Join("', '", mapped)}'); delete the code, or write its parent record from a requirement, drafting the requirement if none exists.",
                        Absolute(frontend, call.File), call.Line, null)
                    {
                        Reader = facts.Reader,
                        ReaderIsQualified = facts.Qualified,
                    });
                    continue;
                }

                useCases.Add(mapped.Min!);

                if (!call.FromLayout)
                {
                    ownUseCases.Add(mapped.Min!);
                }

                if (pairedSet.Add((call.Operation, mapped.Min!)))
                {
                    paired.Add(new ScreenCall(call.Operation, mapped.Min!));
                }
            }

            foreach (var navigation in route.Navigations ?? [])
            {
                var path = NormalizePath(navigation.Path);

                if (path.Length == 0 || MatchRoute(path, byRoute, prefixed) is not { Count: > 0 } rows)
                {
                    continue;
                }

                var mapped = new SortedSet<string>(rows.Select(row => row.UseCaseId), StringComparer.Ordinal);

                if (mapped.Count > 1)
                {
                    findings.Add(new Finding(
                        "behavior-unrecorded",
                        $"Route '{frontend.Name} {route.Address}' navigates to '{navigation.Path}', which endpoints map to different use cases ('{string.Join("', '", mapped)}'); delete the code, or write its parent record from a requirement, drafting the requirement if none exists.",
                        Absolute(frontend, navigation.File), navigation.Line, null)
                    {
                        Reader = facts.Reader,
                        ReaderIsQualified = facts.Qualified,
                    });
                    continue;
                }

                var reached = mapped.Min!;
                useCases.Add(reached);

                if (!navigation.FromLayout)
                {
                    ownUseCases.Add(reached);
                }

                var operation = OperationFor(code, reached, rows);
                navigated.Add(new GeneratedOperation(operation, rows[0].Method, rows[0].Route));

                if (pairedSet.Add((operation, reached)))
                {
                    paired.Add(new ScreenCall(operation, reached));
                }
            }

            screens.Add(new CodeScreen(
                frontend.Name,
                route.Address,
                PathText.Relative(config.Root, file),
                [.. operations.Select(operation => new GeneratedOperation(operation, "", ""))
                    .Concat(navigated)
                    .OrderBy(operation => operation.Name, StringComparer.Ordinal)
                    .ThenBy(operation => operation.Method, StringComparer.Ordinal)
                    .ThenBy(operation => operation.Path, StringComparer.Ordinal)],
                [.. useCases],
                route.Marks.Count > 0 ? route.Marks[0] : null,
                facts.Reader,
                route.Line)
            {
                Calls =
                [
                    .. paired
                        .OrderBy(call => call.Operation, StringComparer.Ordinal)
                        .ThenBy(call => call.UseCase, StringComparer.Ordinal),
                ],
                LayoutOnlyUseCases = [.. useCases.Except(ownUseCases, StringComparer.Ordinal)],
            });
        }

        if (facts.Failure is { } failure)
        {
            findings.Add(new Finding(
                "reader-missing",
                $"Frontend '{frontend.Name}' could not be read: {failure}",
                frontend.Path, 1, null)
            {
                Reader = facts.Reader,
                ReaderIsQualified = false,
            });
        }

        return new ScreenRead(screens, [])
        {
            BrowserTests =
            [
                .. facts.Tests.Select(test => new CodeBrowserTest(
                    frontend.Name, test.Title, Absolute(frontend, test.File), test.Line,
                    test.Proves, test.Flow, test.Gotos, test.Codes, test.Texts, facts.Reader)),
            ],
            Findings = findings,
            TypeScriptQualified = facts.Reader == Finding.TypeScriptReader && facts.Qualified,
        };
    }

    /// <summary>One endpoint row that maps a route: its operation, use case, method, route and file.</summary>
    /// <param name="Operation">The <c>.WithName</c> operation, or empty when the row maps only a route.</param>
    /// <param name="UseCaseId">The use case the endpoint marks.</param>
    /// <param name="Method">The HTTP method, or empty when the row maps only an operation.</param>
    /// <param name="Route">The route template, or empty when the row maps only an operation.</param>
    /// <param name="File">The endpoint file.</param>
    private sealed record EndpointRow(string Operation, string UseCaseId, string Method, string Route, string File);

    /// <summary>The operation a navigation to the use case counts as: the endpoint's <c>.WithName</c> operation in the matched file, else the first operation the use case names, else empty.</summary>
    /// <param name="code">The code model holding the endpoint operations.</param>
    /// <param name="useCaseId">The use case the navigation reaches.</param>
    /// <param name="rows">The endpoint rows the navigation path matched.</param>
    /// <returns>The operation name, or empty when the use case names none.</returns>
    private static string OperationFor(CodeFacts code, string useCaseId, IReadOnlyList<EndpointRow> rows)
    {
        var files = new HashSet<string>(rows.Select(row => row.File), StringComparer.OrdinalIgnoreCase);

        var sameFile = code.Endpoints
            .Where(endpoint => endpoint.UseCaseId == useCaseId
                && endpoint.Operation.Length > 0
                && files.Contains(endpoint.File))
            .Select(endpoint => endpoint.Operation)
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();

        if (sameFile is not null)
        {
            return sameFile;
        }

        return code.Endpoints
            .Where(endpoint => endpoint.UseCaseId == useCaseId && endpoint.Operation.Length > 0)
            .Select(endpoint => endpoint.Operation)
            .Order(StringComparer.Ordinal)
            .FirstOrDefault() ?? "";
    }

    /// <summary>The path a route or a navigation is matched by: without its query and fragment, without a trailing slash, or empty when it names no path from the root.</summary>
    /// <param name="route">The route template or navigation path as written.</param>
    /// <returns>The normalized path, or empty when it names no path from the root.</returns>
    private static string NormalizePath(string route)
    {
        var end = route.IndexOfAny(['?', '#']);

        var path = end >= 0 ? route[..end] : route;

        if (path.Length > 1 && path.EndsWith('/'))
        {
            path = path.TrimEnd('/');
        }

        return path.StartsWith('/') ? path : "";
    }

    /// <summary>The fixed prefix a parameterized route is matched by: the normalized route up to its first parameter, without a trailing slash, or null when the route names no parameter or no usable prefix.</summary>
    /// <param name="path">The normalized endpoint route.</param>
    /// <returns>The fixed prefix, or null when the route matches by exact path only.</returns>
    private static string? FixedPrefix(string path)
    {
        var open = path.IndexOf('{');

        if (open < 0)
        {
            return null;
        }

        var prefix = path[..open].TrimEnd('/');

        return prefix.Length > 1 ? prefix : null;
    }

    /// <summary>Every endpoint row a navigation reaches: the rows of its exact route, plus the rows of each parameterized route whose fixed prefix it starts with at a segment boundary.</summary>
    /// <param name="path">The normalized navigation path.</param>
    /// <param name="byRoute">The endpoint rows by exact normalized route.</param>
    /// <param name="prefixed">The endpoint rows of parameterized routes with their fixed prefixes, in endpoint order.</param>
    /// <returns>The matched rows, exact matches first, or an empty list when nothing matches.</returns>
    private static List<EndpointRow> MatchRoute(
        string path,
        Dictionary<string, List<EndpointRow>> byRoute,
        List<(string Prefix, EndpointRow Row)> prefixed)
    {
        var matched = new List<EndpointRow>();

        if (byRoute.TryGetValue(path, out var exact))
        {
            matched.AddRange(exact);
        }

        foreach (var (prefix, row) in prefixed)
        {
            if (path.Equals(prefix, StringComparison.Ordinal)
                || path.StartsWith(prefix + "/", StringComparison.Ordinal))
            {
                matched.Add(row);
            }
        }

        return matched;
    }

    /// <summary>One marked element per use-case module folder in the facts, in file and line order.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <param name="frontend">The frontend whose files are read.</param>
    /// <param name="facts">The frontend facts the result is built from.</param>
    /// <returns>One marked element per use-case module folder, in file and line order.</returns>
    private static IReadOnlyList<MarkedElement> ModuleElements(
        ConsumerConfig config, FrontendConfig frontend, FrontendFacts facts)
    {
        var found = new List<MarkedElement>();

        foreach (var module in facts.Modules)
        {
            var marks = module.Marks
                .Select(id => new CodeMark(id, MarkStyle.Typed, MarkVerb.Implements, Absolute(frontend, module.File), module.Line))
                .ToList();
            var file = marks.Count > 0 ? Absolute(frontend, module.File) : Absolute(frontend, module.Folder);

            found.Add(new MarkedElement(
                CodeKind.FrontendModule, ModuleName(frontend, module.Folder), null,
                file, module.Line, marks, InTests: false));
        }

        return [.. found.OrderBy(element => element.File, StringComparer.Ordinal).ThenBy(element => element.Line)];
    }

    /// <summary>One marked element per route entry in the facts, in file and line order.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <param name="frontend">The frontend whose files are read.</param>
    /// <param name="facts">The frontend facts the result is built from.</param>
    /// <returns>One marked element per route entry, in file and line order.</returns>
    private static IReadOnlyList<MarkedElement> RouteElements(
        ConsumerConfig config, FrontendConfig frontend, FrontendFacts facts)
    {
        var found = new List<MarkedElement>();

        foreach (var route in facts.Routes)
        {
            var file = Absolute(frontend, route.File);
            var marks = route.Marks
                .Select((id, index) => new CodeMark(id, RouteStyle(config, frontend, route, index), MarkVerb.Implements, file, route.Line))
                .ToList();

            found.Add(new MarkedElement(
                CodeKind.FrontendRoute, route.Address, null, file, route.Line, marks, InTests: false));
        }

        return [.. found.OrderBy(element => element.File, StringComparer.Ordinal).ThenBy(element => element.Line)];
    }

    /// <summary>
    ///     The style the route mark was written in: the typed constant on a TypeScript
    ///     frontend, the style the .NET reader saw on a Blazor or Android frontend,
    ///     and the declared style when the reader recorded none.
    /// </summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <param name="frontend">The frontend whose files are read.</param>
    /// <param name="route">The route entry whose mark style is wanted.</param>
    /// <param name="index">The position of the mark within the route marks, matching its recorded style.</param>
    /// <returns>The mark style that applies to this route mark.</returns>
    private static MarkStyle RouteStyle(ConsumerConfig config, FrontendConfig frontend, RouteFact route, int index)
    {
        if (frontend.Platform is "svelte" or "react")
        {
            return MarkStyle.Typed;
        }

        if (route.Styles is { } styles && index < styles.Count)
        {
            return styles[index];
        }

        return config.Marks;
    }

    /// <summary>The module folder name a mark finding shows: the path under the modules root.</summary>
    /// <param name="frontend">The frontend whose files are read.</param>
    /// <param name="folder">The module folder path, relative to the frontend root.</param>
    /// <returns>The module folder name as findings show it, under the modules root.</returns>
    private static string ModuleName(FrontendConfig frontend, string folder)
    {
        var root = frontend.Platform == "react" ? "src/modules/" : "src/lib/modules/";

        return folder.StartsWith(root, StringComparison.Ordinal) ? folder[root.Length..] : folder;
    }

    /// <summary>The absolute path of a frontend-relative path under the frontend root.</summary>
    /// <param name="frontend">The frontend whose files are read.</param>
    /// <param name="relative">The path relative to the frontend root, with forward slashes.</param>
    /// <returns>The absolute path under the frontend root.</returns>
    private static string Absolute(FrontendConfig frontend, string relative) =>
        Path.GetFullPath(Path.Combine(frontend.Path, relative.Replace('/', Path.DirectorySeparatorChar)));
}
