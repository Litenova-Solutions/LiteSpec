namespace LiteSpec.Model;

/// <summary>
///     The mark checks: every element carries the marks its kind takes, each mark names
///     a record of the type its kind takes in the declared style of its language, each
///     backend mark sits in its record's module folder, hand-written runtime values match
///     their records, each command handler calls its marked act, each marked act raises its
///     use case's events, and every mapped record is named by a mark.
/// </summary>
static partial class Checker
{
    /// <summary>Runs the mark checks.</summary>
    private static void CheckMarks(ProjectModel model, List<Finding> findings)
    {
        CheckMarkPresence(model, findings);
        CheckMarkLinks(model, findings);
        CheckMarkFolderDrift(model, findings);
        CheckStrayMarks(model, findings);
        CheckDecisionsFollowed(model, findings);
        CheckRuntimeValueDrift(model, findings);
        CheckUseCaseActCalled(model, findings);
        CheckActEvents(model, findings);
        CheckRecordsImplemented(model, findings);
        CheckUseCaseExposed(model, findings);
        CheckEffectsImplemented(model, findings);
        CheckUseCaseReached(model, findings);
    }

    private static void CheckMarkPresence(ProjectModel model, List<Finding> findings)
    {
        foreach (var element in model.Code.Elements)
        {
            if (!element.Marks.Any(mark => MarkRules.TakesVerb(element.Kind, mark.Verb)))
            {
                findings.Add(new Finding("mark-missing", $"{KindName(element.Kind)} '{DisplayName(element)}' carries no mark.", element.File, element.Line, null));
            }
        }
    }

    private static void CheckMarkLinks(ProjectModel model, List<Finding> findings)
    {
        foreach (var element in model.Code.Elements)
        {
            foreach (var mark in element.Marks)
            {
                if (mark.Verb == MarkVerb.Follows)
                {
                    CheckFollowsMark(model, element, mark, findings);
                }
                else
                {
                    CheckServesMark(model, element, mark, findings);
                }

                if (UsesDeclaredStyle(element.Kind) && mark.Style != model.Config.Marks)
                {
                    var subject = mark.RecordId.Length > 0 ? mark.RecordId : null;

                    findings.Add(new Finding("mark-style-invalid", $"{KindName(element.Kind)} '{DisplayName(element)}' writes its mark '{mark.RecordId}' in the {mark.Style.ToProjectValue()} style, but the project declares {model.Config.Marks.ToProjectValue()}.", mark.File, mark.Line, subject));
                }
            }

            var counted = element.Marks
                .Where(mark => element.Kind == CodeKind.Technical
                    ? mark.Verb == MarkVerb.Implements || mark.Verb == MarkVerb.Follows
                    : mark.Verb != MarkVerb.Follows)
                .Select(mark => mark.RecordId)
                .ToList();
            var count = MarkRules.CheckCount(element.Kind, counted);

            if (count.Violated)
            {
                findings.Add(count.Duplicate is not null
                    ? new Finding("mark-count-invalid", $"{KindName(element.Kind)} '{DisplayName(element)}' names '{count.Duplicate}' twice.", element.File, element.Line, null)
                    : new Finding("mark-count-invalid", $"{KindName(element.Kind)} '{DisplayName(element)}' carries {counted.Count} marks, but its kind takes one.", element.File, element.Line, null));
            }
        }
    }

    /// <summary>
    ///     Whether the kind writes its marks in the style the project declares: the C#
    ///     elements. A frontend kind writes the typed constant whatever the project
    ///     declares, so its style is compared within its own language, never against
    ///     the declaration.
    /// </summary>
    private static bool UsesDeclaredStyle(CodeKind kind) =>
        kind is not (CodeKind.FrontendModule or CodeKind.FrontendRoute or CodeKind.BrowserTest);

