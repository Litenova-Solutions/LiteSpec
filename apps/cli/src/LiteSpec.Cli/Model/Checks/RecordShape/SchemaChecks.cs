using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The schema checks: every record carries the fields its type requires, names links of the types they may name, and holds its waiver, idempotency and failure-policy shapes.</summary>
/// <follows>litespec/core/model#state-the-common-fields-on-every-record</follows>
/// <follows>litespec/core/model#write-links-on-the-record-that-points</follows>
/// <follows>litespec/core/waivers#record-accepted-gaps-as-waivers</follows>
/// <follows>litespec/core/waivers#state-the-kind-of-every-waiver</follows>
/// <follows>litespec/core/waivers#name-the-records-that-close-a-gap</follows>
internal static class SchemaChecks
{
    /// <summary>The flow field that a flow no longer carries; its paths are branches now.</summary>
    const string RemovedFlowBranchField = "betweenSteps";

    /// <summary>The product field naming the problems the product addresses; the model holds no constant for it.</summary>
    const string ProductAddresses = "addresses";

    /// <summary>The observation date key on a goal.</summary>
    const string DateField = "date";

    /// <summary>The unknown-outcome key of an operation.</summary>
    const string UnknownField = "unknown";

    /// <summary>The waiver field naming the records whose change closes the gap.</summary>
    const string WaiverClosedBy = "closedBy";

    /// <summary>Runs the schema checks on every record.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            // A derived generic result inherits its shape from its use case.
            if (record.Derived)
            {
                continue;
            }

