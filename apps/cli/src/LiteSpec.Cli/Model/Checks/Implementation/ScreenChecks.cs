using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Model.Screens;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>
///     The screen checks: every page has its screen record, a screen's offers
///     match its computed calls, a screen serves only steps its use cases serve,
///     every browser or device step has a screen, browser proofs confirm and are not duplicated, and a use case is
///     reached on each frontend that serves its entry point.
/// </summary>
/// <follows>litespec/core/edges#give-every-page-its-screen-record</follows>
internal static class ScreenChecks
{
    /// <summary>Reports screen-unimplemented, screen-offers-drift, screen-serves-drift, step-screen-missing, reader-missing, proves-duplicate and a route outside its stack's routes root.</summary>
    /// <param name="model">The loaded project model that holds every record, each screen and each browser test.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckScreenImplemented(model, findings);
        CheckRouteRecorded(model, findings);
        CheckRoutePlaced(model, findings);
        CheckScreenCalls(model, findings);
        CheckScreenServes(model, findings);
        CheckStepUnscreened(model, findings);
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
    /// <param name="model">The loaded project model that holds each marked element and every frontend entry point.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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
                    RecordTypes.Screen => FrontendRecords.EntryPointOf(record) is { } point && entryPoints.Contains(point),
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
    /// <param name="model">The loaded project model that holds each frontend and each browser test.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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

    /// <summary>Decides whether a file is the named path or sits beneath it, with the platform path comparison.</summary>
    /// <param name="file">The test file path under test.</param>
    /// <param name="path">The suite or goldens path the frontend names.</param>
    /// <returns>True when the file equals the path or sits under it; otherwise false.</returns>
    private static bool UnderPath(string file, string path)
    {
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(file, path, comparison)
            || file.StartsWith(path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison);
    }

    /// <summary>The route elements naming the screen, across every frontend.</summary>
    /// <param name="model">The loaded project model that holds each marked code element.</param>
    /// <param name="screenId">The screen record identifier the route must mark.</param>
    /// <returns>Each route element that marks the screen.</returns>
    private static IReadOnlyList<MarkedElement> RoutesFor(ProjectModel model, string screenId) =>
        [.. model.Code.Elements.Where(element =>
            element.Kind == CodeKind.FrontendRoute
            && element.Marks.Any(mark => mark.RecordId == screenId))];

    /// <summary>The computed screens (routes) naming the screen record.</summary>
    /// <param name="model">The loaded project model that holds each computed screen.</param>
    /// <param name="screenId">The screen record identifier the computed screen must name.</param>
    /// <returns>Each computed screen that names the record.</returns>
    private static IReadOnlyList<CodeScreen> ReadScreensFor(ProjectModel model, string screenId) =>
        [.. model.Screens.Where(screen => screen.ScreenId == screenId)];

    /// <summary>Reports each screen record that no route element marks.</summary>
    /// <param name="model">The loaded project model that holds each screen record and route element.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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

    /// <summary>Reports each route element that carries no screen mark or marks no screen record.</summary>
    /// <param name="model">The loaded project model that holds each route element and screen record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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

