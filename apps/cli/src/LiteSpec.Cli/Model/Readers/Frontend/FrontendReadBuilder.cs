using LiteSpec.Model;

namespace LiteSpec.Model.Readers.Frontend;

/// <summary>
///     Turns one frontend's facts into screens, mark elements and browser tests:
///     each route's calls map through the endpoints' <c>.WithName</c> operations
///     to their use cases, an operation two endpoints map to different use cases
///     is a <c>behavior-unrecorded</c> diagnostic, and an operation no endpoint
///     names stays on the screen as the unmatched-call fact.
/// </summary>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
static class FrontendReadBuilder
{
    /// <summary>Builds the screen read for one frontend from its facts.</summary>
    public static ScreenRead Build(ConsumerConfig config, FrontendConfig frontend, FrontendFacts facts, CodeModel code)
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

        var screens = new List<ScreenInfo>();
        var findings = new List<Finding>();

        foreach (var route in facts.Routes)
        {
            var file = Absolute(frontend, route.File);
            var useCases = new SortedSet<string>(StringComparer.Ordinal);
            var operations = new SortedSet<string>(StringComparer.Ordinal);

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
            }

            screens.Add(new ScreenInfo(
                frontend.Name,
                route.Address,
                config.Relative(file),
                [.. operations.Select(operation => new GeneratedOperation(operation, "", ""))],
                [.. useCases],
                route.Marks.FirstOrDefault(),
                facts.Reader,
                route.Line));
        }

        if (facts.Failure is { } failure)
        {
            findings.Add(new Finding(
                "reader-unavailable",
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
                .. facts.Tests.Select(test => new BrowserTestInfo(
                    frontend.Name, test.Title, Absolute(frontend, test.File), test.Line,
                    test.Proves, test.Journey, test.Gotos, test.Codes, test.Texts, facts.Reader)),
            ],
            Findings = findings,
            TypeScriptQualified = facts.Reader == Finding.TypeScriptReader && facts.Qualified,
        };
    }

    private static IReadOnlyList<MarkedElement> ModuleElements(
        ConsumerConfig config, FrontendConfig frontend, FrontendFacts facts)
    {
        var found = new List<MarkedElement>();

        foreach (var module in facts.Modules)
        {
            var marks = module.Marks
                .Select(id => new CodeMark(id, config.Marks, MarkVerb.Implements, Absolute(frontend, module.File), module.Line))
                .ToList();
            var file = marks.Count > 0 ? Absolute(frontend, module.File) : Absolute(frontend, module.Folder);

            found.Add(new MarkedElement(
                CodeKind.FrontendModule, ModuleName(frontend, module.Folder), null,
                file, module.Line, marks, InTests: false));
        }

        return [.. found.OrderBy(element => element.File, StringComparer.Ordinal).ThenBy(element => element.Line)];
    }

    private static IReadOnlyList<MarkedElement> RouteElements(
        ConsumerConfig config, FrontendConfig frontend, FrontendFacts facts)
    {
        var found = new List<MarkedElement>();

        foreach (var route in facts.Routes)
        {
            var file = Absolute(frontend, route.File);
            var marks = route.Marks
                .Select(id => new CodeMark(id, config.Marks, MarkVerb.Implements, file, route.Line))
                .ToList();

            found.Add(new MarkedElement(
                CodeKind.FrontendRoute, route.Address, null, file, route.Line, marks, InTests: false));
        }

        return [.. found.OrderBy(element => element.File, StringComparer.Ordinal).ThenBy(element => element.Line)];
    }

    /// <summary>The module folder name a mark finding shows: the path under the modules root.</summary>
    private static string ModuleName(FrontendConfig frontend, string folder)
    {
        var root = frontend.Platform == "react" ? "src/modules/" : "src/lib/modules/";

        return folder.StartsWith(root, StringComparison.Ordinal) ? folder[root.Length..] : folder;
    }

    private static string Absolute(FrontendConfig frontend, string relative) =>
        Path.GetFullPath(Path.Combine(frontend.Path, relative.Replace('/', Path.DirectorySeparatorChar)));
}
