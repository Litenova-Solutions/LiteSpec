using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Checks;

/// <summary>
///     The code-reading checks, kept in one class so the partial checker holds only the entry point.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class CodeChecks
{
    /// <summary>Runs write-undeclared, result-undeclared, effect-undeclared, repository-unimplemented and the handler shape checks.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    public static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckWriteUndeclared(model, findings);
        CheckResultUndeclared(model, findings);
        CheckEffectUndeclared(model, findings);
        CheckOperationIdDuplicate(model, findings);
        CheckOperationUseCaseMismatch(model, findings);
        CheckReasonlessName(model, findings);
        CheckCollaboratorHoldsRepository(model, findings);
        CheckRepositoryReadInLoop(model, findings);
        CheckQueryFiltersInMemory(model, findings);
        CheckEffectBeforeCommit(model, findings);
        CheckRepositoryUnimplemented(model, findings);
    }

    /// <summary>
    ///     One S-level finding. The level comes from the finding code through FindingCodes,
    ///     which maps these three codes to S, so this helper builds the finding as written.
    /// </summary>
    /// <param name="code">The finding code that sets the level.</param>
    /// <param name="message">The finding message shown to the reader.</param>
    /// <param name="path">The file the finding is placed on.</param>
    /// <param name="line">The one-based line the finding is placed on.</param>
    /// <param name="subject">The record the finding names, or null when it names code alone.</param>
    /// <returns>The finding as written, graded by its code.</returns>
    private static Finding CodeFinding(string code, string message, string path, int line, string? subject) =>
        new(code, message, path, line, subject);


    /// <summary>Every event handler and job host as one row each, reactions then jobs.</summary>
    /// <param name="model">The loaded project model that holds each scanned reaction and job host.</param>
    /// <returns>One row per host, reactions first and jobs after.</returns>
    internal static IReadOnlyList<HostRow> Hosts(ProjectModel model)
    {
        var hosts = new List<HostRow>(model.Code.Reactions.Count + model.Code.Jobs.Count);

        foreach (var reaction in model.Code.Reactions)
        {
            hosts.Add(new HostRow(
                reaction.Class, reaction.File, reaction.Line, reaction.RecordId, reaction.UseCaseId,
                reaction.Repositories, reaction.Refusals, reaction.Thrown, reaction.Dispatches,
                reaction.DependencyCount, reaction.Collaborators, reaction.LoopReads));
        }

        foreach (var job in model.Code.Jobs)
        {
            hosts.Add(new HostRow(
                job.Class, job.File, job.Line, job.RecordId, job.UseCaseId,
                job.Repositories, job.Refusals, job.Thrown, job.Dispatches,
                job.DependencyCount, job.Collaborators, job.LoopReads));
        }

        return hosts;
    }

    /// <summary>Reports each repository a handler or host takes whose aggregate its record leaves undeclared.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckWriteUndeclared(ProjectModel model, List<Finding> findings)
    {
        // Each repository interface's implementing classes are looked up once, however many holders take it.
        var marked = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var handler in model.Code.Handlers)
        {
            if (handler.Repositories is not { Count: > 0 }
                || !model.ById.TryGetValue(handler.UseCaseId, out var useCase)
                || useCase.Type != RecordTypes.UseCase)
            {
                continue;
            }

            var declared = new HashSet<string>(useCase.Ids(RecordFields.Changes), StringComparer.Ordinal);
            declared.UnionWith(useCase.Ids(RecordFields.Reads));

            foreach (var repository in handler.Repositories)
            {
                if (!RepositoryDeclared(model, repository.Interface, declared, marked))
                {
                    findings.Add(CodeFinding(
                        "write-undeclared",
                        $"Use case '{handler.UseCaseId}' takes repository '{repository.Interface}', whose aggregate is outside its changes and reads.",
                        repository.File,
                        repository.Line,
                        handler.UseCaseId));
                }
            }
        }

        foreach (var host in Hosts(model))
        {
            if (host.Repositories is not { Count: > 0 }
                || host.RecordId is null
                || !model.ById.TryGetValue(host.RecordId, out var record)
                || record.Type is not (RecordTypes.Reaction or RecordTypes.Job))
            {
                continue;
            }

            var kind = record.Type == RecordTypes.Job ? "Job" : "Reaction";
            var declared = new HashSet<string>(record.Ids(RecordFields.Reads), StringComparer.Ordinal);

            foreach (var repository in host.Repositories)
            {
                if (!RepositoryDeclared(model, repository.Interface, declared, marked))
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

    /// <summary>
    ///     Whether the repository interface serves an aggregate the declared ids hold. The
    ///     aggregate an implementing class names in its implements mark decides; the interface
    ///     name decides only where no implementation names one.
    /// </summary>
    /// <param name="model">The loaded project model that holds every aggregate record and implementation.</param>
    /// <param name="face">The repository interface name under test.</param>
    /// <param name="declared">The aggregate identifiers the record declares in changes and reads.</param>
    /// <param name="marked">The aggregates each repository interface's implementing classes name, filled as interfaces are first seen.</param>
    /// <returns>True when the interface serves a declared aggregate or names no aggregate; otherwise false.</returns>
    private static bool RepositoryDeclared(ProjectModel model, string face, HashSet<string> declared, Dictionary<string, List<string>> marked)
    {
        if (!marked.TryGetValue(face, out var aggregates))
        {
            aggregates = MarkedRepositoryAggregates(model, face);
            marked[face] = aggregates;
        }

        if (aggregates.Count > 0)
        {
            return aggregates.Any(declared.Contains);
        }

        if (RepositoryAggregate(face) is not { } stem)
        {
            return true;
        }

        return model.RecordsByName[stem].Any(record => record.Type == RecordTypes.Aggregate && declared.Contains(record.Id));
    }

    /// <summary>The aggregates the classes implementing the repository interface name in their implements marks.</summary>
    /// <param name="model">The loaded project model that holds each implementation element and its marks.</param>
    /// <param name="face">The repository interface whose implementations are read.</param>
    /// <returns>The aggregate identifiers the implementing classes name in their implements marks.</returns>
    private static List<string> MarkedRepositoryAggregates(ProjectModel model, string face)
    {
        var aggregates = new List<string>();

        foreach (var implementation in model.Code.RepositoryImplementations.Where(entry => entry.Interface == face))
        {
            foreach (var element in model.Code.Elements.Where(element =>
                         element.Class == implementation.Class && element.File == implementation.File))
            {
                aggregates.AddRange(element.Marks
                    .Where(mark => mark.Verb == MarkVerb.Implements
                        && model.ById.TryGetValue(mark.RecordId, out var record)
                        && record.Type == RecordTypes.Aggregate)
                    .Select(mark => mark.RecordId));
            }
        }

        return aggregates;
    }

    /// <summary>The aggregate name a repository interface serves, in kebab-case, or null when it names none.</summary>
    /// <param name="face">The repository interface name, generic or plain.</param>
    /// <returns>The aggregate name in kebab-case, or null when the interface names no aggregate.</returns>
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

        if (type.StartsWith('I') && type.Length > 1 && char.IsUpper(type[1]))
        {
            type = type[1..];
        }

        if (!type.EndsWith("Repository", StringComparison.Ordinal) || type.Length <= "Repository".Length)
        {
            return null;
        }

        return Naming.Kebab(type[..^"Repository".Length]);
    }

    /// <summary>Reports each refusal code a handler or host produces that its use cases leave undeclared.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckResultUndeclared(ProjectModel model, List<Finding> findings)
    {
        var refusals = GenericRefusals(model);
        var declaring = DeclaringCodes(model);

        foreach (var handler in model.Code.Handlers)
        {
            if (!model.ById.TryGetValue(handler.UseCaseId, out var useCase) || useCase.Type != RecordTypes.UseCase)
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
                        $"Use case '{handler.UseCaseId}' produces '{code}', which no result of the use case declares.",
                        file,
                        line,
                        handler.UseCaseId));
                }
            }
        }

        foreach (var host in Hosts(model))
        {
            if (host.RecordId is null
                || !model.ById.TryGetValue(host.RecordId, out var record)
                || record.Type is not (RecordTypes.Reaction or RecordTypes.Job))
            {
                continue;
            }

            var runs = RunsUseCases(model, record);

            if (runs.Count == 0)
            {
                continue;
            }

            var declared = new HashSet<string>(StringComparer.Ordinal);

            foreach (var useCase in runs)
            {
                declared.UnionWith(DeclaredCodes(model, useCase, refusals));
            }

            var label = record.Type == RecordTypes.Job ? "Job" : "Reaction";

            foreach (var (code, file, line) in ProducedCodes(host.Refusals, host.Thrown, declaring))
            {
                if (!declared.Contains(code))
                {
                    findings.Add(CodeFinding(
                        "result-undeclared",
                        $"{label} '{record.Id}' produces '{code}', which its runs use cases declare none of.",
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
    /// <param name="model">The loaded project model that holds every result record.</param>
    /// <param name="useCase">The use case record whose declared refusal codes are collected.</param>
    /// <param name="refusals">The generic refusal codes a granted permission and a validated input derive.</param>
    /// <returns>The refusal codes the use case declares in writing or by derivation.</returns>
    private static HashSet<string> DeclaredCodes(
        ProjectModel model, Record useCase, (string Forbidden, string InvalidInput) refusals)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);

        if (Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
        {
            foreach (var result in model.RecordsByHome[name])
            {
                if (result.Type != RecordTypes.Result || result.Derived || result.Field(RecordFields.Outcome) != "refusal")
                {
                    continue;
                }

                if (result.Field(RecordFields.Code) is { } code)
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

    /// <summary>The generic refusal codes from .litespec/project.yaml refusals, or the defaults.</summary>
    /// <param name="model">The loaded project model that supplies the consumer root for the project file.</param>
    /// <returns>The forbidden and invalid-input codes the project declares, or the shared defaults.</returns>
    private static (string Forbidden, string InvalidInput) GenericRefusals(ProjectModel model)
    {
        const string forbidden = "authorization.forbidden";
        const string invalidInput = "request.validation-failed";

        try
        {
            var file = ConsumerRoot.ProjectFile(model.Config.Root);

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
    /// <param name="model">The loaded project model that holds each declaring code literal.</param>
    /// <returns>The refusal codes each declaring class carries, by class name.</returns>
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
    /// <param name="refusals">The refusal literals the class produces, or null when it produces none directly.</param>
    /// <param name="thrown">The exception and failure constructions of the class, or null when it constructs none.</param>
    /// <param name="declaring">The refusal codes each declaring class carries, by class name.</param>
    /// <returns>Each produced code with its earliest site, in line order.</returns>
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

    /// <summary>The use cases a reaction or job runs.</summary>
    /// <param name="model">The loaded project model that resolves each runs target.</param>
    /// <param name="record">The reaction or job record whose runs edges are read.</param>
    /// <returns>The use case records the holder runs.</returns>
    private static List<Record> RunsUseCases(ProjectModel model, Record record)
    {
        var found = new List<Record>();

        foreach (var id in record.EdgeIds(RecordFields.Runs))
        {
            if (model.ById.TryGetValue(id, out var useCase) && useCase.Type == RecordTypes.UseCase)
            {
                found.Add(useCase);
            }
        }

        return found;
    }

    /// <summary>Reports each dispatch a handler or host makes that its record declares no edge for.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEffectUndeclared(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.Dispatches is not { Count: > 0 }
                || !model.ById.TryGetValue(handler.UseCaseId, out var useCase)
                || useCase.Type != RecordTypes.UseCase)
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
                        $"Use case '{handler.UseCaseId}' {dispatch.Verb} '{DispatchTarget(dispatch)}'{ViaText(dispatch)}, which no effects entry of the use case declares.",
                        dispatch.File,
                        dispatch.Line,
                        handler.UseCaseId));
                }
            }
        }

        foreach (var host in Hosts(model))
        {
            if (host.Dispatches is not { Count: > 0 }
                || (host.RecordId ?? host.UseCaseId) is not { } holderId
                || !model.ById.TryGetValue(holderId, out var record)
                || record.Type is not (RecordTypes.Reaction or RecordTypes.Job or RecordTypes.UseCase))
            {
                continue;
            }

            // A reaction inside a use-case folder with no tag of its own belongs to that use case,
            // so its dispatches answer to the use case's effects.
            // Running the use case it sits with is the reaction's own trigger, not an edge.
            var edges = record.Type == RecordTypes.UseCase
                ? [.. UseCaseEdges(record), ("runs", record.Id)]
                : HolderEdges(record);
            var kind = record.Type switch
            {
                RecordTypes.UseCase => "Use case",
                RecordTypes.Reaction => "Reaction",
                _ => "Job",
            };

            foreach (var dispatch in host.Dispatches)
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
    /// <param name="dispatch">The scanned dispatch whose target is shown.</param>
    /// <returns>The port and method for a call dispatch; otherwise the dispatched message name.</returns>
    private static string DispatchTarget(CodeDispatch dispatch) =>
        dispatch.Verb == "calls" ? $"{dispatch.Detail}.{dispatch.Target}" : dispatch.Target;

    /// <summary>The helper a one-hop attribution found the dispatch through, or nothing when direct.</summary>
    /// <param name="dispatch">The scanned dispatch whose attribution is shown.</param>
    /// <returns>The via clause naming the helper the dispatch was found through, or an empty string when direct.</returns>
    private static string ViaText(CodeDispatch dispatch) =>
        dispatch.Via is { Length: > 0 } via ? $" via '{via}'" : "";

    /// <summary>The title and identifier a reaction or job finding names: the holder record when known, else the class.</summary>
    /// <param name="host">The host row whose class names the holder when no record does.</param>
    /// <param name="holder">The holder record identifier, or null when no record is known.</param>
    /// <returns>The prose kind title and the identifier the finding names.</returns>
    private static (string Title, string Id) HolderTitle(HostRow host, string? holder) =>
        holder switch
        {
            not null when holder.StartsWith("use-case/", StringComparison.Ordinal) => ("Use case", holder),
            not null when holder.StartsWith("reaction/", StringComparison.Ordinal) => ("Reaction", holder),
            not null when holder.StartsWith("job/", StringComparison.Ordinal) => ("Job", holder),
            not null => ("Reaction", holder),
            null => ("Reaction", host.Class),
        };

    /// <summary>Every edge a use case declares in its effects entries.</summary>
    /// <param name="useCase">The use case record whose effects entries are read.</param>
    /// <returns>Each declared edge verb with its target identifier.</returns>
    internal static List<(string Verb, string Target)> UseCaseEdges(Record useCase)
    {
        var edges = new List<(string Verb, string Target)>();

        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Effects, out var effects)
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

            foreach (var verb in new[] { RecordFields.Runs, RecordFields.Sends, RecordFields.Emits, RecordFields.Calls })
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

    /// <summary>Every edge a reaction or job declares: its runs, sends, emits and calls.</summary>
    /// <param name="record">The reaction or job record whose edge fields are read.</param>
    /// <returns>Each declared edge verb with its target identifier.</returns>
    internal static List<(string Verb, string Target)> HolderEdges(Record record)
    {
        var edges = new List<(string Verb, string Target)>();

        foreach (var verb in new[] { RecordFields.Runs, RecordFields.Sends, RecordFields.Emits, RecordFields.Calls })
        {
            foreach (var id in record.EdgeIds(verb))
            {
                edges.Add((verb, id));
            }
        }

        return edges;
    }

    /// <summary>Whether a declared edge of the same verb names the dispatch's target.</summary>
    /// <param name="model">The loaded project model that resolves dispatched messages and notifications.</param>
    /// <param name="edges">The edges the holder record declares.</param>
    /// <param name="dispatch">The scanned dispatch under test.</param>
    /// <returns>True when a declared edge of the same verb names the dispatch target; otherwise false.</returns>
    internal static bool EdgeDeclared(ProjectModel model, List<(string Verb, string Target)> edges, CodeDispatch dispatch) =>
        dispatch.Verb switch
        {
            "runs" => ResolveMessageUseCases(model, dispatch.Target)
                .Any(id => edges.Any(edge => edge.Verb == "runs" && edge.Target == id)),
            "emits" => edges.Any(edge => edge.Verb == "emits" && MessageMatches(dispatch.Target, edge.Target)),
            "sends" => edges.Any(edge => edge.Verb == "sends" && NotificationMatches(model, dispatch, edge.Target)),
            "calls" => edges.Any(edge => edge.Verb == "calls" && OperationMatches(dispatch.Target, edge.Target)),
            _ => true,
        };

    /// <summary>The use cases a dispatched message names, by its operation stem, in identifier order.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="message">The dispatched message type name.</param>
    /// <returns>The use case identifiers the message names, in identifier order.</returns>
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

        var found = stems
            .SelectMany(stem => model.RecordsByName[stem])
            .Where(record => record.Type == RecordTypes.UseCase)
            .Select(record => record.Id)
            .ToList();

        found.Sort(StringComparer.Ordinal);

        return found;
    }

    /// <summary>
    ///     Whether a dispatched message names a declared event edge: the message stem meets the
    ///     identifier after its slash, or its last segment, with dots reading as dashes.
    /// </summary>
    /// <param name="message">The dispatched message type name.</param>
    /// <param name="id">The declared event edge target identifier.</param>
    /// <returns>True when the message stem meets the identifier name or its last segment; otherwise false.</returns>
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
    /// <param name="model">The loaded project model that resolves the notification record.</param>
    /// <param name="dispatch">The scanned send dispatch under test.</param>
    /// <param name="id">The declared notification edge target identifier.</param>
    /// <returns>True when the dispatch names the notification by template key or message type; otherwise false.</returns>
    private static bool NotificationMatches(ProjectModel model, CodeDispatch dispatch, string id)
    {
        if (dispatch.Detail == "template")
        {
            return model.ById.TryGetValue(id, out var notification)
                && notification.Type == RecordTypes.Notification
                && notification.Field(RecordFields.Template) is { } template
                && template.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                           .Contains(dispatch.Target, StringComparer.Ordinal);
        }

        return MessageMatches(dispatch.Target, id);
    }

    /// <summary>Whether a port call names a declared operation: the method, without Async, meets the fragment.</summary>
    /// <param name="method">The called port method name.</param>
    /// <param name="id">The declared operation target with its fragment.</param>
    /// <returns>True when the method name without its Async suffix meets the fragment; otherwise false.</returns>
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

    /// <summary>Reports two endpoints declaring one operation id; the host refuses to start with both.</summary>
    /// <param name="model">The loaded project model that holds each scanned endpoint.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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

    /// <summary>The HTTP method keys a path item holds; other keys carry parameters, not operations.</summary>
    private static readonly HashSet<string> ContractMethods = new(StringComparer.Ordinal)
    {
        "get", "put", "post", "delete", "options", "head", "patch", "trace",
    };

    /// <summary>Reports a published operation whose use-case extension is missing or names another use case.</summary>
    /// <param name="model">The loaded project model that holds each endpoint and the contract path.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOperationUseCaseMismatch(ProjectModel model, List<Finding> findings)
    {
        var contract = model.Config.ContractPath;

        if (string.IsNullOrEmpty(contract))
        {
            return;
        }

        var uses = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var endpoint in model.Code.Endpoints)
        {
            if (endpoint.Operation.Length == 0 || endpoint.UseCaseId.Length == 0)
            {
                continue;
            }

            if (!uses.TryGetValue(endpoint.Operation, out var set))
            {
                set = new SortedSet<string>(StringComparer.Ordinal);
                uses[endpoint.Operation] = set;
            }

            set.Add(endpoint.UseCaseId);
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(File.ReadAllBytes(contract));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            var detail = exception.Message.Trim().TrimEnd('.');

            findings.Add(CodeFinding(
                "operation-use-case-drift",
                $"Contract '{PathText.Relative(model.Config.Root, contract)}' is unreadable: {(detail.Length > 0 ? detail : "an unknown error")}.",
                contract,
                1,
                null));
            return;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                findings.Add(CodeFinding(
                    "operation-use-case-drift",
                    $"Contract '{PathText.Relative(model.Config.Root, contract)}' is unreadable: the document is not an object.",
                    contract,
                    1,
                    null));
                return;
            }

            if (!document.RootElement.TryGetProperty(RecordFields.Paths, out var paths))
            {
                return;
            }

            if (paths.ValueKind != JsonValueKind.Object)
            {
                findings.Add(CodeFinding(
                    "operation-use-case-drift",
                    $"Contract '{PathText.Relative(model.Config.Root, contract)}' is unreadable: the 'paths' value is not an object.",
                    contract,
                    1,
                    null));
                return;
            }

            foreach (var pathItem in paths.EnumerateObject())
            {
                if (pathItem.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var method in pathItem.Value.EnumerateObject())
                {
                    if (!ContractMethods.Contains(method.Name)
                        || method.Value.ValueKind != JsonValueKind.Object
                        || !method.Value.TryGetProperty("operationId", out var named)
                        || named.ValueKind != JsonValueKind.String
                        || named.GetString() is not { Length: > 0 } operationId
                        || !uses.TryGetValue(operationId, out var useCases))
                    {
                        continue;
                    }

                    var extension = method.Value.TryGetProperty("x-litespec-use-case", out var carried)
                        && carried.ValueKind == JsonValueKind.String
                            ? carried.GetString()
                            : null;

                    foreach (var useCase in useCases)
                    {
                        if (extension is null)
                        {
                            findings.Add(CodeFinding(
                                "operation-use-case-drift",
                                $"Operation '{operationId}' carries no x-litespec-use-case; its endpoint implements '{useCase}'.",
                                contract,
                                1,
                                useCase));
                        }
                        else if (!string.Equals(extension, useCase, StringComparison.Ordinal))
                        {
                            findings.Add(CodeFinding(
                                "operation-use-case-drift",
                                $"Operation '{operationId}' names '{extension}' in x-litespec-use-case, but its endpoint implements '{useCase}'.",
                                contract,
                                1,
                                useCase));
                        }
                    }
                }
            }
        }
    }

    /// <summary>Folder and type names that name the absence of a reason.</summary>
    private static readonly HashSet<string> ReasonlessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shared", "Common", "Util", "Utils", "Helpers", "Misc",
    };

    /// <summary>Type suffixes that name the absence of a reason.</summary>
    private static readonly string[] ReasonlessSuffixes = ["Helper", "Manager", "Util", "Utils"];

    /// <summary>Reports each folder or type outside the shared kernel whose name states no reason.</summary>
    /// <param name="model">The loaded project model that holds the scanned code and the shared kernel path.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckReasonlessName(ProjectModel model, List<Finding> findings)
    {
        var kernel = model.Config.SharedKernelRoot;
        var reportedFolders = new HashSet<string>(StringComparer.Ordinal);
        var reportedTypes = new HashSet<string>(StringComparer.Ordinal);
        var files = new List<string>();

        foreach (var handler in model.Code.Handlers)
        {
            files.Add(handler.File);
        }

        foreach (var host in Hosts(model))
        {
            files.Add(host.File);
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

            var relative = PathText.Relative(model.Config.Root, file);
            var parts = relative.Split('/');

            for (var index = 0; index < parts.Length - 1; index++)
            {
                var folder = string.Join('/', parts[..(index + 1)]);

                if (ReasonlessNames.Contains(parts[index]) && reportedFolders.Add(folder))
                {
                    findings.Add(CodeFinding(
                        "name-reason-missing",
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
                "name-reason-missing",
                $"Type '{type}' carries a reasonless name.",
                file,
                line,
                null));
        }
    }

    /// <summary>Whether the file sits under the shared kernel project.</summary>
    /// <param name="kernel">The shared kernel project path.</param>
    /// <param name="file">The full path of the file under test.</param>
    /// <returns>True when the file sits under the shared kernel project; otherwise false.</returns>
    private static bool UnderSharedKernel(string kernel, string file)
    {
        if (kernel.Length == 0)
        {
            return false;
        }

        var relative = PathText.Relative(kernel, file);

        return !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative);
    }

    /// <summary>Every reasonless type name with its first site, in file and line order.</summary>
    /// <param name="model">The loaded project model that holds each scanned handler, host, event and literal.</param>
    /// <returns>Each reasonless type name with its first site, in file and line order.</returns>
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

        foreach (var host in Hosts(model))
        {
            Add(host.Class, host.File, host.Line);
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

    /// <summary>Reports each collaborator a handler or host builds directly whose constructor takes a repository.</summary>
    /// <param name="model">The loaded project model that holds each scanned handler and host.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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
                    "collaborator-repository-invalid",
                    $"Use case '{handler.UseCaseId}' builds '{collaborator.Type}' with 'new', whose constructor takes a repository.",
                    collaborator.File,
                    collaborator.Line,
                    handler.UseCaseId));
            }
        }

        foreach (var host in Hosts(model))
        {
            if (host.Collaborators is null)
            {
                continue;
            }

            var holder = host.RecordId ?? host.UseCaseId;
            var (kind, id) = HolderTitle(host, holder);

            foreach (var collaborator in host.Collaborators)
            {
                findings.Add(CodeFinding(
                    "collaborator-repository-invalid",
                    $"{kind} '{id}' builds '{collaborator.Type}' with 'new', whose constructor takes a repository.",
                    collaborator.File,
                    collaborator.Line,
                    holder));
            }
        }
    }

    /// <summary>Reports each repository read a handler or host performs inside a loop.</summary>
    /// <param name="model">The loaded project model that holds each scanned handler and host.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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
                    "repository-read-invalid",
                    $"Use case '{handler.UseCaseId}' reads repository '{read.Repository}' inside a loop with '{read.Method}'.",
                    read.File,
                    read.Line,
                    handler.UseCaseId));
            }
        }

        foreach (var host in Hosts(model))
        {
            if (host.LoopReads is null)
            {
                continue;
            }

            var holder = host.RecordId ?? host.UseCaseId;
            var (kind, id) = HolderTitle(host, holder);

            foreach (var read in host.LoopReads)
            {
                findings.Add(CodeFinding(
                    "repository-read-invalid",
                    $"{kind} '{id}' reads repository '{read.Repository}' inside a loop with '{read.Method}'.",
                    read.File,
                    read.Line,
                    holder));
            }
        }
    }

    /// <summary>Reports each query handler that filters a repository list in memory.</summary>
    /// <param name="model">The loaded project model that holds each scanned handler.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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
                    "query-filter-invalid",
                    $"Use case '{handler.UseCaseId}' filters a repository list in memory.",
                    filter.File,
                    filter.Line,
                    handler.UseCaseId));
            }
        }
    }

    /// <summary>Reports each command use case that sends or calls inline, before its commit.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEffectBeforeCommit(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "command"))
        {
            if (useCase.Raw.ValueKind != JsonValueKind.Object
                || !useCase.Raw.TryGetProperty(RecordFields.Effects, out var effects)
                || effects.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in effects.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object
                    || !entry.TryGetProperty(RecordFields.Timing, out var timing)
                    || timing.ValueKind != JsonValueKind.String
                    || timing.GetString() != "inline")
                {
                    continue;
                }

                foreach (var verb in new[] { RecordFields.Sends, RecordFields.Calls })
                {
                    if (entry.TryGetProperty(verb, out var effect)
                        && effect.ValueKind == JsonValueKind.String
                        && effect.GetString() is { Length: > 0 } target)
                    {
                        findings.Add(CodeFinding(
                            "effect-timing-invalid",
                            $"Use case '{useCase.Id}' {verb} '{target}' inline.",
                            useCase.File,
                            useCase.Line,
                            useCase.Id));
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Reports each aggregate a use case changes whose domain repository
    ///     interface no Infrastructure class implements. The check reads the
    ///     Infrastructure layer through the backend reader, so a consumer with
    ///     no Infrastructure folder carries no persistence layer for it to
    ///     judge, and an aggregate with no domain repository interface (a store,
    ///     or one whose Domain declares none) names nothing to implement.
    /// </summary>
    /// <param name="model">The loaded project model that holds every use case and the scanned repository implementations.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRepositoryUnimplemented(ProjectModel model, List<Finding> findings)
    {
        if (!Directory.Exists(model.Config.InfrastructureRoot))
        {
            return;
        }

        var implemented = model.Code.RepositoryImplementations
            .Select(entry => entry.Interface)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            foreach (var aggregateId in useCase.Ids(RecordFields.Changes))
            {
                if (!model.ById.TryGetValue(aggregateId, out var aggregate) || aggregate.Type != RecordTypes.Aggregate)
                {
                    continue;
                }

                if (aggregate.Field(RecordFields.Kind) == "store")
                {
                    continue;
                }

                if (!Identifiers.TrySplit(aggregateId, out _, out _, out var name))
                {
                    continue;
                }

                var face = $"I{Naming.Pascal(name)}Repository";

                if (!model.Code.DomainTypes.Any(type => type.Name == face))
                {
                    continue;
                }

                if (implemented.Contains(face))
                {
                    continue;
                }

                findings.Add(CodeFinding(
                    "repository-unimplemented",
                    $"Aggregate '{aggregateId}' changed by '{useCase.Id}' has no Infrastructure repository implementing '{face}'.",
                    useCase.File,
                    useCase.Line,
                    useCase.Id));
            }
        }
    }
}