    /// <summary>
    ///     Reports every backend mark naming a use case, reaction, job or aggregate outside
    ///     the module folder it sits in, and every handler or endpoint mark naming a use
    ///     case outside the use-case folder it sits in. A copied handler keeps its old mark;
    ///     the folder already says what the mark should be. A mark naming no record, a
    ///     record of another type, or a record its kind does not take is
    ///     <c>mark-unknown</c>, not drift.
    /// </summary>
    private static void CheckMarkFolderDrift(ProjectModel model, List<Finding> findings)
    {
        foreach (var element in model.Code.Elements)
        {
            if (element.InTests
                || element.Kind is CodeKind.Test or CodeKind.BrowserTest
                    or CodeKind.FrontendModule or CodeKind.FrontendRoute)
            {
                continue;
            }

            foreach (var mark in element.Marks)
            {
                if (mark.Verb == MarkVerb.Follows)
                {
                    continue;
                }

                if (!Identifiers.TrySplit(mark.RecordId, out var prefix, out var home, out var name)
                    || prefix is not ("use-case" or "reaction" or "job" or "aggregate")
                    || home is not { Length: > 0 })
                {
                    continue;
                }

                if (!model.ById.TryGetValue(mark.RecordId, out var record) || record.Type != prefix)
                {
                    continue;
                }

                if (!MarkRules.Accepts(element.Kind, mark.RecordId))
                {
                    continue;
                }

                if (ModuleSegments(model.Config, element.File) is not { } segments)
                {
                    continue;
                }

                if (!SameName(segments[0], home))
                {
                    findings.Add(new Finding("mark-folder-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in module folder '{segments[0]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
                    continue;
                }

                if (element.Kind is CodeKind.Handler or CodeKind.Endpoint
                    && prefix == "use-case"
                    && UseCaseFolderOf(segments) is { } folder
                    && !SameName(folder, name))
                {
                    findings.Add(new Finding("mark-folder-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{folder}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
                }
            }
        }
    }

    /// <summary>
    ///     The path segments of the file under its layer root, or null when the file sits
    ///     under no layer root, under the cross-module contracts part, under provider
    ///     plumbing, under the shared kernel, or in a layer root itself. A leading WebApi
    ///     <c>Endpoints</c> folder is skipped, so the first segment is the module folder
    ///     and the last is the file name.
    /// </summary>
    private static string[]? ModuleSegments(ConsumerConfig config, string file)
    {
        if (config.SharedKernel.Length > 0 && IsUnder(config.SharedKernel, file))
        {
            return null;
        }

        if (IsUnder(config.ApplicationRoot + ".Abstractions", file))
        {
            return null;
        }

        foreach (var root in new[] { config.ApplicationRoot, config.DomainRoot, config.WebApiRoot, config.InfrastructureRoot, config.WorkerRoot })
        {
            if (root.Length == 0 || !IsUnder(root, file))
            {
                continue;
            }

            var segments = Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (segments.Length < 2)
            {
                return null;
            }

            var first = 0;

            if (root == config.WebApiRoot && segments[0].Equals("Endpoints", StringComparison.OrdinalIgnoreCase))
            {
                first = 1;
            }

            if (root == config.InfrastructureRoot && segments[0].Equals("Persistence", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var rest = segments.Skip(first).ToArray();

            return rest.Length >= 2 ? rest : null;
        }

        return null;
    }

    /// <summary>
    ///     The use-case folder of a handler or endpoint: the third segment of module,
    ///     aggregate and use case, or the second of a flat module and use case. Null when
    ///     the file sits directly in its module folder, where no use-case folder names it.
    /// </summary>
    private static string? UseCaseFolderOf(string[] segments) =>
        segments.Length >= 4 ? segments[2] : segments.Length == 3 ? segments[1] : null;

    /// <summary>Whether a folder name and a record name spell one name: case, hyphens and underscores ignored.</summary>
    private static bool SameName(string folder, string name)
    {
        var left = folder.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        var right = name.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnder(string root, string file)
    {
        if (root.Length == 0)
        {
            return false;
        }

        var relative = Path.GetRelativePath(root, file);

        return !relative.StartsWith("..", StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    /// <summary>
    ///     Checks one <c>implements</c> or <c>proves</c> mark: it names a record of
    ///     the type its kind takes, in the verb its kind takes. A test proves the
    ///     lowest record it verifies: a scenario, a rule, a quality requirement, a
    ///     technical decision, an external system, an integration event, or a
    ///     provision of the carried release.
    /// </summary>
    private static void CheckServesMark(ProjectModel model, MarkedElement element, CodeMark mark, List<Finding> findings)
    {
        var subject = mark.RecordId.Length > 0 ? mark.RecordId : null;

        if (element.Kind == CodeKind.Test && mark.RecordId.StartsWith("litespec/", StringComparison.Ordinal))
        {
            CheckProvisionMark(model, element, mark, "marks", findings);
        }
        else if (!model.ById.TryGetValue(mark.RecordId, out var record))
        {
            findings.Add(new Finding("mark-unknown", $"{KindName(element.Kind)} '{DisplayName(element)}' marks '{mark.RecordId}', which matches no record.", mark.File, mark.Line, subject));
        }
        else if (!MarkRules.Accepts(element.Kind, mark.RecordId))
        {
            findings.Add(new Finding("mark-unknown", $"{KindName(element.Kind)} '{DisplayName(element)}' marks '{mark.RecordId}', which is a {record.Type} record, not a {ExpectedTypes(element.Kind)} record.", mark.File, mark.Line, subject));
        }
        else if (element.Kind == CodeKind.Test && record.Type == "requirement" && record.Field("kind") != "quality")
        {
            var kind = record.Field("kind");
            var what = kind is { Length: > 0 } ? $"a {kind} requirement" : "a requirement with no kind";

            findings.Add(new Finding("mark-unknown", $"{KindName(element.Kind)} '{DisplayName(element)}' marks '{mark.RecordId}', which is {what}, not a quality requirement.", mark.File, mark.Line, subject));
        }
        else if (element.Kind == CodeKind.Test && record.Type == "decision" && !UnderImplementationDecisions(model.Config, record.File))
        {
            findings.Add(new Finding("mark-unknown", $"{KindName(element.Kind)} '{DisplayName(element)}' marks '{mark.RecordId}', which is a product decision, not a technical decision.", mark.File, mark.Line, subject));
        }

        if (FrontendModuleMirror.PathMismatch(model, element, mark) is { } expected)
        {
            findings.Add(new Finding("mark-unknown", $"{KindName(element.Kind)} '{DisplayName(element)}' marks '{mark.RecordId}', but its folder mirrors '{expected}'.", mark.File, mark.Line, subject));
        }

        if (subject is not null && mark.Verb != MarkRules.Verb(element.Kind))
        {
            findings.Add(new Finding("mark-unknown", $"{KindName(element.Kind)} '{DisplayName(element)}' marks '{mark.RecordId}' with the {mark.Verb.ToTagName()} verb, but its kind takes {MarkRules.Verb(element.Kind).ToTagName()}.", mark.File, mark.Line, subject));
        }
    }

    /// <summary>
    ///     Checks one <c>follows</c> mark on an element of any kind: it names a
    ///     provision of the carried release on an applied page, or a decision or
    ///     exception that is not retired.
    /// </summary>
    private static void CheckFollowsMark(ProjectModel model, MarkedElement element, CodeMark mark, List<Finding> findings)
    {
        var name = $"{KindName(element.Kind)} '{DisplayName(element)}'";
        var id = mark.RecordId;

        if (id.StartsWith("litespec/", StringComparison.Ordinal))
        {
            CheckProvisionMark(model, element, mark, "follows", findings);

            return;
        }

        if (id.StartsWith("decision/", StringComparison.Ordinal) || id.StartsWith("exception/", StringComparison.Ordinal))
        {
            if (!model.ById.TryGetValue(id, out var record) || (record.Type != "decision" && record.Type != "exception"))
            {
                findings.Add(new Finding("mark-unknown", $"{name} follows '{id}', which matches no decision or exception.", mark.File, mark.Line, id));
            }
            else if (record.Status == "retired")
            {
                findings.Add(new Finding("mark-unknown", $"{name} follows '{id}', which is retired.", mark.File, mark.Line, null));
            }

            return;
        }

        var subject = id.Length > 0 ? id : null;

        if (!model.ById.TryGetValue(id, out var other))
        {
            findings.Add(new Finding("mark-unknown", $"{name} follows '{id}', which matches no provision, decision, or exception.", mark.File, mark.Line, subject));
        }
        else
        {
            findings.Add(new Finding("mark-unknown", $"{name} follows '{id}', which is a {other.Type} record, not a provision, decision, or exception.", mark.File, mark.Line, subject));
        }
    }

    /// <summary>
    ///     Checks one mark naming a provision: a <c>follows</c> mark on an element
    ///     of any kind, or a <c>proves</c> mark on a test. It names a provision of
    ///     the carried release, on a page the project's stacks and extensions apply.
    /// </summary>
    private static void CheckProvisionMark(ProjectModel model, MarkedElement element, CodeMark mark, string verb, List<Finding> findings)
    {
        var name = $"{KindName(element.Kind)} '{DisplayName(element)}'";
        var id = mark.RecordId;

        if (!Authoring.Provisions.IsProvisionId(id) || (model.Provisions.Count > 0 && !model.Provisions.Contains(id)))
        {
            findings.Add(new Finding("mark-unknown", $"{name} {verb} '{id}', which matches no provision of the carried release.", mark.File, mark.Line, id));
        }
        else if (model.AppliedPages.Count > 0
            && Authoring.Provisions.Parse(id) is { } parsed
            && !model.AppliedPages.Contains("docs/" + parsed.Page + ".md"))
        {
            findings.Add(new Finding("mark-unknown", $"{name} {verb} '{id}', which sits on a page the project's stacks and extensions do not apply.", mark.File, mark.Line, id));
        }
    }

    /// <summary>
    ///     Reports every mark on an element that takes no mark: a reading method, a
    ///     private helper, or an unrecognized class carrying a mark, or a proves mark
    ///     on a method with no test attribute. A <c>follows</c> mark is never stray.
    /// </summary>
    private static void CheckStrayMarks(ProjectModel model, List<Finding> findings)
    {
        foreach (var stray in model.Code.StrayMarks)
        {
            foreach (var mark in stray.Marks)
            {
                if (mark.Verb == MarkVerb.Follows)
                {
                    continue;
                }

                var subject = mark.RecordId.Length > 0 ? mark.RecordId : null;

                findings.Add(new Finding("mark-unknown", $"{StrayName(stray)} takes no mark, but marks '{mark.RecordId}'.", mark.File, mark.Line, subject));
            }
        }
    }

    /// <summary>
    ///     Reports every technical decision no code element follows: a decision under
    ///     the project's implementation decisions folder that no <c>follows</c> mark
    ///     names. Retired decisions need no follower.
    /// </summary>
    private static void CheckDecisionsFollowed(ProjectModel model, List<Finding> findings)
    {
        var followed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var element in model.Code.Elements)
        {
            foreach (var mark in element.Marks)
            {
                if (mark.Verb == MarkVerb.Follows && mark.RecordId.Length > 0)
                {
                    followed.Add(mark.RecordId);
                }
            }
        }

        foreach (var stray in model.Code.StrayMarks)
        {
            foreach (var mark in stray.Marks)
            {
                if (mark.Verb == MarkVerb.Follows && mark.RecordId.Length > 0)
                {
                    followed.Add(mark.RecordId);
                }
            }
        }

        foreach (var mark in Readers.Operations.OperationsMarkReader.Read(model.Config))
        {
            if (mark.Id.Length > 0)
            {
                followed.Add(mark.Id);
            }
        }

        foreach (var record in model.Records)
        {
            if (record.Type != "decision" || record.Status == "retired" || !UnderImplementationDecisions(model.Config, record.File))
            {
                continue;
            }

            if (!followed.Contains(record.Id))
            {
                findings.Add(new Finding("decision-unimplemented", $"Decision '{record.Id}' is followed by no code element.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Whether the file sits under the project's implementation decisions folder.</summary>
    internal static bool UnderImplementationDecisions(ConsumerConfig config, string file)
    {
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        foreach (var root in config.RecordRoots)
        {
            var folder = Path.Combine(root, "implementation", "decisions") + Path.DirectorySeparatorChar;

            if (file.StartsWith(folder, comparison))
            {
                return true;
            }
        }

        return false;
    }

    private static void CheckRuntimeValueDrift(ProjectModel model, List<Finding> findings)
    {
        foreach (var element in model.Code.Elements)
        {
            if (element.RuntimeValue is not { } actual)
            {
                continue;
            }

            if (element.Kind == CodeKind.DomainEvent)
            {
                var record = element.Marks
                    .Select(mark => model.ById.TryGetValue(mark.RecordId, out var found) ? found : null)
                    .FirstOrDefault(found => found?.Type == "event");

                if (record is null || actual == record.Id)
                {
                    continue;
                }

                findings.Add(new Finding("runtime-value-drift", $"Domain event '{element.Class}' declares Classification '{actual}', but its record states '{record.Id}'.", element.File, element.Line, SubjectOf(record)));
            }
            else if (element.Kind == CodeKind.RuleException)
            {
                var rule = element.Marks
                    .Select(mark => model.ById.TryGetValue(mark.RecordId, out var found) ? found : null)
                    .FirstOrDefault(found => found is { Type: "rule" } candidate && RefusalCodeOf(model, candidate) is not null);

                if (rule is null || actual == RefusalCodeOf(model, rule))
                {
                    continue;
                }

                findings.Add(new Finding("runtime-value-drift", $"Rule exception '{element.Class}' declares Code '{actual}', but its record states '{RefusalCodeOf(model, rule)}'.", element.File, element.Line, SubjectOf(rule)));
            }
        }
    }

    /// <summary>The refusal code of the rule through the shared rule over the model.</summary>
    private static string? RefusalCodeOf(ProjectModel model, Record rule) =>
        RefusalCode.Of(rule, id => model.ById.TryGetValue(id, out var result) ? result : null);

    private static void CheckUseCaseActCalled(ProjectModel model, List<Finding> findings)
    {
        var roots = model.Code.Elements
            .Where(element => element.Kind == CodeKind.AggregateRoot)
            .Select(element => element.Class)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var handler in model.Code.Handlers)
        {
            if (handler.Kind != "command" || handler.UseCaseId.Length == 0)
            {
                continue;
            }

            if (!model.ById.TryGetValue(handler.UseCaseId, out var useCase) || useCase.Type != "use-case")
            {
                continue;
            }

            if (!ChangesGuardedAggregate(model, useCase))
            {
                continue;
            }

            var called = (handler.AggregateCalls ?? [])
                .Concat(handler.FactoryCalls ?? [])
                .Any(call => ActCarries(model, roots, call, handler.UseCaseId));

            if (!called)
            {
                findings.Add(new Finding("use-case-act-missing", $"Use case '{handler.UseCaseId}' has no handler calling an aggregate method marked with the use case.", handler.File, handler.Line, handler.UseCaseId));
            }
        }
    }

    /// <summary>
    ///     Whether the use case changes an entity or saga aggregate: one
    ///     <c>changes</c> entry naming an aggregate whose kind is not
    ///     <c>store</c>. An aggregate with no declared kind is an entity; a
    ///     pass running other commands lists nothing in <c>changes</c>.
    /// </summary>
    private static bool ChangesGuardedAggregate(ProjectModel model, Record useCase) =>
        useCase.Ids("changes").Any(id =>
            !model.ById.TryGetValue(id, out var changed)
            || changed.Type != "aggregate"
            || changed.Field("kind") is not "store");

    /// <summary>
    ///     Whether an aggregate call reaches the use case's act: the called method name
    ///     on the aggregate it calls carries the use case. A receiver naming an aggregate
    ///     type is a static call on that root; any other receiver is a held root whose
    ///     type the call does not name, so the method match stands on its own.
    /// </summary>
    private static bool ActCarries(ProjectModel model, HashSet<string> roots, CodeCall call, string useCase)
    {
        foreach (var method in model.Code.AggregateMethods)
        {
            if (method.Method != call.Method
                || !method.UseCases.Contains(useCase, StringComparer.Ordinal))
            {
                continue;
            }

            if (!roots.Contains(call.Receiver) || method.Aggregate == call.Receiver)
            {
                return true;
            }
        }

        return false;
    }

    private static void CheckActEvents(ProjectModel model, List<Finding> findings)
    {
        var eventsByClass = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var domainEvent in model.Code.Events)
        {
            if (domainEvent.EventId is not null)
            {
                eventsByClass.TryAdd(domainEvent.Class, domainEvent.EventId);
            }
        }

        foreach (var useCase in model.Records)
        {
            if (useCase.Type != "use-case")
            {
                continue;
            }

            var marked = model.Code.AggregateMethods
                .Where(method => method.UseCases.Contains(useCase.Id, StringComparer.Ordinal))
                .ToList();
            var methods = marked
                .Select(method => new ActMethodRaises(RaisedIds(eventsByClass, method)))
                .ToList();

            foreach (var decided in ActEvents.Check(useCase.Ids("raises"), methods))
            {
                var method = marked[decided.Method];

                if (decided.Code == "act-event-undeclared")
                {
                    findings.Add(new Finding("act-event-undeclared", $"Use case '{useCase.Id}' lists no '{decided.EventId}' in raises, but aggregate method '{method.Aggregate}.{method.Method}' marked with the use case raises it.", method.File, method.Line, useCase.Id));
                }
                else
                {
                    findings.Add(new Finding("act-event-unimplemented", $"Use case '{useCase.Id}' lists '{decided.EventId}' in raises, which no method marked with the use case raises.", method.File, method.Line, SubjectOf(useCase)));
                }
            }
        }
    }

    /// <summary>The event ids the method raises: its raised classes resolved through marked events.</summary>
    private static IReadOnlyList<string> RaisedIds(Dictionary<string, string> eventsByClass, AggregateMethod method)
    {
        var ids = new List<string>();

        foreach (var raisedClass in method.Events)
        {
            if (eventsByClass.TryGetValue(raisedClass, out var id) && !ids.Contains(id, StringComparer.Ordinal))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private static void CheckRecordsImplemented(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            var code = record.Type switch
            {
                "aggregate" => "aggregate-unimplemented",
                "reaction" => "reaction-unimplemented",
                "job" => "job-unimplemented",
                "external-system" => "external-system-unimplemented",
                _ => null,
            };

            if (code is null)
            {
                continue;
            }

            if (record.Type == "aggregate" && record.Field("kind") == "store")
            {
                continue;
            }

            var kind = record.Type switch
            {
                "aggregate" => CodeKind.AggregateRoot,
                "reaction" => CodeKind.EventHandler,
                "job" => CodeKind.Job,
                _ => CodeKind.Port,
            };
            var label = record.Type switch
            {
                "aggregate" => "Aggregate",
                "reaction" => "Reaction",
                "job" => "Job",
                _ => "External system",
            };
            var element = record.Type switch
            {
                "aggregate" => "aggregate root",
                "reaction" => "event handler",
                "job" => "job host",
                _ => "port",
            };

            if (!model.Code.Elements.Any(candidate => candidate.Kind == kind && candidate.Marks.Any(mark => mark.Verb != MarkVerb.Follows && mark.RecordId == record.Id)))
            {
                findings.Add(new Finding(code, $"{label} '{record.Id}' has no {element} carrying its mark.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    private static void CheckUseCaseExposed(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records)
        {
            if (useCase.Type != "use-case")
            {
                continue;
            }

            var entry = useCase.Ids("reachedThrough").FirstOrDefault(id =>
                model.ById.TryGetValue(id, out var point)
                && point.Type == "entry-point"
                && point.Field("kind") is "browser" or "api");

            if (entry is null
                || model.Code.Elements.Any(element => element.Kind == CodeKind.Endpoint && element.Marks.Any(mark => mark.Verb != MarkVerb.Follows && mark.RecordId == useCase.Id)))
            {
                continue;
            }

            findings.Add(new Finding("use-case-endpoint-missing", $"Use case '{useCase.Id}' is reached through '{entry}', but no endpoint carries its mark.", useCase.File, useCase.Line, SubjectOf(useCase)));
        }
    }

    private static void CheckEffectsImplemented(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Type is not ("use-case" or "reaction" or "job"))
            {
                continue;
            }

            var edges = (record.Type == "use-case" ? CodeChecks.UseCaseEdges(record) : CodeChecks.HolderEdges(record))
                .Distinct()
                .ToList();

            if (edges.Count == 0)
            {
                continue;
            }

            var dispatches = new List<CodeDispatch>();

            if (record.Type == "use-case")
            {
                dispatches.AddRange(model.Code.Handlers
                    .Where(handler => handler.UseCaseId == record.Id)
                    .SelectMany(handler => handler.Dispatches ?? []));
                dispatches.AddRange(CodeChecks.Hosts(model)
                    .Where(host => host.UseCaseId == record.Id)
                    .SelectMany(host => host.Dispatches ?? []));
            }
            else
            {
                dispatches.AddRange(CodeChecks.Hosts(model)
                    .Where(host => host.RecordId == record.Id)
                    .SelectMany(host => host.Dispatches ?? []));
            }

            var label = record.Type switch
            {
                "use-case" => "Use case",
                "reaction" => "Reaction",
                _ => "Job",
            };

            foreach (var edge in edges)
            {
                if (!dispatches.Any(dispatch => CodeChecks.EdgeDeclared(model, [edge], dispatch)))
                {
                    findings.Add(new Finding("effect-unimplemented", $"{label} '{record.Id}' declares '{edge.Verb} {edge.Target}', which no code takes.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

    private static void CheckUseCaseReached(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records)
        {
            if (useCase.Type != "use-case")
            {
                continue;
            }

            var entries = useCase.Ids("reachedThrough").Where(id =>
                model.ById.TryGetValue(id, out var point)
                && point.Type == "entry-point"
                && point.Field("kind") is "browser" or "device").ToList();

            foreach (var entry in entries)
            {
                // Per frontend: the frontends serving this entry point are those
                // holding a route for a screen of it. When none is known, the
                // project-wide screen set decides, as before.
                var serving = model.Config.Frontends
                    .Where(frontend => model.Screens.Any(screen =>
                        screen.Frontend == frontend.Name
                        && screen.ScreenId is { } screenId
                        && model.ById.TryGetValue(screenId, out var screenRecord)
                        && screenRecord.Ids("entryPoint").Contains(entry, StringComparer.Ordinal)))
                    .ToList();

                if (serving.Count == 0)
                {
                    if (!model.Screens.Any(screen => screen.UseCases.Contains(useCase.Id, StringComparer.Ordinal))
                        && !ScreenRecords(model).Any(screen =>
                            screen.Ids("entryPoint").Contains(entry, StringComparer.Ordinal)
                            && screen.Ids("offers").Contains(useCase.Id, StringComparer.Ordinal)))
                    {
                        findings.Add(new Finding("use-case-unreached", $"Use case '{useCase.Id}' is reached through '{entry}', but no screen calls it.", useCase.File, useCase.Line, SubjectOf(useCase)));
                    }

                    continue;
                }

                foreach (var frontend in serving)
                {
                    var reached = model.Screens
                        .Where(screen => screen.Frontend == frontend.Name)
                        .Any(screen => screen.UseCases.Contains(useCase.Id, StringComparer.Ordinal)
                            || (screen.ScreenId is { } screenId
                                && model.ById.TryGetValue(screenId, out var screenRecord)
                                && screenRecord.Ids("offers").Contains(useCase.Id, StringComparer.Ordinal)));

                    if (!reached)
                    {
                        findings.Add(new Finding("use-case-unreached", $"Use case '{useCase.Id}' is reached through '{entry}', but no screen calls it.", useCase.File, useCase.Line, SubjectOf(useCase)));
                    }
                }
            }
        }
    }

    /// <summary>The kind name a mark finding shows: the code element in plain words.</summary>
    private static string KindName(CodeKind kind) => kind switch
    {
        CodeKind.AggregateRoot => "Aggregate root",
        CodeKind.AggregateMethod => "Aggregate method",
        CodeKind.DomainEvent => "Domain event",
        CodeKind.RuleException => "Rule exception",
        CodeKind.Handler => "Handler",
        CodeKind.EventHandler => "Event handler",
        CodeKind.Job => "Job",
        CodeKind.Port => "Port",
        CodeKind.Adapter => "Adapter",
        CodeKind.Endpoint => "Endpoint",
        CodeKind.FrontendModule => "Frontend module",
        CodeKind.FrontendRoute => "Route entry",
        CodeKind.BrowserTest => "Browser test",
        CodeKind.Test => "Test",
        CodeKind.DomainService => "Domain service",
        CodeKind.Technical => "Technical class",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown code kind."),
    };

    /// <summary>The element a mark finding names: its type, or its type and member.</summary>
    private static string DisplayName(MarkedElement element) =>
        element.Member is { Length: > 0 } member ? $"{element.Class}.{member}" : element.Class;

    /// <summary>The stray element a mark finding names: its method, or its type.</summary>
    private static string StrayName(StrayMark stray) =>
        stray.Member is { Length: > 0 } member ? $"Method '{stray.Class}.{member}'" : $"Type '{stray.Class}'";

    /// <summary>The record types a mark on the kind may name, in plain words.</summary>
    private static string ExpectedTypes(CodeKind kind) =>
        string.Join(" or ", MarkRules.RecordTypes(kind));
}