            if (record.Type.Length == 0)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.File}' has no type.", record.File, record.Line, null));
                continue;
            }

            if (!RecordTypes.All.Contains(record.Type))
            {
                continue;
            }

            if (record.Id.Length == 0)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.File}' of type '{record.Type}' has no id.", record.File, record.Line, null));
            }

            if (record.Title.Length == 0)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no title.", record.File, record.Line, SubjectOf(record)));
            }

            CheckTypeFields(model, record, findings);
        }
    }

    /// <summary>Caches the run targets of each built model, so the schema pass computes them once per model.</summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ProjectModel, HashSet<string>> RunTargetCache = new();

    /// <summary>
    ///     The use cases a reaction or job runs through its `runs` effect. Such a use case is
    ///     reached from inside the system.
    /// </summary>
    /// <param name="model">The loaded project model whose reactions and jobs declare the runs effects.</param>
    /// <returns>The identifiers of use cases that a reaction or job runs.</returns>
    private static HashSet<string> RunTargets(ProjectModel model) => RunTargetCache.GetValue(model, built =>
    {
        var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in built.Records)
        {
            if (record.Type is not (RecordTypes.Reaction or RecordTypes.Job) || record.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!record.Raw.TryGetProperty(RecordFields.Effects, out var list) || list.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in list.EnumerateArray())
            {
                if (Str(entry, RecordFields.Runs) is { Length: > 0 } target)
                {
                    targets.Add(target);
                }
            }
        }

        return targets;
    });

    /// <summary>Reports each field the record type requires that is missing, misshaped or outside its vocabulary.</summary>
    /// <param name="model">The loaded project model used to decide whether a use case needs its own entry point.</param>
    /// <param name="record">The record whose fields are judged by its type.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTypeFields(ProjectModel model, Record record, List<Finding> findings)
    {
        switch (record.Type)
        {
            case RecordTypes.Product:
                RequireString(record, RecordFields.Boundary, findings);
                RequireIds(record, ProductAddresses, 1, findings);

                // Only an excludes entry overrides a requirement; the list holds plain sentences.
                if (record.Has(RecordFields.Excludes))
                {
                    RequireArray(record, RecordFields.Excludes, 0, findings);
                }

                break;

            case RecordTypes.Goal:
                RequireString(record, RecordFields.Statement, findings);
                RequireString(record, RecordFields.Measure, findings);
                RequireString(record, RecordFields.Target, findings);
                CheckObservations(record, findings);
                break;

            case RecordTypes.Vision:
                RequireString(record, RecordFields.Statement, findings);
                break;

            case RecordTypes.Constraint:
                RequireString(record, RecordFields.Authority, findings);
                RequireArray(record, RecordFields.Sources, 1, findings);
                RequireEnum(record, RecordFields.Kind, ["technical", "organizational", "convention", "legal"], findings);
                RequireBoolean(record, RecordFields.Applies, findings);
                RequireString(record, RecordFields.AppliesBecause, findings);
                break;

            case RecordTypes.Research:
                RequireArray(record, RecordFields.Sources, 1, findings);
                RequireString(record, RecordFields.Conclusion, findings);
                break;

            case RecordTypes.Decision:
                RequireArray(record, RecordFields.Options, 1, findings);
                RequireString(record, RecordFields.ReopenedWhen, findings);
                RequireString(record, RecordFields.Choice, findings);
                break;

            case RecordTypes.Requirement:
                RequireEnum(record, RecordFields.Kind, ["functional", "quality", "policy"], findings);
                RequireString(record, RecordFields.Statement, findings);
                RequireString(record, RecordFields.Rationale, findings);
                CheckSetBy(record, findings);

                // A requirement with no feature stands at product level, and cites what it comes from.
                if (IsProductRequirement(record) && !CitesOriginOf(model, record))
                {
                    findings.Add(new Finding("requirement-cites-missing", $"Product requirement '{record.Id}' cites no constraint, goal or problem it comes from.", record.File, record.Line, SubjectOf(record)));
                }

                if (record.Field(RecordFields.Kind) == "policy")
                {
                    RequireArray(record, RecordFields.Values, 1, findings);
                }

                if (record.Field(RecordFields.Kind) == "quality")
                {
                    RequireString(record, RecordFields.Stimulus, findings);
                    RequireString(record, RecordFields.Environment, findings);
                    RequireString(record, RecordFields.Response, findings);
                    RequireString(record, RecordFields.Measure, findings);
                }

                break;

            case RecordTypes.Problem:
                RequireIds(record, RecordFields.Affects, 1, findings);
                RequireString(record, RecordFields.Statement, findings);
                break;

            case RecordTypes.Feature:

                if (record.Has(RecordFields.Serves))
                {
                    findings.Add(new Finding("record-invalid", $"Feature '{record.Id}' names serves and advances holds its goal.", record.File, record.Line, SubjectOf(record)));
                }

                // A feature stands on its own and names the problems it solves.
                RequireIds(record, RecordFields.Solves, 1, findings);
                break;

            case RecordTypes.Actor:
                RequireEnum(record, RecordFields.Kind, ["person", "organization"], findings);
                RequireRoleHolds(record, findings);
                break;

            case RecordTypes.Role:
                RequirePattern(record, RecordFields.Scope, @"^[a-z0-9]+(-[a-z0-9]+)*$", "kebab-case", findings);
                RequireIds(record, RecordFields.Allows, 1, findings);
                break;

            case RecordTypes.Permission:
                RequireString(record, RecordFields.Meaning, findings);
                break;

            case RecordTypes.Flow:
                RequireIds(record, RecordFields.Actor, 1, findings);
                CheckFlowReason(record, findings);
                RequireString(record, RecordFields.StartedWhen, findings);
                RequireString(record, RecordFields.FinishedWhen, findings);
                RequireArray(record, RecordFields.Steps, 1, findings);

                // Requirements are cited by each step, so the flow itself holds no cites.
                if (record.Has(RecordFields.Cites))
                {
                    findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' holds cites; each step cites the requirements it carries.", record.File, record.Line, SubjectOf(record)));
                }

                // The order of steps changes only through branches; the removed betweenSteps prose is refused.
                if (record.Has(RemovedFlowBranchField))
                {
                    findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' holds {RemovedFlowBranchField}; state each path in {RecordFields.Branches}.", record.File, record.Line, SubjectOf(record)));
                }

                CheckFlowBranches(record, findings);
                break;

            case RecordTypes.Module:
                RequireArray(record, RecordFields.DependsOn, 0, findings);
                break;

            case RecordTypes.Aggregate:
                RequireArray(record, RecordFields.States, 1, findings);
                RequireStateEntries(record, findings);
                break;

            case RecordTypes.Rule:
                RequireEnum(record, RecordFields.Kind, ["invariant", "precondition", "calculation", "atomicity", "reservation", "frozen-value"], findings);
                RequireString(record, RecordFields.Statement, findings);
                RequireIds(record, RecordFields.Governs, 1, findings);

                // An atomicity rule names the aggregates it binds, so the use case's writes can equal them.
                if (record.Field(RecordFields.Kind) == "atomicity")
                {
                    RequireIds(record, RecordFields.Binds, 1, findings);
                }

                // A rule that refuses names its refusal code, which the module's refusal set holds.
                if (record.Has(RecordFields.Refusal))
                {
                    RequireString(record, RecordFields.Refusal, findings);
                    CheckRefusalCode(model, record, record.Field(RecordFields.Refusal), findings);
                }

                break;

            case RecordTypes.UseCase:
                RequireEnum(record, RecordFields.Kind, ["command", "query"], findings);
                // A use case only a reaction or a job runs has no entry point of its own: only the
                // rest must name one.
                if (!RunTargets(model).Contains(record.Id))
                {
                    RequireIds(record, RecordFields.ReachedThrough, 1, findings);
                }

                // Exposure states the stake of an entry point use case in plain words.
                if (record.Has(RecordFields.ReachedThrough) && !record.Has(RecordFields.Exposure))
                {
                    findings.Add(new Finding("exposure-missing", $"Use case '{record.Id}' is reached through an entry point and states no exposure.", record.File, record.Line, SubjectOf(record)));
                }

                CheckExposureNames(record, findings);
                CheckBackground(model, record, findings);
                CheckInputs(model, record, findings);
                CheckOutputs(record, findings);
                CheckIdempotency(record, findings);
                CheckEffectFailurePolicy(record, findings);
                break;

            case RecordTypes.Reaction:
            case RecordTypes.Job:
                RequireArray(record, RecordFields.Effects, 1, findings);

                // Failure handling and idempotency are required on every reaction and job, and no reaction or job writes.
                if (!record.Has(RecordFields.OnFailure))
                {
                    findings.Add(new Finding("record-invalid", $"{FindingText.Label(record.Type)} '{record.Id}' states no onFailure.", record.File, record.Line, SubjectOf(record)));
                }

                if (!record.Has(RecordFields.Idempotency))
                {
                    findings.Add(new Finding("record-invalid", $"{FindingText.Label(record.Type)} '{record.Id}' states no idempotency.", record.File, record.Line, SubjectOf(record)));
                }

                if (record.Has(RecordFields.Writes))
                {
                    findings.Add(new Finding("record-invalid", $"{FindingText.Label(record.Type)} '{record.Id}' holds writes; a reaction or job writes through the use case it runs.", record.File, record.Line, SubjectOf(record)));
                }

                CheckRunsAtMostOne(record, findings);
                CheckIdempotency(record, findings);
                CheckFailurePolicy(record, findings);
                break;

            case RecordTypes.Setting:
                CheckSetBy(record, findings);
                break;

            case RecordTypes.ReadModel:
                RequireString(record, RecordFields.Summary, findings);
                RequireIds(record, RecordFields.KeyedBy, 1, findings);
                RequireArray(record, RecordFields.Fields, 1, findings);
                CheckFreshness(record, findings);

                // A read model is read by its queries, which carry its fulfilment; it fulfils nothing itself.
                if (record.Has(RecordFields.Fulfills))
                {
                    findings.Add(new Finding("record-invalid", $"Read model '{record.Id}' holds fulfills; the query that reads it fulfils the requirement.", record.File, record.Line, SubjectOf(record)));
                }

                break;

            case RecordTypes.ExternalSystem:
                CheckExternalSystem(record, findings);
                break;

            case RecordTypes.Result:
                RequireEnum(record, RecordFields.Outcome, ["success", "refusal"], findings);

                if (record.Field(RecordFields.Outcome) == "refusal")
                {
                    // A refusal result names its code and sentence; the module's refusal set holds the meaning.
                    if (record.Has(RecordFields.Meaning))
                    {
                        findings.Add(new Finding("record-invalid", $"Result '{record.Id}' holds meaning; a refusal result names its code and sentence, and the module's refusal set holds the meaning.", record.File, record.Line, SubjectOf(record)));
                    }

                    RequireString(record, RecordFields.Code, findings);

                    if (record.Field(RecordFields.Code) is { } code
                        && (code.Length == 0
                            || !Identifiers.RefusalCode().IsMatch(code)
                            || Identifiers.RefusalCode().Match(code).Value != code))
                    {
                        findings.Add(new Finding("record-invalid", $"Result '{record.Id}' has code '{code}', which is not a refusal code.", record.File, record.Line, SubjectOf(record)));
                    }
                    else if (record.Field(RecordFields.Code) is { } written)
                    {
                        CheckGenericRefusal(record, written, findings);
                        CheckRefusalCode(model, record, written, findings);
                    }
                }
                else
                {
                    RequireString(record, RecordFields.Meaning, findings);
                }

                break;

            case RecordTypes.Scenario:
                RequireString(record, RecordFields.Given, findings);
                RequireString(record, RecordFields.When, findings);
                RequireString(record, RecordFields.Then, findings);

                // A use case scenario names its result; a reaction or job scenario names its result or its outcome.
                if (!record.Has(RecordFields.Result) && !record.Has(RecordFields.Outcome))
                {
                    findings.Add(new Finding("record-invalid", $"Scenario '{record.Id}' names neither result nor outcome.", record.File, record.Line, SubjectOf(record)));
                }

                CheckExampleKeys(record, findings);
                break;

            case RecordTypes.DomainEvent:
                RequireString(record, RecordFields.Meaning, findings);
                RequireArray(record, RecordFields.Carries, 0, findings);
                CheckCarries(record, findings);
                break;

            case RecordTypes.Notification:
                RequireIds(record, RecordFields.Audience, 1, findings);
                RequireIds(record, RecordFields.DeliveredThrough, 1, findings);

                if (record.Has(RecordFields.Consent))
                {
                    findings.Add(new Finding("record-invalid", $"Notification '{record.Id}' holds consent as prose; name the rule in requiresConsent.", record.File, record.Line, SubjectOf(record)));
                }

                if (record.Field(RecordFields.Channel) == "webhook")
                {
                    findings.Add(new Finding("record-invalid", $"Notification '{record.Id}' names the webhook channel; a webhook to a system is an outbound integration event.", record.File, record.Line, SubjectOf(record)));
                }

                break;

            case RecordTypes.IntegrationEvent:
                RequireIds(record, RecordFields.Through, 1, findings);

                if (record.Field(RecordFields.Direction) == "in")
                {
                    RequireIds(record, RecordFields.ArrivesThrough, 1, findings);
                }

                break;

            case RecordTypes.EntryPoint:
                RequireEnum(record, RecordFields.Kind, ["browser", "api", "webhook", "device", "cli", "mcp"], findings);

                // An entry point carries no fulfills itself; a served-content entry carries it, and otherwise the use case it reaches fulfills the requirement.
                if (record.Has(RecordFields.Fulfills))
                {
                    findings.Add(new Finding("record-invalid", $"Entry point '{record.Id}' holds fulfills; a served-content entry carries the fulfills, and otherwise the use case the entry point reaches fulfills the requirement.", record.File, record.Line, SubjectOf(record)));
                }

                if (record.Has(RecordFields.ServedContent) && record.Field(RecordFields.Kind) != "browser")
                {
                    findings.Add(new Finding("record-invalid", $"Entry point '{record.Id}' holds servedContent, which only a browser entry point serves.", record.File, record.Line, SubjectOf(record)));
                }

                break;

            case RecordTypes.Screen:
                if (record.Has(RecordFields.Address) && record.Has(RecordFields.Host))
                {
                    findings.Add(new Finding("record-invalid", $"Screen '{record.Id}' carries address and host.", record.File, record.Line, SubjectOf(record)));
                }
                else if (!record.Has(RecordFields.Address) && !record.Has(RecordFields.Host))
                {
                    findings.Add(new Finding("record-invalid", $"Screen '{record.Id}' carries neither address nor host.", record.File, record.Line, SubjectOf(record)));
                }

                break;

            case RecordTypes.Runbook:
                RequireString(record, RecordFields.Trigger, findings);
                break;

            case RecordTypes.Waiver:
                RequireString(record, RecordFields.Reason, findings);
                RequireArray(record, RecordFields.Covers, 1, findings);
                RequireEnum(record, RecordFields.Kind, ["gap", "lasting"], findings);
                CheckWaiverScope(record, findings);
                CheckWaiverKind(record, findings);

                break;

            case RecordTypes.Term:
                if (record.Field(RecordFields.Meaning) is not { Length: > 0 })
                {
                    findings.Add(new Finding("record-invalid", $"Term '{record.Id}' has no meaning.", record.File, record.Line, SubjectOf(record)));
                }
                else if (record.Field(RecordFields.Meaning)!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length > 25)
                {
                    findings.Add(new Finding("term-meaning-invalid", $"Term '{record.Id}' has a meaning over 25 words.", record.File, record.Line, SubjectOf(record)));
                }

                break;
        }

        CheckLinkTypes(model, record, findings);
    }

    /// <summary>Reports a waiver naming neither the records nor the paths it covers.</summary>
    /// <param name="record">The waiver record whose subject and paths fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckWaiverScope(Record record, List<Finding> findings)
    {
        if (record.Ids(RecordFields.Subject).Count == 0 && record.Ids(RecordFields.Paths).Count == 0)
        {
            findings.Add(new Finding("record-invalid", $"Waiver '{record.Id}' names neither subject nor paths.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a gap waiver without its expiry date, exit or closing records, and a lasting waiver missing its consequences or holding a gap field.</summary>
    /// <param name="record">The waiver record whose kind fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckWaiverKind(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.Kind) == "gap")
        {
            RequireDate(record, RecordFields.ExpiresOn, findings);
            RequireString(record, RecordFields.Exit, findings);
            RequireIds(record, WaiverClosedBy, 1, findings);
        }
        else if (record.Field(RecordFields.Kind) == "lasting")
        {
            RequireString(record, RecordFields.Consequences, findings);

            foreach (var field in new[] { RecordFields.ExpiresOn, RecordFields.Exit, WaiverClosedBy })
            {
                if (record.Has(field))
                {
                    findings.Add(new Finding("record-invalid", $"Waiver '{record.Id}' is lasting and holds {field}.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

    /// <summary>Reports each link whose target record type the linking field forbids, and each step or actor a flow names wrongly.</summary>
    /// <param name="model">The loaded project model that resolves each link target.</param>
    /// <param name="record">The record whose links are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckLinkTypes(ProjectModel model, Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in record.Raw.EnumerateObject())
        {
            if (!RecordTypes.LinkTargets.TryGetValue(property.Name, out var allowed))
            {
                continue;
            }

            foreach (var target in ModelBuilder.TargetsOf(property.Value))
            {
                var baseId = Identifiers.BaseId(target);

                if (!model.ById.TryGetValue(baseId, out var resolved))
                {
                    continue;
                }

                // A technical decision serves the requirements it names in basedOn; every
                // other basedOn names only constraints and research.
                var servesRequirement = property.Name == RecordFields.BasedOn
                    && resolved.Type == RecordTypes.Requirement
                    && record.Type == RecordTypes.Decision
                    && MarkChecks.UnderImplementationDecisions(model.Config, record.File);

                if (allowed.Count > 0 && !allowed.Contains(resolved.Type) && !servesRequirement)
                {
                    findings.Add(new Finding("record-invalid", $"Record '{record.Id}' links {property.Name} to '{target}', which is a {resolved.Type}.", record.File, record.Line, SubjectOf(record)));
                }

                // A flow cites the requirements it shows; every other cites names
                // a goal, problem, feature, constraint, research or decision.
                if (property.Name == RecordFields.Cites && resolved.Type == RecordTypes.Requirement && record.Type != RecordTypes.Flow)
                {
                    findings.Add(new Finding("record-invalid", $"Record '{record.Id}' links cites to '{target}', which is a requirement.", record.File, record.Line, SubjectOf(record)));
                }

                CheckFragment(model, record, target, baseId, resolved, findings);
            }
        }

        if (record.Type == RecordTypes.Flow
            && record.Raw.TryGetProperty(RecordFields.Steps, out var steps)
            && steps.ValueKind == JsonValueKind.Array)
        {
            foreach (var step in steps.EnumerateArray())
            {
                if (step.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (step.TryGetProperty(RecordFields.UseCase, out var useCase)
                    && useCase.ValueKind == JsonValueKind.String
                    && useCase.GetString() is { } id
                    && model.ById.TryGetValue(id, out var resolved)
                    && resolved.Type != RecordTypes.UseCase)
                {
                    findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' steps to '{id}', which is a {resolved.Type}.", record.File, record.Line, SubjectOf(record)));
                }

                if (step.TryGetProperty(RecordFields.Actor, out var actor)
                    && actor.ValueKind == JsonValueKind.String
                    && actor.GetString() is { } actorId
                    && model.ById.TryGetValue(actorId, out var actorRecord)
                    && actorRecord.Type != RecordTypes.Actor)
                {
                    findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' names actor '{actorId}', which is a {actorRecord.Type}.", record.File, record.Line, SubjectOf(record)));
                }

                // A step performs with an external system only; jobs and reactions never appear in a step.
                if (step.TryGetProperty(RecordFields.System, out var system)
                    && system.ValueKind == JsonValueKind.String
                    && system.GetString() is { } systemId
                    && model.ById.TryGetValue(systemId, out var systemRecord)
                    && systemRecord.Type != RecordTypes.ExternalSystem)
                {
                    findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' names system '{systemId}' in a step, which is a {systemRecord.Type}.", record.File, record.Line, SubjectOf(record)));
                }

                // A step cites the requirements it carries, and nothing else.
                foreach (var cited in LinkStrings(step, RecordFields.Cites))
                {
                    if (model.ById.TryGetValue(Identifiers.BaseId(cited), out var citedRecord) && citedRecord.Type != RecordTypes.Requirement)
                    {
                        findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' step cites '{cited}', which is a {citedRecord.Type}, not a requirement.", record.File, record.Line, SubjectOf(record)));
                    }
                }
            }
        }

        if (record.Type == RecordTypes.EntryPoint
            && record.Raw.TryGetProperty(RecordFields.ServedContent, out var served)
            && served.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in served.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                // A served-content entry fulfills the requirements it names, and nothing else.
                foreach (var fulfilled in LinkStrings(entry, RecordFields.Fulfills))
                {
                    if (model.ById.TryGetValue(Identifiers.BaseId(fulfilled), out var fulfilledRecord) && fulfilledRecord.Type != RecordTypes.Requirement)
                    {
                        findings.Add(new Finding("record-invalid", $"Entry point '{record.Id}' serves content fulfilling '{fulfilled}', which is a {fulfilledRecord.Type}, not a requirement.", record.File, record.Line, SubjectOf(record)));
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Whether the link fragment names a flow step or an external-system
    ///     operation its record holds. The citation check reads only the base id,
    ///     so a missing step or operation is reported here.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the link target.</param>
    /// <param name="record">The record that holds the link.</param>
    /// <param name="target">The full link target including any step or operation fragment.</param>
    /// <param name="baseId">The target record identifier without its fragment.</param>
    /// <param name="resolved">The record the base identifier resolves to.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFragment(
        ProjectModel model, Record record, string target, string baseId, Record resolved, List<Finding> findings)
    {
        if (target == baseId)
        {
            return;
        }

        if (Identifiers.TrySplitStep(target, out _, out _))
        {
            if (resolved.Type == RecordTypes.Flow && !resolved.StepIds().Contains(target, StringComparer.Ordinal))
            {
                findings.Add(new Finding("reference-unknown", $"Record '{record.Id}' serves '{target}', which names no step of the flow.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }

        if (resolved.Type == RecordTypes.ExternalSystem)
        {
            var operation = target[(target.IndexOf('#', StringComparison.Ordinal) + 1)..];

            if (!OperationNames(resolved).Contains(operation, StringComparer.Ordinal))
            {
                findings.Add(new Finding("reference-unknown", $"Record '{record.Id}' calls '{target}', which names no operation of the system.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>The operation names an external system record declares.</summary>
    /// <param name="system">The external system record whose operations array is read.</param>
    /// <returns>The declared operation names in written order, or an empty list when it declares none.</returns>
    private static IReadOnlyList<string> OperationNames(Record system)
    {
        if (system.Raw.ValueKind != JsonValueKind.Object
            || !system.Raw.TryGetProperty(RecordFields.Operations, out var operations)
            || operations.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. operations.EnumerateArray()
                          .Where(entry => entry.ValueKind == JsonValueKind.Object
                              && entry.TryGetProperty(RecordFields.Name, out var name)
                              && name.ValueKind == JsonValueKind.String)
                          .Select(entry => entry.GetProperty(RecordFields.Name).GetString()!),
        ];
    }

    /// <summary>Decides whether text states a date as an ISO day or a timestamp.</summary>
    /// <param name="text">The date text under test.</param>
    /// <returns>True when the text parses as an ISO date or a timestamp; otherwise false.</returns>
    private static bool ValidDate(string text) =>
        IsoDate(text, out _)
        || DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _);

    /// <summary>
    ///     Reports a flow branch that lacks its step, condition or nextStep, and a branch whose
    ///     step or nextStep names no step of the flow. The end of a path is written as end.
    /// </summary>
    /// <param name="record">The flow record whose branches are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFlowBranches(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Branches, out var branches))
        {
            return;
        }

        if (branches.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {RecordFields.Branches} as no array.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (branches.GetArrayLength() == 0)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds an empty {RecordFields.Branches}.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        var stepTargets = record.StepIds().ToHashSet(StringComparer.Ordinal);

        foreach (var branch in branches.EnumerateArray())
        {
            if (branch.ValueKind != JsonValueKind.Object)
            {
                findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' holds a branch as no object.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            foreach (var field in new[] { RecordFields.Step, RecordFields.Condition, RecordFields.NextStep })
            {
                if (Str(branch, field) is not { Length: > 0 })
                {
                    findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' has a branch with no {field}.", record.File, record.Line, SubjectOf(record)));
                }
            }

            if (Str(branch, RecordFields.Step) is { Length: > 0 } step && !stepTargets.Contains($"{record.Id}#{step}"))
            {
                findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' branches from step '{step}', which it does not hold.", record.File, record.Line, SubjectOf(record)));
            }

            if (Str(branch, RecordFields.NextStep) is { Length: > 0 } next && next != "end" && !stepTargets.Contains($"{record.Id}#{next}"))
            {
                findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' branches to step '{next}', which it does not hold.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>The refusal names a product derives for every use case, which no result states by hand.</summary>
    private static readonly string[] GenericRefusals = ["forbidden", "invalid-input", "not-found", "conflict", "unauthenticated", "sign-in-too-old"];

    /// <summary>The modes a read model's freshness may state.</summary>
    private static readonly string[] FreshnessModes = ["immediate", "snapshot", "eventual"];

    /// <summary>The handling an unknown external outcome may name.</summary>
    private static readonly string[] UnknownHandlings = ["dedupe-key", "reconcile", "retry-safe"];

    /// <summary>A permission or entry point identifier written in prose.</summary>
    private static readonly Regex ProseReference = new(@"(entry-point|permission)/[a-z0-9][a-z0-9-]*", RegexOptions.CultureInvariant);

    /// <summary>The permission identifiers written in a grant.</summary>
    private static readonly Regex GrantPermission = new(@"permission/[a-z0-9][a-z0-9-]*", RegexOptions.CultureInvariant);

    /// <summary>Whether a requirement sits at product level, outside every feature folder.</summary>
    /// <param name="record">The requirement record whose file path is read.</param>
    /// <returns>True when the requirement file lies under the product requirements folder.</returns>
    private static bool IsProductRequirement(Record record) =>
        record.File.Replace('\\', '/').Contains("/intent/requirements/", StringComparison.Ordinal);

    /// <summary>Whether a product requirement cites a constraint, goal or problem it comes from.</summary>
    /// <param name="model">The loaded project model that resolves each cited identifier.</param>
    /// <param name="record">The requirement record whose cites are read.</param>
    /// <returns>True when one cited record is a constraint, a goal or a problem.</returns>
    private static bool CitesOriginOf(ProjectModel model, Record record) =>
        record.Ids(RecordFields.Cites).Any(id =>
            model.ById.TryGetValue(Identifiers.BaseId(id), out var resolved)
            && resolved.Type is RecordTypes.Constraint or RecordTypes.Goal or RecordTypes.Problem);

    /// <summary>Reports a flow that does not state exactly one reason, serves or answers, held as a list of one item.</summary>
    /// <param name="record">The flow record whose reason fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFlowReason(Record record, List<Finding> findings)
    {
        var serves = record.Has(RecordFields.Serves);
        var answers = record.Has(RecordFields.Answers);

        if (!serves && !answers)
        {
            findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' states neither serves nor answers.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (serves && answers)
        {
            findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' states serves and answers together.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        var field = serves ? RecordFields.Serves : RecordFields.Answers;
        if (!IsOneIdList(record, field))
        {
            findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' holds its {field} as a list of exactly one identifier.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Whether a record holds the named field as a list of exactly one string.</summary>
    /// <param name="record">The record to read.</param>
    /// <param name="name">The field name.</param>
    /// <returns>True when the field is an array of one string.</returns>
    private static bool IsOneIdList(Record record, string name) =>
        record.Raw.ValueKind == JsonValueKind.Object
        && record.Raw.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Array
        && value.GetArrayLength() == 1
        && value[0].ValueKind == JsonValueKind.String;

    /// <summary>Reports each goal observation that holds no date, value and source, or a date that is no date.</summary>
    /// <param name="record">The goal record whose observations are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckObservations(Record record, List<Finding> findings)
    {
        if (!record.Has(RecordFields.Observations))
        {
            return;
        }

        RequireArray(record, RecordFields.Observations, 0, findings);

        if (!record.Raw.TryGetProperty(RecordFields.Observations, out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in list.EnumerateArray())
        {
            var date = Str(entry, DateField);
            var value = Str(entry, RecordFields.Value);
            var source = Str(entry, RecordFields.Source);

            if (date is null || value is null || source is null)
            {
                findings.Add(new Finding("record-invalid", $"Goal '{record.Id}' holds an observation with no date, value and source.", record.File, record.Line, SubjectOf(record)));
            }
            else if (!ValidDate(date))
            {
                findings.Add(new Finding("record-invalid", $"Goal '{record.Id}' holds observation date '{date}', which is not a date.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Whether a refusal code names one of the generic refusals the product derives.</summary>
    /// <param name="code">The refusal code under test, bare or with its module prefix.</param>
    /// <returns>True when the name after the module prefix is a generic refusal; otherwise false.</returns>
    private static bool IsGenericRefusal(string code)
    {
        var name = code.Contains('.', StringComparison.Ordinal) ? code[(code.IndexOf('.', StringComparison.Ordinal) + 1)..] : code;
        return GenericRefusals.Contains(name, StringComparer.Ordinal);
    }

    /// <summary>Reports a result that writes a generic refusal by hand, which the product derives.</summary>
    /// <param name="record">The result record that holds the code.</param>
    /// <param name="code">The refusal code the result writes.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckGenericRefusal(Record record, string code, List<Finding> findings)
    {
        if (IsGenericRefusal(code))
        {
            findings.Add(new Finding("refusal-generic-written", $"Result '{record.Id}' writes refusal '{code}' by hand; the product derives it and no result states it.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a refusal code that is neither derived nor named in the refusal set of its module.</summary>
    /// <param name="model">The loaded project model that holds each module's refusal set.</param>
    /// <param name="record">The record that names the refusal code.</param>
    /// <param name="code">The refusal code under test, or null when the record names none.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRefusalCode(ProjectModel model, Record record, string? code, List<Finding> findings)
    {
        if (code is not { Length: > 0 } || IsGenericRefusal(code) || record.Module is not { Length: > 0 } module)
        {
            return;
        }

        if (!model.RefusalSets.Any(entry => entry.Module == module && entry.Code == code))
        {
            findings.Add(new Finding("reference-unknown", $"Record '{record.Id}' names refusal '{code}', which the refusal set of module '{module}' does not declare.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a read model whose freshness is not an object with a known mode and an ISO 8601 within.</summary>
    /// <param name="record">The read model record whose freshness is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFreshness(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Freshness, out var freshness) || freshness.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding("record-invalid", $"Read model '{record.Id}' states no freshness object.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        var mode = Str(freshness, RecordFields.Mode);

        if (mode is null || !FreshnessModes.Contains(mode, StringComparer.Ordinal))
        {
            findings.Add(new Finding("record-invalid", $"Read model '{record.Id}' holds freshness mode '{mode ?? "none"}' outside {string.Join(", ", FreshnessModes)}.", record.File, record.Line, SubjectOf(record)));
        }

        if (Str(freshness, RecordFields.Within) is { } within && !IsoDuration.IsMatch(within))
        {
            findings.Add(new Finding("record-invalid", $"Read model '{record.Id}' holds freshness within '{within}', which is not an ISO 8601 duration.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports each permission or entry point a use case's exposure names that its reachedThrough or access does not hold.</summary>
    /// <param name="record">The use case record whose exposure is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckExposureNames(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.Exposure) is not { Length: > 0 } exposure)
        {
            return;
        }

        var reached = record.Ids(RecordFields.ReachedThrough).ToHashSet(StringComparer.Ordinal);
        var granted = AccessPermissions(record);

        foreach (Match match in ProseReference.Matches(exposure))
        {
            var id = match.Value;
            var held = id.StartsWith("entry-point/", StringComparison.Ordinal) ? reached.Contains(id) : granted.Contains(id);

            if (!held)
            {
                findings.Add(new Finding("exposure-reference-drift", $"Use case '{record.Id}' names '{id}' in exposure, which its reachedThrough or access does not hold.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>The permission identifiers a use case's access grant names.</summary>
    /// <param name="record">The use case record whose access grant is read.</param>
    /// <returns>Each permission identifier the grant names, including those inside self(...).</returns>
    private static HashSet<string> AccessPermissions(Record record)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Access, out var access)
            || access.ValueKind != JsonValueKind.Object)
        {
            return found;
        }

        foreach (var grant in LinkStrings(access, RecordFields.Grant))
        {
            foreach (Match match in GrantPermission.Matches(grant))
            {
                found.Add(match.Value);
            }
        }

        return found;
    }

    /// <summary>Reports a use case with a background and fewer than two scenarios of its own.</summary>
    /// <param name="model">The loaded project model that holds every scenario.</param>
    /// <param name="record">The use case record whose background is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckBackground(ProjectModel model, Record record, List<Finding> findings)
    {
        if (!record.Has(RecordFields.Background) || !Identifiers.TrySplit(record.Id, out _, out _, out var name))
        {
            return;
        }

        var count = model.Records.Count(scenario =>
            scenario.Type == RecordTypes.Scenario
            && Identifiers.TrySplit(scenario.Id, out _, out var home, out _)
            && home == name);

        if (count < 2)
        {
            findings.Add(new Finding("background-scenarios-missing", $"Use case '{record.Id}' states a background with {count} scenario(s); a background needs at least two.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports an input that is a verified claim with no verifier, an unknown verifier, or a rule that is no rule record.</summary>
    /// <param name="model">The loaded project model that resolves each verifier and rule.</param>
    /// <param name="record">The use case record whose input array is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckInputs(ProjectModel model, Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Input, out var inputs) || inputs.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var input in inputs.EnumerateArray())
        {
            if (input.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = Str(input, RecordFields.Name) ?? "";
            var verifiedBy = Str(input, RecordFields.VerifiedBy);

            if (verifiedBy is { Length: > 0 } verifier && !model.ById.ContainsKey(Identifiers.BaseId(verifier)))
            {
                findings.Add(new Finding("reference-unknown", $"Use case '{record.Id}' input '{name}' names verifiedBy '{verifier}', which no record holds.", record.File, record.Line, SubjectOf(record)));
            }

            foreach (var rule in LinkStrings(input, RecordFields.Rules))
            {
                if (!model.ById.TryGetValue(Identifiers.BaseId(rule), out var resolved))
                {
                    findings.Add(new Finding("reference-unknown", $"Use case '{record.Id}' input '{name}' names rule '{rule}', which no record holds.", record.File, record.Line, SubjectOf(record)));
                }
                else if (resolved.Type != RecordTypes.Rule)
                {
                    findings.Add(new Finding("record-invalid", $"Use case '{record.Id}' input '{name}' names rule '{rule}', which is a {resolved.Type}.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

    /// <summary>Reports an output field that states no type, when the output is a list of fields.</summary>
    /// <param name="record">The use case record whose output list is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOutputs(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Output, out var outputs) || outputs.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var output in outputs.EnumerateArray())
        {
            if (output.ValueKind == JsonValueKind.Object
                && Str(output, RecordFields.Name) is { Length: > 0 } name
                && Str(output, RecordFields.Type) is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"Use case '{record.Id}' output field '{name}' states no type.", record.File, record.Line, SubjectOf(record)));
            }

            if (output.ValueKind == JsonValueKind.Object
                && Str(output, RecordFields.Type) == "list"
                && output.TryGetProperty(RecordFields.Of, out var rows))
            {
                Behavior.ContractChecks.CheckRowShape(record, "Use case", "output", Str(output, RecordFields.Name) ?? "(no name)", rows, findings);
            }
        }
    }

    /// <summary>Reports an external system's prose send, receive or failure field, and each operation with an unknown outcome that names no handling.</summary>
    /// <param name="record">The external system record whose fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckExternalSystem(Record record, List<Finding> findings)
    {
        foreach (var field in new[] { RecordFields.Sends, RecordFields.Receives, RecordFields.Failure })
        {
            if (record.Has(field))
            {
                findings.Add(new Finding("record-invalid", $"External system '{record.Id}' holds {field} as prose; its typed operations hold it.", record.File, record.Line, SubjectOf(record)));
            }
        }

        if (!record.Raw.TryGetProperty(RecordFields.Operations, out var operations) || operations.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var operation in operations.EnumerateArray())
        {
            if (operation.ValueKind != JsonValueKind.Object || !operation.TryGetProperty(UnknownField, out _))
            {
                continue;
            }

            var handling = Str(operation, RecordFields.OnUnknown);

            if (handling is null || !UnknownHandlings.Contains(handling, StringComparer.Ordinal))
            {
                findings.Add(new Finding("record-invalid", $"External system '{record.Id}' holds an operation with an unknown outcome and no onUnknown of {string.Join(", ", UnknownHandlings)}.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports a scenario whose example rows do not share one set of keys.</summary>
    /// <param name="record">The scenario record whose example rows are compared.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckExampleKeys(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Examples, out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var shapes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind == JsonValueKind.Object)
            {
                shapes.Add(string.Join(",", row.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)));
            }
        }

        if (shapes.Count > 1)
        {
            findings.Add(new Finding("example-keys-drift", $"Scenario '{record.Id}' holds example rows with differing keys.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a domain event payload entry that names no field or no type.</summary>
    /// <param name="record">The domain event record whose carries list is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckCarries(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Carries, out var carries) || carries.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in carries.EnumerateArray())
        {
            if (Str(entry, RecordFields.Name) is not { Length: > 0 } || Str(entry, RecordFields.Type) is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"Domain event '{record.Id}' carries a field with no name and no type.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports the record when the named field holds no non-empty string.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name that must hold text.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RequireString(Record record, string field, List<Finding> findings)
    {
        if (record.Field(field) is not { Length: > 0 })
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports the aggregate when any state entry lacks a name or a meaning.</summary>
    /// <param name="record">The aggregate record whose state entries are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RequireStateEntries(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.States, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object
                || !entry.TryGetProperty(RecordFields.Name, out var name)
                || name.ValueKind != JsonValueKind.String
                || string.IsNullOrEmpty(name.GetString())
                || !entry.TryGetProperty(RecordFields.Meaning, out var meaning)
                || meaning.ValueKind != JsonValueKind.String
                || string.IsNullOrEmpty(meaning.GetString()))
            {
                findings.Add(new Finding("record-invalid", $"Aggregate '{record.Id}' holds a state without a name and a meaning.", record.File, record.Line, SubjectOf(record)));
                return;
            }
        }
    }

    /// <summary>Reports the record when the named field is absent, holds no array, or holds fewer entries than required.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name that must hold an array.</param>
    /// <param name="minimum">The smallest entry count the field must hold.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RequireArray(Record record, string field, int minimum, List<Finding> findings)
    {
        if (!record.Has(field))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (record.Raw.TryGetProperty(field, out var value) && value.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {field} as no array.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (value.GetArrayLength() < minimum)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds an empty {field}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports the record when the named field names no identifier and at least one is required.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name that must name identifiers.</param>
    /// <param name="minimum">The smallest identifier count the field must name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RequireIds(Record record, string field, int minimum, List<Finding> findings)
    {
        var ids = record.Ids(field);

        if (!record.Has(field) || ids.Count == 0)
        {
            if (minimum > 0 || !record.Has(field))
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }
    }

    /// <summary>Reports each holds entry that is not a role identifier, so a hold that carries a scope is refused.</summary>
    /// <param name="record">The actor record whose holds list is read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RequireRoleHolds(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Holds, out var holds))
        {
            return;
        }

        if (holds.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds no list in {RecordFields.Holds}.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        foreach (var entry in holds.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.String)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds an entry in {RecordFields.Holds} that is not a role identifier.", record.File, record.Line, SubjectOf(record)));
            }
            else if (!Regex.IsMatch(entry.GetString() ?? "", @"^role/[a-z0-9][a-z0-9-]*$"))
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds '{entry.GetString()}' in {RecordFields.Holds}, which is not a role identifier.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports the record when the named field is absent or holds a value outside the allowed set.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name whose value is judged.</param>
    /// <param name="values">The allowed field values in message order.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RequireEnum(Record record, string field, string[] values, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!values.Contains(value, StringComparer.Ordinal))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has {field} '{value}' outside {string.Join(", ", values)}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a setBy outside the decider, operator and tenant who sets the value.</summary>
    /// <param name="record">The record whose setBy field is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSetBy(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.SetBy) is { } setBy && setBy is not ("product" or "operator" or "tenant"))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has setBy '{setBy}' outside product, operator, tenant.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports the record when the named field is absent or holds no boolean.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name that must hold a boolean.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RequireBoolean(Record record, string field, List<Finding> findings)
    {
        if (!record.Has(field))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (record.Raw.TryGetProperty(field, out var value)
            && value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {field} as no boolean.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports the record when the named field is absent or holds no date.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name that must hold a date.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RequireDate(Record record, string field, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!ValidDate(value))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has {field} '{value}', which is not a date.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports the record when the named field is absent or its value breaks the required pattern.</summary>
    /// <param name="record">The record whose field is required.</param>
    /// <param name="field">The field name whose value is judged.</param>
    /// <param name="pattern">The regular expression the value must match in full.</param>
    /// <param name="shape">The prose name of the required form, shown in the finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void RequirePattern(Record record, string field, string pattern, string shape, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!Regex.IsMatch(value, pattern))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has {field} '{value}', which is not {shape}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>The idempotency scopes a key may name.</summary>
    private static readonly string[] IdempotencyScopes = ["tenant", "account", "login", "global"];

    /// <summary>A {field} placeholder inside an idempotency key template.</summary>
    static readonly Regex IdempotencyPlaceholder = new(
        @"\{(?<name>[^{}]+)\}",
        RegexOptions.CultureInvariant);

    /// <summary>
    ///     The idempotency key and scope: the key templates {field} placeholders over
    ///     the record's input field names, and the scope names one of the four tenancies.
    /// </summary>
    /// <param name="record">The record whose idempotency key and scope are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckIdempotency(Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Idempotency, out var idempotency)
            || idempotency.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (idempotency.TryGetProperty(RecordFields.Scope, out var scope)
            && scope.ValueKind == JsonValueKind.String
            && scope.GetString() is { } scopeName
            && !IdempotencyScopes.Contains(scopeName, StringComparer.Ordinal))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds idempotency scope '{scopeName}' outside {string.Join(", ", IdempotencyScopes)}.", record.File, record.Line, SubjectOf(record)));
        }

        if (!idempotency.TryGetProperty(RecordFields.Key, out var key)
            || key.ValueKind != JsonValueKind.String
            || key.GetString() is not { } template)
        {
            return;
        }

        var fields = InputNames(record);
        var inScope = fields.Count > 0 ? string.Join(", ", fields) : "none";

        var placeholders = IdempotencyPlaceholder.Matches(template)
            .Select(match => match.Groups["name"].Value.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (placeholders.Count == 0)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds idempotency key '{template}' with no {{field}} placeholder and input fields in scope: {inScope}.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        // A reaction or a job takes no input: its key names fields of the triggering
        // event or the run window, which the record does not list.
        if (record.Type is RecordTypes.Reaction or RecordTypes.Job)
        {
            return;
        }

        foreach (var name in placeholders)
        {
            if (!fields.Contains(name, StringComparer.Ordinal))
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds idempotency key '{template}' where placeholder '{name}' names no input field with input fields in scope: {inScope}.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>The record's input field names, in written order.</summary>
    /// <param name="record">The record whose input array is read.</param>
    /// <returns>The input field names in written order, or an empty list when the record holds no input array.</returns>
    private static IReadOnlyList<string> InputNames(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Input, out var input)
            || input.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. input.EnumerateArray()
                      .Where(entry => entry.ValueKind == JsonValueKind.Object
                          && entry.TryGetProperty(RecordFields.Name, out var name)
                          && name.ValueKind == JsonValueKind.String
                          && name.GetString() is { Length: > 0 })
                      .Select(entry => entry.GetProperty(RecordFields.Name).GetString()!),
        ];
    }

    /// <summary>Reports a reaction or job whose effects hold more than one runs entry: it runs at most one use case.</summary>
    /// <param name="record">The reaction or job record whose effects are counted.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/core/automation#run-at-most-one-use-case-from-a-reaction-or-job</follows>
    private static void CheckRunsAtMostOne(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Effects, out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var runs = 0;

        foreach (var entry in list.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty(RecordFields.Runs, out _))
            {
                runs++;
            }
        }

        if (runs > 1)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has {runs} runs effects; a reaction or job runs at most one use case.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>
    ///     The reaction or job failure handling: a mode object naming retry with its
    ///     attempts and delays, or skip-and-log, or discard. Prose and malformed values report.
    /// </summary>
    /// <param name="record">The reaction or job record whose failure handling is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFailurePolicy(Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(RecordFields.OnFailure, out var policy))
        {
            CheckOnFailureObject(record, policy, "onFailure", findings);
        }

        CheckEffectFailurePolicy(record, findings);
    }

    /// <summary>One structured onFailure for every edge: each effect entry carries the same mode object.</summary>
    /// <param name="record">The record whose effect entries carry the failure policies under test.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEffectFailurePolicy(Record record, List<Finding> findings)
    {
        foreach (var entry in EffectEntries(record))
        {
            if (entry.TryGetProperty(RecordFields.OnFailure, out var policy))
            {
                CheckOnFailureObject(record, policy, "effect onFailure", findings);
            }
        }
    }

    /// <summary>The failure mode object: retry with attempts, delays and dead-letter, or a terminal mode.</summary>
    /// <param name="record">The record that holds the failure policy.</param>
    /// <param name="policy">The failure policy value under test.</param>
    /// <param name="where">The policy location description quoted in each finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOnFailureObject(Record record, JsonElement policy, string where, List<Finding> findings)
    {
        if (policy.ValueKind == JsonValueKind.String)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} '{policy.GetString()}' as prose with no mode object.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (policy.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} as no object.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!policy.TryGetProperty(RecordFields.Mode, out var mode)
            || mode.ValueKind != JsonValueKind.String
            || mode.GetString() is not { } shape)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} with no mode.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (shape is not ("retry" or "skip-and-log" or "discard"))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} mode '{shape}' outside retry, skip-and-log, discard.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (shape != "retry")
        {
            return;
        }

        if (!policy.TryGetProperty(RecordFields.Retry, out var retry) || retry.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} with no retry.", record.File, record.Line, SubjectOf(record)));
        }
        else
        {
            CheckRetryObject(record, retry, where, findings);
        }

        if (!policy.TryGetProperty(RecordFields.IfExhausted, out var then)
            || then.ValueKind != JsonValueKind.String
            || then.GetString() is not { Length: > 0 })
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} retry with no ifExhausted.", record.File, record.Line, SubjectOf(record)));
        }
        else if (then.GetString() != "dead-letter")
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} ifExhausted '{then.GetString()}' outside dead-letter.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>The retry object: positive attempts, ISO 8601 delays and a backoff of exponential or fixed.</summary>
    /// <param name="record">The record that holds the failure policy.</param>
    /// <param name="retry">The retry object under test.</param>
    /// <param name="where">The policy location description quoted in each finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRetryObject(Record record, JsonElement retry, string where, List<Finding> findings)
    {
        if (!retry.TryGetProperty(RecordFields.MaxAttempts, out var attempts)
            || attempts.ValueKind != JsonValueKind.Number
            || !attempts.TryGetInt32(out var count)
            || count < 1)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} retry with no positive maxAttempts.", record.File, record.Line, SubjectOf(record)));
        }

        CheckPolicyDuration(record, retry, "initialDelay", $"{where} retry initialDelay", findings);
        CheckPolicyDuration(record, retry, "maxDelay", $"{where} retry maxDelay", findings);

        if (!retry.TryGetProperty(RecordFields.Backoff, out var backoff)
            || backoff.ValueKind != JsonValueKind.String
            || backoff.GetString() is not { } shape)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} retry with no backoff.", record.File, record.Line, SubjectOf(record)));
        }
        else if (shape is not ("exponential" or "fixed"))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} retry backoff '{shape}' outside exponential, fixed.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>One ISO 8601 duration inside the failure policy: missing or malformed reports.</summary>
    /// <param name="record">The record that holds the failure policy.</param>
    /// <param name="holder">The policy object that holds the duration field.</param>
    /// <param name="field">The duration field name under test.</param>
    /// <param name="where">The duration location description quoted in each finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPolicyDuration(Record record, JsonElement holder, string field, string where, List<Finding> findings)
    {
        if (!holder.TryGetProperty(field, out var value)
            || value.ValueKind != JsonValueKind.String
            || value.GetString() is not { } text)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} with no duration.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!IsoDuration.IsMatch(text))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} '{text}', which is not an ISO 8601 duration.", record.File, record.Line, SubjectOf(record)));
        }
    }
}