    /// <summary>Reports each route entry outside its stack's routes root: the web routes root, the Blazor Routes folder, or the Android Screens folder. A route in no declared frontend states no root, so it is left out.</summary>
    /// <param name="model">The loaded project model that holds each frontend and route element.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRoutePlaced(ProjectModel model, List<Finding> findings)
    {
        foreach (var element in model.Code.Elements.Where(element => element.Kind == CodeKind.FrontendRoute))
        {
            var frontend = model.Config.Frontends.FirstOrDefault(candidate =>
                element.File.StartsWith(candidate.Path + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

            if (frontend is null || RoutesRootOf(frontend) is not { } root)
            {
                continue;
            }

            if (UnderPath(element.File, root))
            {
                continue;
            }

            var reader = ReaderForElement(model, element);
            var mark = element.Marks.Count > 0 ? element.Marks[0] : null;
            var subject = mark is not null && mark.RecordId.Length > 0 ? mark.RecordId : null;
            var file = mark?.File ?? element.File;
            var line = mark?.Line ?? element.Line;
            var shown = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            findings.Add(new Finding(
                "code-placement-drift",
                $"Route '{element.Class}' sits outside the '{shown}/' root.",
                file, line, subject)
            {
                Reader = reader,
                ReaderIsQualified = reader == Finding.TypeScriptReader ? model.TypeScriptQualified : true,
            });
        }
    }

    /// <summary>The routes root of one frontend: the web routes root, the Blazor Routes folder, or the Android Screens folder.</summary>
    /// <param name="frontend">The frontend whose routes root is wanted.</param>
    /// <returns>The absolute routes root, or null when the platform states none.</returns>
    private static string? RoutesRootOf(FrontendConfig frontend) =>
        frontend.Platform.ToLowerInvariant() switch
        {
            "svelte" or "react" => frontend.RoutesRoot,
            "blazor" => Path.Combine(frontend.Path, "Routes"),
            "android" => Path.Combine(frontend.Path, "Screens"),
            _ => null,
        };

    /// <summary>Reports each screen whose offered use cases differ from the use cases its routes call.</summary>
    /// <param name="model">The loaded project model that holds each screen record and computed screen.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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

    /// <summary>Reports each flow step a screen serves that none of its offered use cases serves.</summary>
    /// <param name="model">The loaded project model that holds each screen and use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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

    /// <summary>Reports each flow step served by a browser or device use case that no screen serves.</summary>
    /// <param name="model">The loaded project model that holds each flow, screen and use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckStepUnscreened(ProjectModel model, List<Finding> findings)
    {
        var screened = new HashSet<string>(ScreenRecords(model).SelectMany(screen => screen.Ids(RecordFields.Serves)), StringComparer.Ordinal);

        foreach (var flow in model.Records.Where(record => record.Type == RecordTypes.Flow))
        {
            foreach (var step in flow.StepIds())
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
                        $"Flow step '{step}' is served by a browser or device use case but by no screen.",
                        flow.File, flow.Line, step));
                }
            }
        }
    }

    /// <summary>Reports each browser test claim that its screens and assertions leave unreached or that names no provable record.</summary>
    /// <param name="model">The loaded project model that holds each browser test and record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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

    /// <summary>Reports each scenario that more than one unit or browser test carries a proves mark for.</summary>
    /// <param name="model">The loaded project model that holds each unit test and browser test.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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
    /// <param name="model">The loaded project model that holds each computed screen.</param>
    /// <param name="screen">The screen record whose marking route reader is wanted.</param>
    /// <returns>The reader of the first computed screen for the record, or the TypeScript reader when none exists.</returns>
    private static string ReaderForScreen(ProjectModel model, Record screen)
    {
        var routes = ReadScreensFor(model, screen.Id);
        var route = routes.Count > 0 ? routes[0] : null;

        return route?.Reader ?? Finding.TypeScriptReader;
    }

    /// <summary>Decides whether findings on the screen carry a qualified reader.</summary>
    /// <param name="model">The loaded project model that holds the TypeScript qualification state.</param>
    /// <param name="screen">The screen record whose reader qualification is tested.</param>
    /// <returns>True when the marking reader is not TypeScript or the TypeScript reads are qualified; otherwise false.</returns>
    private static bool QualifiedForScreen(ProjectModel model, Record screen) =>
        ReaderForScreen(model, screen) != Finding.TypeScriptReader || model.TypeScriptQualified;

    /// <summary>The reader of the computed screen held in the element file, or TypeScript when none matches.</summary>
    /// <param name="model">The loaded project model that holds each computed screen.</param>
    /// <param name="element">The marked code element whose file decides the reader.</param>
    /// <returns>The matching screen reader, or the TypeScript reader when no screen file matches.</returns>
    private static string ReaderForElement(ProjectModel model, MarkedElement element)
    {
        var screen = model.Screens.FirstOrDefault(candidate =>
            string.Equals(ScreenFile(model, candidate.File), element.File, StringComparison.OrdinalIgnoreCase));

        return screen?.Reader ?? Finding.TypeScriptReader;
    }
}
