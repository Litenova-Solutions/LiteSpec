using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>
///     The S-tier checks that read code: a write outside the declared aggregates, a refusal
///     code the code produces that no result declares, and an edge the code takes that no
///     record declares.
/// </summary>
static partial class Checker
{
    /// <summary>Runs the code-reading checks.</summary>
    private static void CheckCode(ProjectModel model, List<Finding> findings) =>
        CodeChecks.Run(model, findings);
}

/// <summary>
///     The code-reading checks, kept in one class so the partial checker holds only the entry point.
/// </summary>
static class CodeChecks
{
    /// <summary>Runs write-undeclared, result-undeclared, effect-undeclared and the handler shape checks.</summary>
    public static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckWriteUndeclared(model, findings);
        CheckResultUndeclared(model, findings);
        CheckEffectUndeclared(model, findings);
        CheckUseCaseVerbMethod(model, findings);
        CheckEventTenseMismatch(model, findings);
        CheckNameDriftAcrossLayers(model, findings);
        CheckOperationIdDuplicate(model, findings);
        CheckRouteShape(model, findings);
        CheckReasonlessName(model, findings);
        CheckCollaboratorHoldsRepository(model, findings);
        CheckRepositoryReadInLoop(model, findings);
        CheckQueryFiltersInMemory(model, findings);
        CheckDependencyCount(model, findings);
        CheckEffectBeforeCommit(model, findings);
    }

    /// <summary>
    ///     One S-tier finding. The tier comes from the finding code through Finding.Tiers,
    ///     which maps these three codes to S, so this helper builds the finding as written.
    /// </summary>
    private static Finding CodeFinding(string code, string message, string path, int line, string? subject) =>
        new(code, message, path, line, subject);

    private static void CheckWriteUndeclared(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.Repositories is not { Count: > 0 }
                || !model.ById.TryGetValue(handler.UseCaseId, out var useCase)
                || useCase.Type != "use-case")
            {
                continue;
            }

            var declared = new HashSet<string>(useCase.Ids("changes"), StringComparer.Ordinal);
            declared.UnionWith(useCase.Ids("reads"));

            foreach (var repository in handler.Repositories)
            {
                if (!RepositoryDeclared(model, repository.Interface, declared))
                {
                    findings.Add(CodeFinding(
                        "write-undeclared",
                        $"Handler for '{handler.UseCaseId}' takes repository '{repository.Interface}', whose aggregate is outside its changes and reads.",
                        repository.File,
                        repository.Line,
                        handler.UseCaseId));
                }
            }
        }

        foreach (var reaction in model.Code.Reactions)
        {
            if (reaction.Repositories is not { Count: > 0 }
                || reaction.RecordId is null
                || !model.ById.TryGetValue(reaction.RecordId, out var record)
                || record.Type is not ("reaction" or "job"))
            {
                continue;
            }

            var declared = new HashSet<string>(record.Ids("reads"), StringComparer.Ordinal);
            var kind = record.Type == "job" ? "Job" : "Reaction";

            foreach (var repository in reaction.Repositories)
            {
                if (!RepositoryDeclared(model, repository.Interface, declared))
                {
                    findings.Add(CodeFinding(
                        "write-undeclared",
                        $"{kind} '{record.Id}' takes repository '{repository.Interface}', whose aggregate is outside its reads.",
                        repository.File,
                        repository.Line,
                        record.Id));
                }
            }
        }
    }

    /// <summary>Whether the repository interface serves an aggregate the declared ids hold.</summary>
    private static bool RepositoryDeclared(ProjectModel model, string face, HashSet<string> declared)
    {
        if (RepositoryAggregate(face) is not { } stem)
        {
            return true;
        }

        foreach (var record in model.Records)
        {
            if (record.Type != "aggregate")
            {
                continue;
            }

            if (Identifiers.TrySplit(record.Id, out _, out _, out var name)
                && name == stem
                && declared.Contains(record.Id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The aggregate name a repository interface serves, in kebab-case, or null when it names none.</summary>
    private static string? RepositoryAggregate(string face)
    {
        var type = face;
        var generic = type.IndexOf('<');

        // A generic repository serves its first type argument.
        if (generic >= 0)
        {
            var args = type[(generic + 1)..].TrimEnd('>');
            type = args.Split(',')[0].Trim();

            var dot = type.LastIndexOf('.');

            if (dot >= 0)
            {
                type = type[(dot + 1)..];
            }

            return type.Length > 0 ? Naming.Kebab(type) : null;
        }

        if (type.StartsWith("I", StringComparison.Ordinal) && type.Length > 1 && char.IsUpper(type[1]))
        {
            type = type[1..];
        }

        if (!type.EndsWith("Repository", StringComparison.Ordinal) || type.Length <= "Repository".Length)
        {
            return null;
        }

        return Naming.Kebab(type[..^"Repository".Length]);
    }

    private static void CheckResultUndeclared(ProjectModel model, List<Finding> findings)
    {
        var refusals = GenericRefusals(model);
        var declaring = DeclaringCodes(model);

        foreach (var handler in model.Code.Handlers)
        {
            if (!model.ById.TryGetValue(handler.UseCaseId, out var useCase) || useCase.Type != "use-case")
            {
                continue;
            }

            var declared = DeclaredCodes(model, useCase, refusals);

            foreach (var (code, file, line) in ProducedCodes(handler.Refusals, handler.Thrown, declaring))
            {
                if (!declared.Contains(code))
                {
                    findings.Add(CodeFinding(
                        "result-undeclared",
                        $"Handler for '{handler.UseCaseId}' produces '{code}', which no result of the use case declares.",
                        file,
                        line,
                        handler.UseCaseId));
                }
            }
        }

        foreach (var reaction in model.Code.Reactions)
        {
            if (reaction.RecordId is null
                || !model.ById.TryGetValue(reaction.RecordId, out var record)
                || record.Type is not ("reaction" or "job"))
            {
                continue;
            }

            var issued = IssuedUseCases(model, record);

            if (issued.Count == 0)
            {
                continue;
            }

            var declared = new HashSet<string>(StringComparer.Ordinal);

            foreach (var useCase in issued)
            {
                declared.UnionWith(DeclaredCodes(model, useCase, refusals));
            }

            var holder = record.Type == "job" ? "its issued use cases" : "the issued use case";

            foreach (var (code, file, line) in ProducedCodes(reaction.Refusals, reaction.Thrown, declaring))
            {
                if (!declared.Contains(code))
                {
                    findings.Add(CodeFinding(
                        "result-undeclared",
                        $"{(record.Type == "job" ? "Job" : "Reaction")} '{record.Id}' produces '{code}', which {holder} declare none of.",
                        file,
                        line,
                        record.Id));
                }
            }
        }
    }

    /// <summary>
    ///     Every refusal code a use case declares: its written refusal results plus the derived
    ///     generic codes, which a permission grant and a validated input each derive.
    /// </summary>
    private static HashSet<string> DeclaredCodes(
        ProjectModel model, Record useCase, (string Forbidden, string InvalidInput) refusals)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);

        if (Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
        {
            foreach (var result in model.Records)
            {
                if (result.Type != "result" || result.Derived || result.Field("outcome") != "refusal")
                {
                    continue;
                }

                if (result.Field("code") is { } code
                    && Identifiers.TrySplit(result.Id, out _, out var home, out _)
                    && home == name)
                {
                    declared.Add(code);
                }
            }
        }

        if (ModelBuilder.GrantedPermission(useCase) is not null)
        {
            declared.Add(refusals.Forbidden);
        }

        if (ModelBuilder.HasValidatedInput(useCase))
        {
            declared.Add(refusals.InvalidInput);
        }

        return declared;
    }

    /// <summary>The generic refusal codes from litespec.project.json refusals, or the defaults.</summary>
    private static (string Forbidden, string InvalidInput) GenericRefusals(ProjectModel model)
    {
        const string forbidden = "authorization.forbidden";
        const string invalidInput = "request.validation-failed";

        try
        {
            var file = Path.Combine(model.Config.Root, "litespec.project.json");

            if (!File.Exists(file))
            {
                return (forbidden, invalidInput);
            }

            using var document = JsonDocument.Parse(File.ReadAllBytes(file));

            if (!document.RootElement.TryGetProperty("refusals", out var refusals)
                || refusals.ValueKind != JsonValueKind.Object)
            {
                return (forbidden, invalidInput);
            }

            return (
                refusals.TryGetProperty("forbidden", out var denied)
                    && denied.ValueKind == JsonValueKind.String
                    && denied.GetString() is { Length: > 0 } customForbidden
                        ? customForbidden
                        : forbidden,
                refusals.TryGetProperty("invalidInput", out var invalid)
                    && invalid.ValueKind == JsonValueKind.String
                    && invalid.GetString() is { Length: > 0 } customInvalid
                        ? customInvalid
                        : invalidInput);
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return (forbidden, invalidInput);
        }
    }

    /// <summary>Refusal codes by the class whose declaring literal carries them.</summary>
    private static Dictionary<string, HashSet<string>> DeclaringCodes(ProjectModel model)
    {
        var declaring = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var literal in model.Code.Literals)
        {
            if (!literal.Declares || literal.Class is null)
            {
                continue;
            }

            if (!declaring.TryGetValue(literal.Class, out var codes))
            {
                codes = new HashSet<string>(StringComparer.Ordinal);
                declaring[literal.Class] = codes;
            }

            codes.Add(literal.Value);
        }

        return declaring;
    }

    /// <summary>
    ///     Every refusal code a class produces: its declaring literals plus the codes of the
    ///     exception and failure types it constructs, earliest site first.
    /// </summary>
    private static List<(string Code, string File, int Line)> ProducedCodes(
        IReadOnlyList<CodeRefusal>? refusals,
        IReadOnlyList<CodeThrown>? thrown,
        Dictionary<string, HashSet<string>> declaring)
    {
        var produced = new Dictionary<string, (string File, int Line)>(StringComparer.Ordinal);

        if (refusals is not null)
        {
            foreach (var refusal in refusals)
            {
                produced.TryAdd(refusal.Code, (refusal.File, refusal.Line));
            }
        }

        if (thrown is not null)
        {
            foreach (var construction in thrown)
            {
                if (!declaring.TryGetValue(construction.Type, out var codes))
                {
                    continue;
                }

                foreach (var code in codes.Order(StringComparer.Ordinal))
                {
                    produced.TryAdd(code, (construction.File, construction.Line));
                }
            }
        }

        return [.. produced
            .OrderBy(entry => entry.Value.Line)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => (entry.Key, entry.Value.File, entry.Value.Line))];
    }

    /// <summary>The use cases a reaction or job issues.</summary>
    private static List<Record> IssuedUseCases(ProjectModel model, Record record)
    {
        var found = new List<Record>();

        foreach (var id in record.EdgeIds("issues"))
        {
            if (model.ById.TryGetValue(id, out var useCase) && useCase.Type == "use-case")
            {
                found.Add(useCase);
            }
        }

        return found;
    }

    private static void CheckEffectUndeclared(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.Dispatches is not { Count: > 0 }
                || !model.ById.TryGetValue(handler.UseCaseId, out var useCase)
                || useCase.Type != "use-case")
            {
                continue;
            }

            var edges = UseCaseEdges(useCase);

            foreach (var dispatch in handler.Dispatches)
            {
                if (!EdgeDeclared(model, edges, dispatch))
                {
                    findings.Add(CodeFinding(
                        "effect-undeclared",
                        $"Handler for '{handler.UseCaseId}' {dispatch.Verb} '{DispatchTarget(dispatch)}'{ViaText(dispatch)}, which no effects entry of the use case declares.",
                        dispatch.File,
                        dispatch.Line,
                        handler.UseCaseId));
                }
            }
        }

        foreach (var reaction in model.Code.Reactions)
        {
            if (reaction.Dispatches is not { Count: > 0 }
                || (reaction.RecordId ?? reaction.UseCaseId) is not { } holderId
                || !model.ById.TryGetValue(holderId, out var record)
                || record.Type is not ("reaction" or "job" or "use-case"))
            {
                continue;
            }

            // A reaction inside a use-case folder with no tag of its own belongs to that use case,
            // so its dispatches answer to the use case's effects.
            // Issuing the use case it sits with is the reaction's own trigger, not an edge.
            var edges = record.Type == "use-case"
                ? [.. UseCaseEdges(record), ("issues", record.Id)]
                : HolderEdges(record);
            var kind = record.Type == "job" ? "Job" : "Reaction";

            foreach (var dispatch in reaction.Dispatches)
            {
                if (!EdgeDeclared(model, edges, dispatch))
                {
                    findings.Add(CodeFinding(
                        "effect-undeclared",
                        $"{kind} '{record.Id}' {dispatch.Verb} '{DispatchTarget(dispatch)}'{ViaText(dispatch)}, which no declared edge of the {record.Type} names.",
                        dispatch.File,
                        dispatch.Line,
                        record.Id));
                }
            }
        }
    }

    /// <summary>One dispatch's target as the message reads it: the port and method for calls, else the message.</summary>
    private static string DispatchTarget(CodeDispatch dispatch) =>
        dispatch.Verb == "calls" ? $"{dispatch.Detail}.{dispatch.Target}" : dispatch.Target;

    /// <summary>The helper a one-hop attribution found the dispatch through, or nothing when direct.</summary>
    private static string ViaText(CodeDispatch dispatch) =>
        dispatch.Via is { Length: > 0 } via ? $" via '{via}'" : "";

    /// <summary>Every edge a use case declares in its effects entries.</summary>
    internal static List<(string Verb, string Target)> UseCaseEdges(Record useCase)
    {
        var edges = new List<(string Verb, string Target)>();

        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty("effects", out var effects)
            || effects.ValueKind != JsonValueKind.Array)
        {
            return edges;
        }

        foreach (var entry in effects.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var verb in new[] { "issues", "sends", "publishes", "calls" })
            {
                if (entry.TryGetProperty(verb, out var target)
                    && target.ValueKind == JsonValueKind.String
                    && target.GetString() is { Length: > 0 } id)
                {
                    edges.Add((verb, id));
                }
            }
        }

        return edges;
    }

    /// <summary>Every edge a reaction or job declares: its issues, and a reaction's sends.</summary>
    internal static List<(string Verb, string Target)> HolderEdges(Record record)
    {
        var edges = new List<(string Verb, string Target)>();

        foreach (var id in record.EdgeIds("issues"))
        {
            edges.Add(("issues", id));
        }

        // A reaction sends, publishes and calls as well; a job only issues.
        if (record.Type == "reaction")
        {
            foreach (var verb in new[] { "sends", "publishes", "calls" })
            {
                foreach (var id in record.EdgeIds(verb))
                {
                    edges.Add((verb, id));
                }
            }
        }

        return edges;
    }

    /// <summary>Whether a declared edge of the same verb names the dispatch's target.</summary>
    internal static bool EdgeDeclared(ProjectModel model, List<(string Verb, string Target)> edges, CodeDispatch dispatch) =>
        dispatch.Verb switch
        {
            "issues" => ResolveMessageUseCases(model, dispatch.Target)
                .Any(id => edges.Any(edge => edge.Verb == "issues" && edge.Target == id)),
            "publishes" => edges.Any(edge => edge.Verb == "publishes" && MessageMatches(dispatch.Target, edge.Target)),
            "sends" => edges.Any(edge => edge.Verb == "sends" && NotificationMatches(model, dispatch, edge.Target)),
            "calls" => edges.Any(edge => edge.Verb == "calls" && OperationMatches(dispatch.Target, edge.Target)),
            _ => true,
        };

    /// <summary>The use cases a dispatched message names, by its operation stem, in identifier order.</summary>
    private static List<string> ResolveMessageUseCases(ProjectModel model, string message)
    {
        var stems = new HashSet<string>(StringComparer.Ordinal) { Naming.Kebab(message) };

        foreach (var suffix in new[] { "Command", "Query" })
        {
            if (message.EndsWith(suffix, StringComparison.Ordinal) && message.Length > suffix.Length)
            {
                stems.Add(Naming.Kebab(message[..^suffix.Length]));
            }
        }

        var found = new List<string>();

        foreach (var record in model.Records)
        {
            if (record.Type != "use-case")
            {
                continue;
            }

            if (Identifiers.TrySplit(record.Id, out _, out _, out var operation) && stems.Contains(operation))
            {
                found.Add(record.Id);
            }
        }

        found.Sort(StringComparer.Ordinal);

        return found;
    }

    /// <summary>
    ///     Whether a dispatched message names a declared event edge: the message stem meets the
    ///     identifier after its slash, or its last segment, with dots reading as dashes.
    /// </summary>
    private static bool MessageMatches(string message, string id)
    {
        var stem = message;

        foreach (var suffix in new[] { "IntegrationEvent", "Notification", "Event", "Message" })
        {
            if (stem.EndsWith(suffix, StringComparison.Ordinal) && stem.Length > suffix.Length)
            {
                stem = stem[..^suffix.Length];
                break;
            }
        }

        var kebab = Naming.Kebab(stem);
        var rest = id.Contains('/') ? id[(id.IndexOf('/') + 1)..] : id;
        var normalized = rest.Replace('.', '-');
        var last = rest.Contains('.') ? rest[(rest.LastIndexOf('.') + 1)..] : rest;

        return kebab == normalized || kebab == last;
    }

    /// <summary>
    ///     Whether a dispatched send names a declared notification: a template key meets the
    ///     notification's template, and a message type meets its identifier.
    /// </summary>
    private static bool NotificationMatches(ProjectModel model, CodeDispatch dispatch, string id)
    {
        if (dispatch.Detail == "template")
        {
            return model.ById.TryGetValue(id, out var notification)
                && notification.Type == "notification"
                && notification.Field("template") is { } template
                && template.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                           .Contains(dispatch.Target, StringComparer.Ordinal);
        }

        return MessageMatches(dispatch.Target, id);
    }

    /// <summary>Whether a port call names a declared operation: the method, without Async, meets the fragment.</summary>
    private static bool OperationMatches(string method, string id)
    {
        var hash = id.IndexOf('#');

        if (hash < 0 || hash == id.Length - 1)
        {
            return false;
        }

        var operation = id[(hash + 1)..];
        var name = method;

        if (name.EndsWith("Async", StringComparison.Ordinal) && name.Length > "Async".Length)
        {
            name = name[..^"Async".Length];
        }

        return Naming.Kebab(name) == operation;
    }

    private static void CheckUseCaseVerbMethod(ProjectModel model, List<Finding> findings)
    {
        // Only a method that raises a fact performs the act; a read or a key helper names none.
        var raising = model.Code.AggregateMethods
            .Where(method => method.Events.Count > 0)
            .Select(method => method.Method)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var handler in model.Code.Handlers)
        {
            var calls = handler.AggregateCalls?.Where(call => raising.Contains(call.Method)).ToList() ?? [];

            if (handler.Kind != "command" || calls.Count == 0)
            {
                continue;
            }

            var verb = UseCaseVerbs.VerbOf(handler.Name, model.Config.PhrasalVerbs);
            var matches = calls.Any(call =>
            {
                var kebab = Naming.Kebab(call.Method);

                // A command that records a request for work finished after the commit performs the request.
                return kebab == verb
                    || kebab.StartsWith(verb + "-", StringComparison.Ordinal)
                    || kebab.StartsWith("request-", StringComparison.Ordinal);
            });

            if (!matches)
            {
                var first = calls[0];
                var called = string.Join(", ", calls.Select(call => call.Method).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));

                findings.Add(CodeFinding(
                    "use-case-verb-method",
                    $"Handler for '{handler.UseCaseId}' calls '{called}' on its aggregate, none starting with the use-case verb '{verb}'.",
                    first.File,
                    first.Line,
                    handler.UseCaseId));
            }
        }
    }

    private static void CheckEventTenseMismatch(ProjectModel model, List<Finding> findings)
    {
        foreach (var method in model.Code.AggregateMethods)
        {
            if (method.Events.Count == 0)
            {
                continue;
            }

            var kebab = Naming.Kebab(method.Method);
            var verb = UseCaseVerbs.VerbOf(kebab, model.Config.PhrasalVerbs);

            // A record method stores a fact from outside, and its event names that fact rather than the act of recording.
            if (verb == "record")
            {
                continue;
            }

            // A mark method sets a state, so its event names that state rather than the act of marking.
            var past = verb == "mark" && kebab.StartsWith("mark-", StringComparison.Ordinal)
                ? kebab["mark-".Length..]
                : UseCaseVocabulary.PastTenseOf(verb, model.Config.PastTense);

            bool NamesThePastTense(string raised)
            {
                var stem = raised.EndsWith("Event", StringComparison.Ordinal) && raised.Length > "Event".Length
                    ? raised[..^"Event".Length]
                    : raised;
                var stemKebab = Naming.Kebab(stem);

                return stemKebab.EndsWith(past, StringComparison.Ordinal) || HasPastTenseComplement(stemKebab, past);
            }

            // The method's own fact is enough; a further event it raises names a consequence, such as
            // the registrations a changed window archives.
            if (method.Events.Any(NamesThePastTense))
            {
                continue;
            }

            foreach (var raised in method.Events.Order(StringComparer.Ordinal))
            {
                findings.Add(CodeFinding(
                    "event-tense-mismatch",
                    $"Aggregate method '{method.Aggregate}.{method.Method}' raises '{raised}', which does not end with the past tense '{past}' of its verb.",
                    method.File,
                    method.Line,
                    null));
            }
        }
    }

    /// <summary>
    ///     The prepositions and particles that open an event-name complement after the past tense.
    /// </summary>
    private static readonly string[] TenseComplementOpeners =
    [
        "to", "from", "by", "for", "on", "off", "in", "into", "out", "outside",
        "at", "with", "over", "up", "down", "back", "after", "before", "as",
    ];

    /// <summary>
    ///     Whether the stem names the past tense as a whole segment followed only by a complement
    ///     opening with a preposition or particle, such as <c>assigned-to-gate</c>.
    /// </summary>
    private static bool HasPastTenseComplement(string stemKebab, string past)
    {
        var segments = stemKebab.Split('-', StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index + 1 < segments.Length; index++)
        {
            if (segments[index] == past
                && TenseComplementOpeners.Contains(segments[index + 1], StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void CheckNameDriftAcrossLayers(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.MessageType.Length > 0)
            {
                var stem = handler.MessageType;

                foreach (var suffix in new[] { "Command", "Query" })
                {
                    if (stem.EndsWith(suffix, StringComparison.Ordinal) && stem.Length > suffix.Length)
                    {
                        stem = stem[..^suffix.Length];
                        break;
                    }
                }

                if (Naming.Kebab(stem) != handler.Name)
                {
                    findings.Add(CodeFinding(
                        "name-drift-across-layers",
                        $"Handler for '{handler.UseCaseId}' handles message '{handler.MessageType}', which does not match the use-case name.",
                        handler.File,
                        handler.Line,
                        handler.UseCaseId));
                }
            }

            // A use case served on several routes needs a distinct operation id per route, so one of
            // them carrying the use-case name, or every one opening with it, is enough.
            var operations = model.Code.Endpoints
                .Where(endpoint => endpoint.UseCaseId == handler.UseCaseId && endpoint.Operation.Length > 0)
                .ToList();
            var oneNamed = operations.Count > 1
                && (operations.Any(endpoint => Naming.Kebab(endpoint.Operation) == handler.Name)
                    || operations.All(endpoint => Naming.Kebab(endpoint.Operation).StartsWith(handler.Name + "-", StringComparison.Ordinal)));

            foreach (var endpoint in operations)
            {
                if (!oneNamed && Naming.Kebab(endpoint.Operation) != handler.Name)
                {
                    findings.Add(CodeFinding(
                        "name-drift-across-layers",
                        $"Endpoint for '{handler.UseCaseId}' declares operation id '{endpoint.Operation}', which does not match the use-case name.",
                        endpoint.File,
                        endpoint.Line,
                        handler.UseCaseId));
                }
            }

            foreach (var action in model.Code.AuditActions)
            {
                if (Naming.Kebab(action.Operation) == handler.Name && !NamesUseCase(action.Action, handler))
                {
                    findings.Add(CodeFinding(
                        "name-drift-across-layers",
                        $"Command '{handler.UseCaseId}' declares audit action '{action.Action}', which does not match the use-case name.",
                        action.File,
                        action.Line,
                        handler.UseCaseId));
                }
            }
        }
    }

    /// <summary>Whether an audit action names the use case: its full identifier, its dotted name, or its bare name.</summary>
    private static bool NamesUseCase(string action, HandlerInfo handler)
    {
        var bare = action.StartsWith("use-case/", StringComparison.Ordinal) ? action["use-case/".Length..] : action;

        return action == handler.UseCaseId
            || bare == handler.UseCaseId["use-case/".Length..]
            || Naming.Kebab(bare) == handler.Name;
    }

    /// <summary>Reports two endpoints declaring one operation id; the host refuses to start with both.</summary>
    private static void CheckOperationIdDuplicate(ProjectModel model, List<Finding> findings)
    {
        var groups = model.Code.Endpoints
            .Where(endpoint => endpoint.Operation.Length > 0)
            .GroupBy(endpoint => endpoint.Operation, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        foreach (var group in groups)
        {
            foreach (var endpoint in group.Skip(1))
            {
                findings.Add(CodeFinding(
                    "operation-id-duplicate",
                    $"Operation id '{group.Key}' on '{endpoint.Method} {endpoint.Route}' is declared by another endpoint too.",
                    endpoint.File,
                    endpoint.Line,
                    endpoint.UseCaseId));
            }
        }
    }

    private static void CheckRouteShape(ProjectModel model, List<Finding> findings)
    {
        // A route may nest under any resource the project holds, so every aggregate plural counts.
        var plurals = model.Records
            .Where(record => record.Type == "aggregate")
            .Select(record => Identifiers.TrySplit(record.Id, out _, out _, out var aggregateName) ? Plural(aggregateName) : null)
            .OfType<string>()
            .ToList();

        foreach (var endpoint in model.Code.Endpoints)
        {
            if (endpoint.Method.Length == 0 || endpoint.Route.Length == 0)
            {
                continue;
            }

            var name = model.ById.TryGetValue(endpoint.UseCaseId, out var useCase) && useCase.Type == "use-case"
                ? Identifiers.TrySplit(useCase.Id, out _, out _, out var split) ? split : null
                : null;
            var kind = useCase?.Field("kind")
                ?? model.Code.Handlers.FirstOrDefault(handler => handler.UseCaseId == endpoint.UseCaseId)?.Kind;

            if (name is null)
            {
                continue;
            }

            var verb = UseCaseVerbs.VerbOf(name, model.Config.PhrasalVerbs);
            var segments = endpoint.Route.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var plural = useCase is not null
                && ModelBuilder.AggregateOf(model, useCase) is { } owner
                && Identifiers.TrySplit(owner.Id, out _, out _, out var ownerName)
                    ? Plural(ownerName)
                    : null;

            if (kind == "command")
            {
                var last = segments.Length == 0 ? "" : VerbPart(segments[^1]);
                var rawLast = segments.Length == 0 ? "" : segments[^1];

                // A POST to the aggregate collection and a PUT, PATCH or DELETE on one resource are
                // ordinary resource routes; only a custom action names its verb in the last segment.
                var collectionPost = endpoint.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)
                    && plural is not null
                    && rawLast.Equals(plural, StringComparison.Ordinal);
                var resourceWrite = endpoint.Method.ToUpperInvariant() is "PUT" or "PATCH" or "DELETE"
                    && rawLast.StartsWith('{')
                    && rawLast.EndsWith('}');

                var methodVerb = MethodVerb(endpoint.Method, verb);

                if (last != verb && !collectionPost && !resourceWrite && !methodVerb)
                {
                    findings.Add(CodeFinding(
                        "route-shape",
                        $"Route '{endpoint.Method} {endpoint.Route}' for '{endpoint.UseCaseId}' ends in '{last}', not the use-case verb '{verb}'.",
                        endpoint.File,
                        endpoint.Line,
                        endpoint.UseCaseId));
                }
            }

            if (plural is not null)
            {
                var parts = segments.SelectMany(PluralParts).ToList();
                var named = parts.Any(part => plurals.Any(known =>
                    known == part || known.EndsWith("-" + part, StringComparison.Ordinal)));

                if (!named)
                {
                    findings.Add(CodeFinding(
                        "route-shape",
                        $"Route '{endpoint.Method} {endpoint.Route}' for '{endpoint.UseCaseId}' carries no aggregate plural such as '{plural}'.",
                        endpoint.File,
                        endpoint.Line,
                        endpoint.UseCaseId));
                }
            }
        }
    }

    /// <summary>Whether the HTTP method already says the verb: POST creates or adds, PUT or PATCH changes, DELETE removes.</summary>
    private static bool MethodVerb(string method, string verb) => method.ToUpperInvariant() switch
    {
        "POST" => verb is "create" or "add" or "register",
        "PUT" or "PATCH" => verb is "change",
        "DELETE" => verb is "remove",
        _ => false,
    };

    /// <summary>The verb a route segment carries: parameters removed, the custom method after any colon.</summary>
    private static string VerbPart(string segment)
    {
        var cleaned = StripParameters(segment);
        var colon = cleaned.LastIndexOf(':');

        return colon >= 0 ? cleaned[(colon + 1)..].Trim() : cleaned.Trim();
    }

    /// <summary>The name parts a route segment carries for the aggregate plural: parameters removed, split on any colon.</summary>
    private static IEnumerable<string> PluralParts(string segment) =>
        StripParameters(segment).Split(':').Select(part => part.Trim()).Where(part => part.Length > 0);

    /// <summary>The segment without its route parameters.</summary>
    private static string StripParameters(string segment)
    {
        var cleaned = segment;

        while (true)
        {
            var open = cleaned.IndexOf('{');
            var close = open < 0 ? -1 : cleaned.IndexOf('}', open);

            if (open < 0 || close < 0)
            {
                return cleaned;
            }

            cleaned = cleaned[..open] + cleaned[(close + 1)..];
        }
    }

    /// <summary>The plural of an aggregate name.</summary>
    private static string Plural(string name)
    {
        if (name.EndsWith('s'))
        {
            return name + "es";
        }

        if (name.EndsWith('y') && name.Length > 1 && !"aeiou".Contains(name[^2]))
        {
            return name[..^1] + "ies";
        }

        return name + "s";
    }

    /// <summary>Folder and type names that name the absence of a reason.</summary>
    private static readonly HashSet<string> ReasonlessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shared", "Common", "Util", "Utils", "Helpers", "Misc",
    };

    /// <summary>Type suffixes that name the absence of a reason.</summary>
    private static readonly string[] ReasonlessSuffixes = ["Helper", "Manager", "Util", "Utils"];

    private static void CheckReasonlessName(ProjectModel model, List<Finding> findings)
    {
        var kernel = model.Config.SharedKernel;
        var reportedFolders = new HashSet<string>(StringComparer.Ordinal);
        var reportedTypes = new HashSet<string>(StringComparer.Ordinal);
        var files = new List<string>();

        foreach (var handler in model.Code.Handlers)
        {
            files.Add(handler.File);
        }

        foreach (var reaction in model.Code.Reactions)
        {
            files.Add(reaction.File);
        }

        foreach (var domainEvent in model.Code.Events)
        {
            files.Add(domainEvent.File);
        }

        foreach (var endpoint in model.Code.Endpoints)
        {
            files.Add(endpoint.File);
        }

        foreach (var literal in model.Code.Literals)
        {
            files.Add(literal.File);
        }

        foreach (var comment in model.Code.Comments)
        {
            files.Add(comment.File);
        }

        foreach (var file in files.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            if (UnderSharedKernel(kernel, file))
            {
                continue;
            }

            var relative = model.Config.Relative(file);
            var parts = relative.Split('/');

            for (var index = 0; index < parts.Length - 1; index++)
            {
                var folder = string.Join('/', parts[..(index + 1)]);

                if (ReasonlessNames.Contains(parts[index]) && reportedFolders.Add(folder))
                {
                    findings.Add(CodeFinding(
                        "reasonless-name",
                        $"Folder '{folder}' carries the reasonless name '{parts[index]}'.",
                        file,
                        1,
                        null));
                }
            }
        }

        foreach (var (type, file, line) in ReasonlessTypeSites(model))
        {
            if (UnderSharedKernel(kernel, file) || !reportedTypes.Add(type))
            {
                continue;
            }

            findings.Add(CodeFinding(
                "reasonless-name",
                $"Type '{type}' carries a reasonless name.",
                file,
                line,
                null));
        }
    }

    /// <summary>Whether the file sits under the declared shared-kernel folder.</summary>
    private static bool UnderSharedKernel(string kernel, string file)
    {
        if (kernel.Length == 0)
        {
            return false;
        }

        var relative = Path.GetRelativePath(kernel, file);

        return !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative);
    }

    /// <summary>Every reasonless type name with its first site, in file and line order.</summary>
    private static List<(string Type, string File, int Line)> ReasonlessTypeSites(ProjectModel model)
    {
        var sites = new Dictionary<string, (string File, int Line)>(StringComparer.Ordinal);

        void Add(string? type, string file, int line)
        {
            if (type is not { Length: > 0 } || sites.ContainsKey(type))
            {
                return;
            }

            if (ReasonlessNames.Contains(type)
                || ReasonlessSuffixes.Any(suffix =>
                    type.EndsWith(suffix, StringComparison.Ordinal) && type.Length > suffix.Length))
            {
                sites[type] = (file, line);
            }
        }

        foreach (var handler in model.Code.Handlers)
        {
            Add(handler.Class, handler.File, handler.Line);
        }

        foreach (var reaction in model.Code.Reactions)
        {
            Add(reaction.Class, reaction.File, reaction.Line);
        }

        foreach (var domainEvent in model.Code.Events)
        {
            Add(domainEvent.Class, domainEvent.File, domainEvent.Line);
        }

        foreach (var literal in model.Code.Literals)
        {
            Add(literal.Class, literal.File, literal.Line);
        }

        return [.. sites
            .OrderBy(entry => entry.Value.File, StringComparer.Ordinal)
            .ThenBy(entry => entry.Value.Line)
            .Select(entry => (entry.Key, entry.Value.File, entry.Value.Line))];
    }

    private static void CheckCollaboratorHoldsRepository(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.Collaborators is null)
            {
                continue;
            }

            foreach (var collaborator in handler.Collaborators)
            {
                findings.Add(CodeFinding(
                    "collaborator-holds-repository",
                    $"Handler for '{handler.UseCaseId}' builds '{collaborator.Type}' with 'new', whose constructor takes a repository; register it in the container.",
                    collaborator.File,
                    collaborator.Line,
                    handler.UseCaseId));
            }
        }

        foreach (var reaction in model.Code.Reactions)
        {
            if (reaction.Collaborators is null)
            {
                continue;
            }

            var holder = reaction.RecordId ?? reaction.UseCaseId;
            var kind = reaction.Kind == "job" ? "Job" : "Reaction";

            foreach (var collaborator in reaction.Collaborators)
            {
                findings.Add(CodeFinding(
                    "collaborator-holds-repository",
                    $"{kind} '{reaction.Class}' builds '{collaborator.Type}' with 'new', whose constructor takes a repository; register it in the container.",
                    collaborator.File,
                    collaborator.Line,
                    holder));
            }
        }
    }

    private static void CheckRepositoryReadInLoop(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.LoopReads is null)
            {
                continue;
            }

            foreach (var read in handler.LoopReads)
            {
                findings.Add(CodeFinding(
                    "repository-read-in-loop",
                    $"Handler for '{handler.UseCaseId}' reads repository '{read.Repository}' inside a loop with '{read.Method}'.",
                    read.File,
                    read.Line,
                    handler.UseCaseId));
            }
        }

        foreach (var reaction in model.Code.Reactions)
        {
            if (reaction.LoopReads is null)
            {
                continue;
            }

            var holder = reaction.RecordId ?? reaction.UseCaseId;
            var kind = reaction.Kind == "job" ? "Job" : "Reaction";

            foreach (var read in reaction.LoopReads)
            {
                findings.Add(CodeFinding(
                    "repository-read-in-loop",
                    $"{kind} '{reaction.Class}' reads repository '{read.Repository}' inside a loop with '{read.Method}'.",
                    read.File,
                    read.Line,
                    holder));
            }
        }
    }

    private static void CheckQueryFiltersInMemory(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.MemoryFilters is null)
            {
                continue;
            }

            foreach (var filter in handler.MemoryFilters)
            {
                findings.Add(CodeFinding(
                    "query-filters-in-memory",
                    $"Query handler for '{handler.UseCaseId}' filters a repository list in memory.",
                    filter.File,
                    filter.Line,
                    handler.UseCaseId));
            }
        }
    }

    private static void CheckDependencyCount(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.DependencyCount > 10)
            {
                findings.Add(CodeFinding(
                    "dependency-count",
                    $"Handler for '{handler.UseCaseId}' takes {handler.DependencyCount} constructor dependencies.",
                    handler.File,
                    handler.Line,
                    handler.UseCaseId));
            }
        }

        foreach (var reaction in model.Code.Reactions)
        {
            if (reaction.DependencyCount > 10)
            {
                var holder = reaction.RecordId ?? reaction.UseCaseId;
                var kind = reaction.Kind == "job" ? "Job" : "Reaction";

                findings.Add(CodeFinding(
                    "dependency-count",
                    $"{kind} '{reaction.Class}' takes {reaction.DependencyCount} constructor dependencies.",
                    reaction.File,
                    reaction.Line,
                    holder));
            }
        }
    }

    private static void CheckEffectBeforeCommit(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == "use-case" && record.Field("kind") == "command"))
        {
            if (useCase.Raw.ValueKind != JsonValueKind.Object
                || !useCase.Raw.TryGetProperty("effects", out var effects)
                || effects.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in effects.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object
                    || !entry.TryGetProperty("timing", out var timing)
                    || timing.ValueKind != JsonValueKind.String
                    || timing.GetString() != "inline")
                {
                    continue;
                }

                foreach (var verb in new[] { "sends", "calls" })
                {
                    if (entry.TryGetProperty(verb, out var effect)
                        && effect.ValueKind == JsonValueKind.String
                        && effect.GetString() is { Length: > 0 } target)
                    {
                        findings.Add(CodeFinding(
                            "effect-before-commit",
                            $"Use case '{useCase.Id}' {verb} '{target}' inline; it runs after the commit from an event handler or a job.",
                            useCase.File,
                            useCase.Line,
                            useCase.Id));
                    }
                }
            }
        }
    }
}
