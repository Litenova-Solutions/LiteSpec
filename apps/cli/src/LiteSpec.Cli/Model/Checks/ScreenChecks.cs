using System.Text.RegularExpressions;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>
///     The screen checks: every page has its screen record, a screen's offers
///     match its computed calls, a screen serves only steps its use cases serve,
///     every browser or device step has a screen, every reachable refusal is
///     shown, browser proofs confirm and are not duplicated, and a use case is
///     reached on each frontend that serves its entry point.
/// </summary>
/// <follows>litespec/core/edges#give-every-page-its-screen-record</follows>
internal static class ScreenChecks
{
    /// <summary>Reports screen-unimplemented, screen-offers-drift, screen-serves-drift, step-screen-missing, refusal-sentence-missing, reader-missing and proves-duplicate.</summary>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckScreenImplemented(model, findings);
        CheckRouteRecorded(model, findings);
        CheckScreenCalls(model, findings);
        CheckScreenServes(model, findings);
        CheckStepUnscreened(model, findings);
        CheckResultUnshown(model, findings);
        CheckBrowserProofs(model, findings);
        CheckProvesDuplicated(model, findings);
        CheckRouteSuiteMarks(model, findings);
        CheckFrontendMarkScope(model, findings);
    }

    /// <summary>
    ///     A frontend mark naming a use case or screen outside that frontend's
    ///     own entry points is <c>mark-unknown</c>: the generated records
    ///     file of that frontend does not hold the id.
    /// </summary>
    private static void CheckFrontendMarkScope(ProjectModel model, List<Finding> findings)
    {
        foreach (var element in model.Code.Elements.Where(element =>
            element.Kind is CodeKind.FrontendModule or CodeKind.FrontendRoute))
        {
            var frontend = model.Config.Frontends.FirstOrDefault(candidate =>
                element.File.StartsWith(candidate.Path + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

            if (frontend is null)
            {
                continue;
            }

            var entryPoints = new HashSet<string>(FrontendRecords.EntryPointsOf(model, frontend), StringComparer.Ordinal);

            foreach (var mark in element.Marks)
            {
                if (!model.ById.TryGetValue(mark.RecordId, out var record))
                {
                    continue;
                }

                var inScope = record.Type switch
                {
                    RecordTypes.UseCase => record.Ids(RecordFields.ReachedThrough).Any(entryPoints.Contains),
                    RecordTypes.Screen => record.Ids(RecordFields.EntryPoint).Any(entryPoints.Contains),
                    _ => true,
                };

                if (!inScope)
                {
                    findings.Add(new Finding(
                        "mark-unknown",
                        $"{KindName(element.Kind)} '{DisplayName(element)}' marks '{mark.RecordId}', which is outside frontend '{frontend.Name}'s entry points.",
                        mark.File, mark.Line, mark.RecordId)
                    {
                        Reader = ReaderForElement(model, element),
                        ReaderIsQualified = ReaderForElement(model, element) == Finding.TypeScriptReader
                            ? model.TypeScriptQualified
                            : true,
                    });
                }
            }
        }
    }

    /// <summary>
    ///     The route suite and the visual goldens each frontend names prove UI
    ///     provisions, patterns or quality requirements, never a scenario: a
    ///     test there with no <c>proves</c> annotation is <c>mark-missing</c>,
    ///     and one naming a scenario is <c>mark-unknown</c>.
    /// </summary>
    private static void CheckRouteSuiteMarks(ProjectModel model, List<Finding> findings)
    {
        foreach (var frontend in model.Config.Frontends)
        {
            var named = new[] { frontend.RouteSuite, frontend.VisualGoldens }
                .OfType<string>()
                .ToList();

            if (named.Count == 0)
            {
                continue;
            }

            foreach (var test in model.BrowserTests.Where(test => test.Frontend == frontend.Name))
            {
                if (!named.Any(path => UnderPath(test.File, path)))
                {
                    continue;
                }

                var qualified = test.Reader == Finding.TypeScriptReader ? model.TypeScriptQualified : true;

                if (test.Proves.Count == 0)
                {
                    findings.Add(new Finding(
                        "mark-missing",
                        $"Browser test '{test.Title}' in frontend '{frontend.Name}'s route suite or visual goldens carries no proves annotation.",
                        test.File, test.Line, null)
                    {
                        Reader = test.Reader,
                        ReaderIsQualified = qualified,
                    });
                }

                foreach (var claim in test.Proves.Where(claim => claim.StartsWith("scenario/", StringComparison.Ordinal)))
                {
                    findings.Add(new Finding(
                        "mark-unknown",
                        $"Browser test '{test.Title}' in frontend '{frontend.Name}'s route suite or visual goldens proves '{claim}', which is a scenario.",
                        test.File, test.Line, claim)
                    {
                        Reader = test.Reader,
                        ReaderIsQualified = qualified,
                    });
                }
            }
        }
    }

    private static bool UnderPath(string file, string path)
    {
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(file, path, comparison)
            || file.StartsWith(path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison);
    }

    /// <summary>The route elements naming the screen, across every frontend.</summary>
    private static IReadOnlyList<MarkedElement> RoutesFor(ProjectModel model, string screenId) =>
        [.. model.Code.Elements.Where(element =>
            element.Kind == CodeKind.FrontendRoute
            && element.Marks.Any(mark => mark.RecordId == screenId))];

    /// <summary>The computed screens (routes) naming the screen record.</summary>
    private static IReadOnlyList<ScreenInfo> ReadScreensFor(ProjectModel model, string screenId) =>
        [.. model.Screens.Where(screen => screen.ScreenId == screenId)];

    private static void CheckScreenImplemented(ProjectModel model, List<Finding> findings)
    {
        foreach (var screen in ScreenRecords(model))
        {
            if (RoutesFor(model, screen.Id).Count > 0)
            {
                continue;
            }

            findings.Add(new Finding(
                "screen-unimplemented",
                $"Screen '{screen.Id}' has no route carrying its mark.",
                screen.File, screen.Line, SubjectOf(screen))
            {
                Reader = ReaderForScreen(model, screen),
                ReaderIsQualified = QualifiedForScreen(model, screen),
            });
        }
    }

    private static void CheckRouteRecorded(ProjectModel model, List<Finding> findings)
    {
        foreach (var element in model.Code.Elements.Where(element => element.Kind == CodeKind.FrontendRoute))
        {
            var reader = ReaderForElement(model, element);
            var qualified = reader == Finding.TypeScriptReader ? model.TypeScriptQualified : true;

            if (element.Marks.Count == 0)
            {
                findings.Add(new Finding(
                    "behavior-unrecorded",
                    $"Route '{element.Class}' carries no screen mark.",
                    element.File, element.Line, null)
                {
                    Reader = reader,
                    ReaderIsQualified = qualified,
                });
                continue;
            }

            foreach (var mark in element.Marks)
            {
                if (!model.ById.TryGetValue(mark.RecordId, out var record) || record.Type != RecordTypes.Screen)
                {
                    findings.Add(new Finding(
                        "behavior-unrecorded",
                        $"Route '{element.Class}' marks '{mark.RecordId}', which names no screen.",
                        mark.File, mark.Line, mark.RecordId.Length > 0 ? mark.RecordId : null)
                    {
                        Reader = reader,
                        ReaderIsQualified = qualified,
                    });
                }
            }
        }
    }

    private static void CheckScreenCalls(ProjectModel model, List<Finding> findings)
    {
        foreach (var screen in ScreenRecords(model))
        {
            var routes = ReadScreensFor(model, screen.Id);

            if (routes.Count == 0)
            {
                continue;
            }

            var offers = new HashSet<string>(screen.Ids(RecordFields.Offers), StringComparer.Ordinal);
            var calls = new HashSet<string>(routes.SelectMany(route => route.UseCases), StringComparer.Ordinal);

            if (offers.SetEquals(calls))
            {
                continue;
            }

            var parts = new List<string>();

            foreach (var id in offers.Except(calls).Order(StringComparer.Ordinal))
            {
                parts.Add($"offers '{id}' but its route does not call it");
            }

            foreach (var id in calls.Except(offers).Order(StringComparer.Ordinal))
            {
                parts.Add($"calls '{id}' but does not offer it");
            }

            var first = routes[0];

            findings.Add(new Finding(
                "screen-offers-drift",
                $"Screen '{screen.Id}' {string.Join("; ", parts)}.",
                ScreenFile(model, first.File), first.Line, SubjectOf(screen))
            {
                Reader = first.Reader,
                ReaderIsQualified = first.Reader == Finding.TypeScriptReader ? model.TypeScriptQualified : true,
            });
        }
    }

    private static void CheckScreenServes(ProjectModel model, List<Finding> findings)
    {
        foreach (var screen in ScreenRecords(model))
        {
            var servedByOffers = new HashSet<string>(StringComparer.Ordinal);

            foreach (var useCaseId in screen.Ids(RecordFields.Offers))
            {
                if (model.ById.TryGetValue(useCaseId, out var useCase))
                {
                    servedByOffers.UnionWith(useCase.Ids(RecordFields.Serves));
                }
            }

            foreach (var step in screen.Ids(RecordFields.Serves))
            {
                if (!servedByOffers.Contains(step))
                {
                    findings.Add(new Finding(
                        "screen-serves-drift",
                        $"Screen '{screen.Id}' serves '{step}', which no use case it offers serves.",
                        screen.File, screen.Line, SubjectOf(screen)));
                }
            }
        }
    }

    private static void CheckStepUnscreened(ProjectModel model, List<Finding> findings)
    {
        var screened = new HashSet<string>(ScreenRecords(model).SelectMany(screen => screen.Ids(RecordFields.Serves)), StringComparer.Ordinal);

        foreach (var journey in model.Records.Where(record => record.Type == RecordTypes.Journey))
        {
            foreach (var step in journey.StepIds())
            {
                if (screened.Contains(step))
                {
                    continue;
                }

                var servedByBrowser = model.Records
                    .Where(record => record.Type == RecordTypes.UseCase && record.Ids(RecordFields.Serves).Contains(step, StringComparer.Ordinal))
                    .Any(useCase => useCase.Ids(RecordFields.ReachedThrough).Any(id =>
                        model.ById.TryGetValue(id, out var point)
                        && point.Type == RecordTypes.EntryPoint
                        && point.Field(RecordFields.Kind) is "browser" or "device"));

                if (servedByBrowser)
                {
                    findings.Add(new Finding(
                        "step-screen-missing",
                        $"Journey step '{step}' is served by a browser or device use case but by no screen.",
                        journey.File, journey.Line, step));
                }
            }
        }
    }

    private static void CheckResultUnshown(ProjectModel model, List<Finding> findings)
    {
        foreach (var screen in ScreenRecords(model))
        {
            foreach (var route in ReadScreensFor(model, screen.Id))
            {
                var frontend = model.Config.Frontends.FirstOrDefault(candidate => candidate.Name == route.Frontend);

                if (frontend is null)
                {
                    continue;
                }

                var dictionary = RefusalDictionaryKeys(frontend);
                var asserted = new HashSet<string>(
                    model.BrowserTests
                        .Where(test => test.Frontend == frontend.Name)
                        .SelectMany(test => test.Codes),
                    StringComparer.Ordinal);

                foreach (var useCaseId in screen.Ids(RecordFields.Offers))
                {
                    if (!Identifiers.TrySplit(useCaseId, out _, out _, out var useCaseName) || useCaseName.Length == 0)
                    {
                        continue;
                    }

                    foreach (var result in model.Records.Where(record =>
                        record.Type == RecordTypes.Result
                        && !record.Derived
                        && record.Field(RecordFields.Outcome) == "refusal"
                        && Identifiers.TrySplit(record.Id, out _, out var resultHome, out _)
                        && resultHome == useCaseName))
                    {
                        var code = result.Field(RecordFields.Code);

                        if (code is null || dictionary.Contains(code) || asserted.Contains(code))
                        {
                            continue;
                        }

                        findings.Add(new Finding(
                            "refusal-sentence-missing",
                            $"Screen '{screen.Id}' reaches refusal '{code}' of '{useCaseId}' but has no sentence in frontend '{frontend.Name}'s refusal dictionary and no browser test asserting its code.",
                            screen.File, screen.Line, SubjectOf(screen))
                        {
                            Reader = route.Reader,
                            ReaderIsQualified = route.Reader == Finding.TypeScriptReader ? model.TypeScriptQualified : true,
                        });
                    }
                }
            }
        }
    }

    /// <summary>The refusal codes a frontend's dictionary holds a sentence for, read from its file or folder.</summary>
    private static IReadOnlySet<string> RefusalDictionaryKeys(FrontendConfig frontend)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var path = frontend.RefusalDictionary;

        if (path is null)
        {
            return keys;
        }

        IEnumerable<string> files = File.Exists(path)
            ? [path]
            : Directory.Exists(path)
                ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                : [];

        foreach (var file in files)
        {
            string text;

            try
            {
                text = File.ReadAllText(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (Match match in DictionaryKey.Matches(text))
            {
                var code = match.Groups["code"].Value.Length > 0 ? match.Groups["code"].Value : match.Groups["code2"].Value;

                if (code.Length > 0)
                {
                    keys.Add(code);
                }
            }
        }

        return keys;
    }

    private static readonly Regex DictionaryKey = new(@"(?:^|[\s{,\""'])[""']?(?<code>[a-z][a-z0-9-]*(?:\.[a-z0-9-]+)+)[""']?\s*[:=]|^#{1,6}\s+(?<code2>[a-z][a-z0-9-]*(?:\.[a-z0-9-]+)+)\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static void CheckBrowserProofs(ProjectModel model, List<Finding> findings)
    {
        foreach (var test in model.BrowserTests)
        {
            foreach (var claim in test.Proves)
            {
                if (!IsBehaviorClaim(claim))
                {
                    continue;
                }

                if (!ModelBuilder.BrowserConfirm(model, test, claim))
                {
                    findings.Add(new Finding(
                        "proves-unreached",
                        $"Browser test '{test.Title}' claims '{claim}' but no goto reaches a screen offering its use case, or no assertion in the same test holds its result.",
                        test.File, test.Line, claim)
                    {
                        Reader = test.Reader,
                        ReaderIsQualified = test.Reader == Finding.TypeScriptReader ? model.TypeScriptQualified : true,
                    });
                }
            }

            foreach (var claim in test.Proves.Where(claim => !IsBehaviorClaim(claim)))
            {
                if (!model.ById.TryGetValue(claim, out var record)
                    || record.Type is not (RecordTypes.Requirement or RecordTypes.Decision or RecordTypes.ExternalSystem or RecordTypes.IntegrationEvent))
                {
                    if (!claim.StartsWith("litespec/", StringComparison.Ordinal))
                    {
                        findings.Add(new Finding(
                            "mark-unknown",
                            $"Browser test '{test.Title}' proves '{claim}', which matches no record a test may prove.",
                            test.File, test.Line, claim.Length > 0 ? claim : null)
                        {
                            Reader = test.Reader,
                            ReaderIsQualified = test.Reader == Finding.TypeScriptReader ? model.TypeScriptQualified : true,
                        });
                    }
                }
            }
        }
    }

    private static void CheckProvesDuplicated(ProjectModel model, List<Finding> findings)
    {
        var byScenario = new Dictionary<string, List<(string Test, string File, int Line, string Reader)>>(StringComparer.Ordinal);

        void Add(string claim, string test, string file, int line, string reader)
        {
            if (!claim.StartsWith("scenario/", StringComparison.Ordinal))
            {
                return;
            }

            if (!byScenario.TryGetValue(claim, out var list))
            {
                list = [];
                byScenario[claim] = list;
            }

            list.Add((test, file, line, reader));
        }

        foreach (var test in model.Tests)
        {
            foreach (var claim in test.Claims)
            {
                Add(claim, test.Name, test.File, test.Line, Finding.RoslynReader);
            }
        }

        foreach (var test in model.BrowserTests)
        {
            foreach (var claim in test.Proves)
            {
                Add(claim, test.Title, test.File, test.Line, test.Reader);
            }
        }

        foreach (var (scenario, tests) in byScenario.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (tests.Count <= 1)
            {
                continue;
            }

            var extra = tests[1];

            findings.Add(new Finding(
                "proves-duplicate",
                $"Scenario '{scenario}' carries proves marks on more than one test ({string.Join(", ", tests.Select(test => $"'{test.Test}'"))}).",
                extra.File, extra.Line, scenario)
            {
                Reader = extra.Reader,
                ReaderIsQualified = extra.Reader == Finding.TypeScriptReader ? model.TypeScriptQualified : true,
            });
        }
    }

    /// <summary>The reader of the route that marks the screen, or TypeScript when none does.</summary>
    private static string ReaderForScreen(ProjectModel model, Record screen)
    {
        var route = ReadScreensFor(model, screen.Id).FirstOrDefault();

        return route?.Reader ?? Finding.TypeScriptReader;
    }

    private static bool QualifiedForScreen(ProjectModel model, Record screen) =>
        ReaderForScreen(model, screen) != Finding.TypeScriptReader || model.TypeScriptQualified;

    private static string ReaderForElement(ProjectModel model, MarkedElement element)
    {
        var screen = model.Screens.FirstOrDefault(candidate =>
            string.Equals(ScreenFile(model, candidate.File), element.File, StringComparison.OrdinalIgnoreCase));

        return screen?.Reader ?? Finding.TypeScriptReader;
    }
}
