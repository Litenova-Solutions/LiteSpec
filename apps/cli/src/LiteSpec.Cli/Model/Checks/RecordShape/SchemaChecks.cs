using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;
using static LiteSpec.Cli.Model.Checks.RecordShape.Schema.SchemaEffectShapes;
using static LiteSpec.Cli.Model.Checks.RecordShape.Schema.SchemaFieldRules;
using static LiteSpec.Cli.Model.Checks.RecordShape.Schema.SchemaFlowShapes;
using static LiteSpec.Cli.Model.Checks.RecordShape.Schema.SchemaLinkResolution;
using static LiteSpec.Cli.Model.Checks.RecordShape.Schema.SchemaRecordShapes;
using static LiteSpec.Cli.Model.Checks.RecordShape.Schema.SchemaRefusalRules;
using static LiteSpec.Cli.Model.Checks.RecordShape.Schema.SchemaWaiverShapes;

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

    /// <summary>The product field naming the actors the product serves.</summary>
    const string ProductAddresses = "users";

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

            case RecordTypes.Direction:
                RequireString(record, RecordFields.Statement, findings);
                break;

            case RecordTypes.Constraint:
                RequireString(record, RecordFields.Authority, findings);
                RequireArray(record, RecordFields.Sources, 1, findings);
                RequireEnum(record, RecordFields.Kind, ["technical", "organizational", "convention", "legal"], findings);
                RequireBoolean(record, RecordFields.Applies, findings);
                RequireString(record, RecordFields.AppliesBecause, findings);
                RequireString(record, RecordFields.Scope, findings);
                RequireArray(record, RecordFields.Obligations, 1, findings);
                RequireString(record, RecordFields.ProductEffect, findings);
                break;

            case RecordTypes.Research:
                RequireArray(record, RecordFields.Sources, 1, findings);
                RequireString(record, RecordFields.Conclusion, findings);
                RequireArray(record, RecordFields.Findings, 1, findings);
                break;

            case RecordTypes.Decision:
                RequireArray(record, RecordFields.Options, 1, findings);
                RequireString(record, RecordFields.ReopenedWhen, findings);
                RequireString(record, RecordFields.Choice, findings);
                RequireString(record, RecordFields.Context, findings);
                break;

            case RecordTypes.Requirement:
                RequireEnum(record, RecordFields.Kind, ["functional", "quality", "policy"], findings);
                RequireString(record, RecordFields.Statement, findings);
                RequireString(record, RecordFields.Rationale, findings);
                CheckSetBy(record, findings);

                // A requirement with no feature stands at product level, and names in basedOn what it comes from.
                if (IsProductRequirement(record) && !CitesOriginOf(model, record))
                {
                    findings.Add(new Finding("requirement-cites-missing", $"Product requirement '{record.Id}' names no constraint, goal or problem it comes from in basedOn.", record.File, record.Line, SubjectOf(record)));
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
                RequireArray(record, RecordFields.FailureCases, 1, findings);
                RequireString(record, RecordFields.Workaround, findings);
                RequireString(record, RecordFields.Cost, findings);
                RequireArray(record, RecordFields.ResolutionCriteria, 1, findings);
                break;

            case RecordTypes.Feature:

                if (record.Has(RecordFields.Serves))
                {
                    findings.Add(new Finding("record-invalid", $"Feature '{record.Id}' names serves and advances holds its goal.", record.File, record.Line, SubjectOf(record)));
                }

                RequireArray(record, RecordFields.SuccessCriteria, 1, findings);
                RequireArray(record, RecordFields.Excludes, 1, findings);
                break;

            case RecordTypes.Actor:
                RequireEnum(record, RecordFields.Kind, ["person", "organization", "system"], findings);
                RequireArray(record, RecordFields.Responsibilities, 1, findings);
                RequireArray(record, RecordFields.Knowledge, 1, findings);
                RequireRoleHolds(record, findings);
                break;

            case RecordTypes.Role:
                RequirePattern(record, RecordFields.Scope, @"^[a-z0-9]+(-[a-z0-9]+)*$", "kebab-case", findings);
                RequireArray(record, RecordFields.Excludes, 1, findings);
                break;

            case RecordTypes.Permission:
                RequireString(record, RecordFields.Meaning, findings);
                RequireIds(record, RecordFields.GrantedTo, 1, findings);
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

            case RecordTypes.Step:
                RequireString(record, RecordFields.Statement, findings);
                CheckStepLinks(model, record, findings);
                CheckStepBranches(model, record, findings);
                break;

            case RecordTypes.Module:
                RequireArray(record, RecordFields.DependsOn, 0, findings);
                RequireArray(record, RecordFields.Covers, 1, findings);
                RequireArray(record, RecordFields.Excludes, 1, findings);
                break;

            case RecordTypes.Aggregate:
                RequireArray(record, RecordFields.States, 1, findings);
                RequireStateEntries(record, findings);
                break;

            case RecordTypes.Rule:
                RequireEnum(record, RecordFields.Kind, ["invariant", "precondition", "calculation", "atomicity", "reservation", "frozen-value"], findings);
                RequireString(record, RecordFields.Statement, findings);
                RequireIds(record, RecordFields.Governs, 1, findings);

                // An atomicity rule names the aggregates it governs, so the use case's writes can equal them.
                if (record.Field(RecordFields.Kind) == "atomicity")
                {
                    RequireIds(record, RecordFields.Governs, 1, findings);
                }

                // A rule that refuses names its refusal, which the module's refusal set holds.
                if (record.Has(RecordFields.Refusal))
                {
                    RequireString(record, RecordFields.Refusal, findings);
                    CheckRefusalCode(model, record, record.Field(RecordFields.Refusal), findings);
                }

                break;

            case RecordTypes.UseCase:
                RequireEnum(record, RecordFields.Kind, ["command", "query"], findings);
                RequireArray(record, RecordFields.Steps, 1, findings);
                // A use case only a reaction or a job runs has no entry point of its own: only the
                // rest must name one.
                if (!RunTargets(model).Contains(record.Id))
                {
                    RequireIds(record, RecordFields.Via, 1, findings);
                }

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
                RequireString(record, RecordFields.Vendor, findings);
                RequireIds(record, RecordFields.Represents, 1, findings);
                CheckExternalSystem(model, record, findings);
                break;

            case RecordTypes.Result:
                RequireEnum(record, RecordFields.Outcome, ["success", "refusal"], findings);

                if (record.Field(RecordFields.Outcome) == "refusal")
                {
                    // A refusal result names its refusal and sentence; the module's refusal set holds the meaning.
                    if (record.Has(RecordFields.Meaning))
                    {
                        findings.Add(new Finding("record-invalid", $"Result '{record.Id}' holds meaning; a refusal result names its refusal and sentence, and the module's refusal set holds the meaning.", record.File, record.Line, SubjectOf(record)));
                    }

                    RequireString(record, RecordFields.Refusal, findings);

                    if (record.Field(RecordFields.Refusal) is { } refusal
                        && !IsRefusalId(refusal))
                    {
                        findings.Add(new Finding("record-invalid", $"Result '{record.Id}' has refusal '{refusal}', which is not a refusal identifier.", record.File, record.Line, SubjectOf(record)));
                    }
                    else if (record.Field(RecordFields.Refusal) is { } written)
                    {
                        CheckGenericRefusal(record, RefusalCode.CodeOf(written) ?? written, findings);
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
                RequireArray(record, RecordFields.Fields, 0, findings);
                CheckCarries(record, findings);
                break;

            case RecordTypes.Notification:
                RequireIds(record, RecordFields.Audience, 1, findings);
                RequireIds(record, RecordFields.Via, 1, findings);

                if (record.Has(RecordFields.Consent))
                {
                    findings.Add(new Finding("record-invalid", $"Notification '{record.Id}' holds consent as prose; name the rule in consentRule.", record.File, record.Line, SubjectOf(record)));
                }

                if (record.Strings(RecordFields.Channels).Contains("webhook", StringComparer.Ordinal))
                {
                    findings.Add(new Finding("record-invalid", $"Notification '{record.Id}' names the webhook channel; a webhook to a system is an outbound integration event.", record.File, record.Line, SubjectOf(record)));
                }

                break;

            case RecordTypes.IntegrationEvent:
                RequireIds(record, RecordFields.Via, 1, findings);
                RequireString(record, RecordFields.Compatibility, findings);
                RequireEnum(record, RecordFields.Direction, ["send", "receive"], findings);

                break;

            case RecordTypes.ModuleEvent:
                RequireString(record, RecordFields.Compatibility, findings);
                break;

            case RecordTypes.EntryPoint:
                RequireEnum(record, RecordFields.Kind, ["browser", "api", "webhook", "device", "cli", "mcp"], findings);
                RequireIds(record, RecordFields.Audience, 1, findings);

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
                RequireString(record, RecordFields.Job, findings);
                RequireString(record, RecordFields.Archetype, findings);
                RequireArray(record, RecordFields.States, 1, findings);
                if (record.Has(RecordFields.Route) && record.Has(RecordFields.Host))
                {
                    findings.Add(new Finding("record-invalid", $"Screen '{record.Id}' carries route and host.", record.File, record.Line, SubjectOf(record)));
                }
                else if (!record.Has(RecordFields.Route) && !record.Has(RecordFields.Host))
                {
                    findings.Add(new Finding("record-invalid", $"Screen '{record.Id}' carries neither route nor host.", record.File, record.Line, SubjectOf(record)));
                }

                break;

            case RecordTypes.Runbook:
                RequireString(record, RecordFields.Trigger, findings);
                break;

            case RecordTypes.Waiver:
                RequireString(record, RecordFields.Reason, findings);
                RequireArray(record, RecordFields.Waives, 1, findings);
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
}
