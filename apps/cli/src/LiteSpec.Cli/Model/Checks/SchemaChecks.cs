using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The schema checks: every record carries the fields its type requires, names links of the types they may name, and holds its exception, idempotency and failure-policy shapes.</summary>
/// <follows>litespec/core/model#state-the-common-fields-on-every-record</follows>
/// <follows>litespec/core/model#write-links-on-the-record-that-points</follows>
/// <follows>litespec/core/model#keep-one-owner-and-status-on-every-record</follows>
/// <follows>litespec/core/model#record-accepted-gaps-as-exceptions</follows>
internal static class SchemaChecks
{
    /// <summary>Whether the record lives in the behavior layer, from the one layer table.</summary>
    private static bool IsBehavior(Record record) =>
        RecordTypes.LayerByType.TryGetValue(record.Type, out var layer) && layer == "behavior";

    /// <summary>Runs the schema checks on every record.</summary>
    internal static void Run(ProjectModel model, List<Finding> findings, Codeowners owners)
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

            if (record.Owner is null)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no owner.", record.File, record.Line, SubjectOf(record)));
            }
            else if (model.Config.HasOwners && !model.Config.Owners.Contains(record.Owner, StringComparer.Ordinal))
            {
                findings.Add(new Finding("owner-unknown", $"Record '{record.Id}' names owner '{record.Owner}', which the owner list does not hold.", record.File, record.Line, SubjectOf(record)));
            }
            else if (record.Owner is { } owner && !owners.Covers(model.Config.Root, record.File, model.Config.CandidatesFor(owner)))
            {
                findings.Add(new Finding("owner-unknown", $"Record '{record.Id}' names owner '{owner}', which belongs to no team owning its path.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Status is null)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no status.", record.File, record.Line, SubjectOf(record)));
            }
            else if (!ValidStatus(record))
            {
                findings.Add(new Finding("field-invalid", $"Record '{record.Id}' has status '{record.Status}' outside {StatusVocabulary(record)}.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Status == RecordStatuses.Draft)
            {
                if (record.Decider is null)
                {
                    findings.Add(new Finding("decider-missing", $"Record '{record.Id}' is a draft with no decider.", record.File, record.Line, SubjectOf(record)));
                }
                else if (model.Config.HasOwners && !model.Config.Owners.Contains(record.Decider, StringComparer.Ordinal))
                {
                    findings.Add(new Finding("owner-unknown", $"Record '{record.Id}' is a draft naming decider '{record.Decider}' outside the owner list.", record.File, record.Line, SubjectOf(record)));
                }

                if (record.DecideBy is null)
                {
                    findings.Add(new Finding("decider-missing", $"Record '{record.Id}' is a draft with no decide-by date.", record.File, record.Line, SubjectOf(record)));
                }
                else if (!ValidDate(record.DecideBy))
                {
                    findings.Add(new Finding("field-invalid", $"Record '{record.Id}' is a draft with decide-by '{record.DecideBy}' which is no date.", record.File, record.Line, SubjectOf(record)));
                }
            }

            CheckQuestionDeciders(record, model, findings);
            CheckTypeFields(model, record, findings);
            CheckReplaces(model, record, findings);
        }
    }

    /// <summary>Whether the status fits the record layer vocabulary.</summary>
    private static bool ValidStatus(Record record)
    {
        if (record.Status is RecordStatuses.Draft or RecordStatuses.Retired)
        {
            return true;
        }

        // Terms span both layers, so either live status fits.
        if (record.Type == RecordTypes.Term)
        {
            return record.Status is RecordStatuses.Approved or RecordStatuses.Reviewed;
        }

        return IsBehavior(record)
            ? record.Status == RecordStatuses.Reviewed
            : record.Status == RecordStatuses.Approved;
    }

    /// <summary>The status words the record layer vocabulary allows.</summary>
    private static string StatusVocabulary(Record record)
    {
        if (record.Type == RecordTypes.Term)
        {
            return "draft, approved, reviewed or retired";
        }

        return IsBehavior(record)
            ? "draft, reviewed or retired"
            : "draft, approved or retired";
    }

    /// <summary>Reports a question entry naming no decider or no decide-by date.</summary>
    private static void CheckQuestionDeciders(Record record, ProjectModel model, List<Finding> findings)
    {
        if (record.Status == RecordStatuses.Retired)
        {
            return;
        }

        var decider = false;
        var date = false;

        foreach (var entry in QuestionEntries(record))
        {
            if (!decider)
            {
                var named = Str(entry, RecordFields.Decider);

                if (named is not { Length: > 0 })
                {
                    findings.Add(new Finding("decider-missing", $"Record '{record.Id}' holds a question that names no decider.", record.File, record.Line, SubjectOf(record)));
                    decider = true;
                }
                else if (model.Config.HasOwners && !model.Config.Owners.Contains(named, StringComparer.Ordinal))
                {
                    findings.Add(new Finding("owner-unknown", $"Record '{record.Id}' holds a question naming decider '{named}' outside the owner list.", record.File, record.Line, SubjectOf(record)));
                    decider = true;
                }
            }

            if (!date)
            {
                var decideBy = Str(entry, RecordFields.DecideBy);

                if (decideBy is not { Length: > 0 })
                {
                    findings.Add(new Finding("decider-missing", $"Record '{record.Id}' holds a question that names no decide-by date.", record.File, record.Line, SubjectOf(record)));
                    date = true;
                }
                else if (!ValidDate(decideBy))
                {
                    findings.Add(new Finding("field-invalid", $"Record '{record.Id}' holds a question with decide-by '{decideBy}' which is no date.", record.File, record.Line, SubjectOf(record)));
                    date = true;
                }
            }

            if (decider && date)
            {
                break;
            }
        }
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ProjectModel, HashSet<string>> RunTargetCache = new();

    /// <summary>
    ///     The use cases another record runs: a reaction, job or use case through a `runs`
    ///     effect, or a saga through a step. Such a use case is reached from inside the system.
    /// </summary>
    private static HashSet<string> RunTargets(ProjectModel model) => RunTargetCache.GetValue(model, built =>
    {
        var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in built.Records)
        {
            if (record.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var listName in new[] { RecordFields.Effects, RecordFields.Steps })
            {
                if (!record.Raw.TryGetProperty(listName, out var list) || list.ValueKind != JsonValueKind.Array)
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
        }

        return targets;
    });

    private static void CheckTypeFields(ProjectModel model, Record record, List<Finding> findings)
    {
        switch (record.Type)
        {
            case RecordTypes.Product:
                RequireString(record, RecordFields.Boundary, findings);
                RequireArray(record, RecordFields.Exclusions, 0, findings);
                RequireIds(record, RecordFields.Addresses, 1, findings);
                break;

            case RecordTypes.Goal:
                RequireString(record, RecordFields.Statement, findings);
                RequireString(record, RecordFields.Measure, findings);
                RequireString(record, RecordFields.Target, findings);
                RequireString(record, RecordFields.DueBy, findings);
                break;

            case RecordTypes.Constraint:
                RequireString(record, RecordFields.Authority, findings);
                RequireEnum(record, RecordFields.Kind, ["technical", "organizational", "convention", "legal"], findings);
                RequireBoolean(record, RecordFields.Applies, findings);
                RequireString(record, RecordFields.AppliesBecause, findings);
                break;

            case RecordTypes.Research:
                RequireArray(record, RecordFields.Sources, 1, findings);
                RequireString(record, RecordFields.Question, findings);
                RequireString(record, RecordFields.Conclusion, findings);
                break;

            case RecordTypes.Decision:
                RequireArray(record, RecordFields.Options, 1, findings);
                RequireString(record, RecordFields.ReopenedWhen, findings);

                if (record.Status == RecordStatuses.Approved)
                {
                    RequireString(record, RecordFields.Choice, findings);
                }

                break;

            case RecordTypes.Topic:
                RequireDate(record, RecordFields.ReviewBy, findings);
                break;

            case RecordTypes.Requirement:
                RequireEnum(record, RecordFields.Kind, ["functional", "quality", "policy"], findings);
                RequireString(record, RecordFields.Statement, findings);
                RequireString(record, RecordFields.Rationale, findings);
                CheckSetBy(record, findings);

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
                    RequireString(record, RecordFields.DueBy, findings);
                }

                if (record.Has(RecordFields.Measure))
                {
                    RequireString(record, RecordFields.DueBy, findings);
                }

                if (record.Has(RecordFields.DueBy))
                {
                    RequireString(record, RecordFields.Measure, findings);
                }

                break;

            case RecordTypes.Problem:
                RequireIds(record, RecordFields.Affects, 1, findings);
                RequireString(record, RecordFields.Statement, findings);
                break;

            case RecordTypes.Feature:
                RequireEnum(record, RecordFields.Lifecycle, ["planned", "shipped", "later", "refused"], findings);
                RequireIds(record, RecordFields.Solves, 1, findings);

                if (record.Field(RecordFields.Lifecycle) == "refused")
                {
                    RequireString(record, RecordFields.RefusedBecause, findings);
                }

                if (record.Field(RecordFields.Lifecycle) == "planned")
                {
                    RequireString(record, RecordFields.PlannedFor, findings);
                }

                if (record.Has(RecordFields.Serves))
                {
                    findings.Add(new Finding("field-invalid", $"Feature '{record.Id}' names serves and advances holds its goal.", record.File, record.Line, SubjectOf(record)));
                }

                break;

            case RecordTypes.Actor:
                RequireEnum(record, RecordFields.Kind, ["person", "organization", "system"], findings);
                break;

            case RecordTypes.Role:
                RequirePattern(record, RecordFields.Scope, @"^[a-z0-9]+(-[a-z0-9]+)*$", "kebab-case", findings);
                RequireIds(record, RecordFields.Allows, 1, findings);
                break;

            case RecordTypes.Permission:
                RequireString(record, RecordFields.Meaning, findings);
                break;

            case RecordTypes.Journey:
                RequireIds(record, RecordFields.Actor, 1, findings);
                RequireIds(record, RecordFields.Serves, 1, findings);
                RequireArray(record, RecordFields.Steps, 1, findings);
                break;

            case RecordTypes.Module:
                RequireArray(record, RecordFields.DependsOn, 0, findings);
                break;

            case RecordTypes.Aggregate:
                // A store carries no lifecycle, so it carries no states.
                if (record.Field(RecordFields.Kind) is not "store")
                {
                    RequireArray(record, RecordFields.States, 1, findings);
                    RequireStateEntries(record, findings);
                }

                // A saga coordinates steps on other aggregates, with its exits and driver.
                if (record.Field(RecordFields.Kind) == "saga")
                {
                    RequireArray(record, RecordFields.Steps, 1, findings);
                    RequireArray(record, RecordFields.Exits, 1, findings);

                    if (!record.Has(RecordFields.DrivenBy))
                    {
                        findings.Add(new Finding("record-invalid", $"Aggregate '{record.Id}' is a saga with no drivenBy.", record.File, record.Line, SubjectOf(record)));
                    }
                }

                break;

            case RecordTypes.Rule:
                RequireEnum(record, RecordFields.Kind, ["invariant", "precondition", "calculation", "cross-aggregate", "atomicity", "reservation", "commercial-data", "default"], findings);
                RequireString(record, RecordFields.Statement, findings);
                RequireIds(record, RecordFields.Governs, 1, findings);

                // An atomicity rule names the aggregates it binds, so the use case's changes can equal them.
                if (record.Field(RecordFields.Kind) == "atomicity")
                {
                    RequireIds(record, RecordFields.Binds, 1, findings);
                }

                break;

            case RecordTypes.UseCase:
                RequireEnum(record, RecordFields.Kind, ["command", "query"], findings);
                // A retired use case names no entry point, and a use case only a reaction, a job,
                // a saga step or another use case runs has no door of its own: only the rest must
                // name one.
                if (record.Status != RecordStatuses.Retired && !RunTargets(model).Contains(record.Id))
                {
                    RequireIds(record, RecordFields.ReachedThrough, 1, findings);
                }
                CheckIdempotency(record, findings);
                CheckEffectFailurePolicy(record, findings);
                break;

            case RecordTypes.Reaction:
            case RecordTypes.Job:
                CheckLegacyEdges(record, [RecordFields.Runs, RecordFields.Sends, RecordFields.Emits, RecordFields.Calls], findings);
                RequireArray(record, RecordFields.Effects, 1, findings);
                CheckIdempotency(record, findings);
                CheckFailurePolicy(record, findings);
                break;

            case RecordTypes.Setting:
                CheckSetBy(record, findings);
                break;

            case RecordTypes.ExternalSystem:
                RequireIds(record, RecordFields.Actor, 1, findings);
                break;

            case RecordTypes.Result:
                RequireEnum(record, RecordFields.Outcome, ["success", "refusal"], findings);
                RequireString(record, RecordFields.Meaning, findings);

                if (record.Field(RecordFields.Outcome) == "refusal")
                {
                    RequireString(record, RecordFields.Code, findings);

                    if (record.Field(RecordFields.Code) is { } code
                        && (code.Length == 0
                            || !Identifiers.RefusalCode().IsMatch(code)
                            || Identifiers.RefusalCode().Match(code).Value != code))
                    {
                        findings.Add(new Finding("field-invalid", $"Result '{record.Id}' has code '{code}', which is not a refusal code.", record.File, record.Line, SubjectOf(record)));
                    }
                }

                break;

            case RecordTypes.Scenario:
                RequireString(record, RecordFields.Given, findings);
                RequireString(record, RecordFields.When, findings);
                RequireString(record, RecordFields.Then, findings);
                RequireString(record, RecordFields.Result, findings);
                break;

            case RecordTypes.Event:
                RequireString(record, RecordFields.Meaning, findings);
                RequireArray(record, RecordFields.Carries, 0, findings);
                break;

            case RecordTypes.EntryPoint:
                RequireEnum(record, RecordFields.Kind, ["browser", "api", "webhook", "device", "cli", "mcp", "served-content"], findings);
                break;

            case RecordTypes.Screen:
                RequireIds(record, RecordFields.EntryPoint, 1, findings);

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

            case RecordTypes.Exception:
                RequireEnum(record, RecordFields.Kind, ["departure", "gap"], findings);
                RequireString(record, RecordFields.Reason, findings);
                RequireDate(record, RecordFields.ExpiresOn, findings);
                CheckExceptionCovers(record, findings);
                CheckExceptionScope(record, findings);

                break;

            case RecordTypes.Term:
                if (record.Field(RecordFields.Meaning) is not { Length: > 0 })
                {
                    findings.Add(new Finding("record-invalid", $"Term '{record.Id}' has no meaning.", record.File, record.Line, SubjectOf(record)));
                }
                else if (record.Field(RecordFields.Meaning)!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length > 25)
                {
                    findings.Add(new Finding("field-invalid", $"Term '{record.Id}' has a meaning over 25 words.", record.File, record.Line, SubjectOf(record)));
                }

                break;
        }

        CheckLinkTypes(model, record, findings);
    }

    /// <summary>Reports an exception whose covers names no shared provision or no emitted finding code.</summary>
    private static void CheckExceptionCovers(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.Kind) == "departure")
        {
            RequireString(record, RecordFields.Covers, findings);

            if (record.Field(RecordFields.Covers) is { } covers && !IsProvisionCitation(covers))
            {
                findings.Add(new Finding("field-invalid", $"Exception '{record.Id}' names '{covers}', which is not a shared provision.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }

        RequireArray(record, RecordFields.Covers, 1, findings);

        foreach (var code in record.Strings(RecordFields.Covers).Order(StringComparer.Ordinal))
        {
            if (!Checker.Codes.Contains(code))
            {
                findings.Add(new Finding("exception-finding-unknown", $"Exception '{record.Id}' names finding code '{code}', which no check emits.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports an exception naming neither the records nor the paths it covers.</summary>
    private static void CheckExceptionScope(Record record, List<Finding> findings)
    {
        if (record.Ids(RecordFields.Subject).Count == 0 && record.Ids(RecordFields.Paths).Count == 0)
        {
            findings.Add(new Finding("record-invalid", $"Exception '{record.Id}' names neither subject nor paths.", record.File, record.Line, SubjectOf(record)));
        }
    }

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
                    findings.Add(new Finding("field-invalid", $"Record '{record.Id}' links {property.Name} to '{target}', which is a {resolved.Type}.", record.File, record.Line, SubjectOf(record)));
                }

                // A journey cites the requirements it shows; every other cites names
                // a goal, problem, feature, constraint, research or decision.
                if (property.Name == RecordFields.Cites && resolved.Type == RecordTypes.Requirement && record.Type != RecordTypes.Journey)
                {
                    findings.Add(new Finding("field-invalid", $"Record '{record.Id}' links cites to '{target}', which is a requirement.", record.File, record.Line, SubjectOf(record)));
                }

                CheckFragment(model, record, target, baseId, resolved, findings);
            }
        }

        if (record.Type == RecordTypes.Journey
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
                    findings.Add(new Finding("field-invalid", $"Journey '{record.Id}' steps to '{id}', which is a {resolved.Type}.", record.File, record.Line, SubjectOf(record)));
                }

                if (step.TryGetProperty(RecordFields.Actor, out var actor)
                    && actor.ValueKind == JsonValueKind.String
                    && actor.GetString() is { } actorId
                    && model.ById.TryGetValue(actorId, out var actorRecord)
                    && actorRecord.Type != RecordTypes.Actor)
                {
                    findings.Add(new Finding("field-invalid", $"Journey '{record.Id}' names actor '{actorId}', which is a {actorRecord.Type}.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

    /// <summary>
    ///     Whether the link fragment names a journey step or an external-system
    ///     operation its record holds. The citation check reads only the base id,
    ///     so a missing step or operation is reported here.
    /// </summary>
    private static void CheckFragment(
        ProjectModel model, Record record, string target, string baseId, Record resolved, List<Finding> findings)
    {
        if (target == baseId)
        {
            return;
        }

        if (Identifiers.TrySplitStep(target, out _, out _))
        {
            if (resolved.Type == RecordTypes.Journey && !resolved.StepIds().Contains(target, StringComparer.Ordinal))
            {
                findings.Add(new Finding("reference-unknown", $"Record '{record.Id}' serves '{target}', which names no step of the journey.", record.File, record.Line, SubjectOf(record)));
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

    private static void CheckReplaces(ProjectModel model, Record record, List<Finding> findings)
    {
        if (record.Type != RecordTypes.Decision && record.Ids(RecordFields.Replaces).Any())
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' carries replaces and is no decision.", record.File, record.Line, SubjectOf(record)));
        }

        foreach (var target in record.Ids(RecordFields.Replaces))
        {
            if (model.ById.TryGetValue(target, out var resolved) && resolved.Type != record.Type)
            {
                findings.Add(new Finding("field-invalid", $"Record '{record.Id}' replaces '{target}', which is a {resolved.Type}.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    private static bool ValidDate(string text) =>
        IsoDate(text, out _)
        || DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _);

    private static bool IsProvisionCitation(string text) =>
        Identifiers.SharedCitation().IsMatch(text)
        && Identifiers.SharedCitation().Match(text).Value == text;

    private static void RequireString(Record record, string field, List<Finding> findings)
    {
        if (record.Field(field) is not { Length: > 0 })
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
    }

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
                findings.Add(new Finding("field-invalid", $"Aggregate '{record.Id}' holds a state without a name and a meaning.", record.File, record.Line, SubjectOf(record)));
                return;
            }
        }
    }

    private static void RequireArray(Record record, string field, int minimum, List<Finding> findings)
    {
        if (!record.Has(field))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (record.Raw.TryGetProperty(field, out var value) && value.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' holds {field} as no array.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (value.GetArrayLength() < minimum)
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' holds an empty {field}.", record.File, record.Line, SubjectOf(record)));
        }
    }

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

    private static void RequireEnum(Record record, string field, string[] values, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!values.Contains(value, StringComparer.Ordinal))
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' has {field} '{value}' outside {string.Join(", ", values)}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a setBy outside the decider, operator and tenant who sets the value.</summary>
    private static void CheckSetBy(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.SetBy) is { } setBy && setBy is not ("decider" or "operator" or "tenant"))
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' has setBy '{setBy}' outside decider, operator, tenant.", record.File, record.Line, SubjectOf(record)));
        }
    }

    private static void RequireBoolean(Record record, string field, List<Finding> findings)
    {
        if (!record.Has(field))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (record.Raw.TryGetProperty(field, out var value)
            && value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' holds {field} as no boolean.", record.File, record.Line, SubjectOf(record)));
        }
    }

    private static void RequireDate(Record record, string field, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!ValidDate(value))
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' has {field} '{value}', which is not a date.", record.File, record.Line, SubjectOf(record)));
        }
    }

    private static void RequirePattern(Record record, string field, string pattern, string shape, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!Regex.IsMatch(value, pattern))
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' has {field} '{value}', which is not {shape}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>
    ///     Flags the retired top-level edge fields on a reaction or job: edges live in
    ///     the effects array, under one grammar for every record that has edges.
    /// </summary>
    private static void CheckLegacyEdges(Record record, string[] fields, List<Finding> findings)
    {
        foreach (var field in fields)
        {
            if (record.Has(field))
            {
                findings.Add(new Finding("record-invalid", $"{Label(record.Type)} '{record.Id}' holds top-level {field} outside effects.", record.File, record.Line, SubjectOf(record)));
            }
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

    /// <summary>
    ///     The reaction or job failure handling: a mode object naming retry with its
    ///     attempts and delays, or skip-and-log, or discard. Prose and malformed values report.
    /// </summary>
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

        if (!policy.TryGetProperty(RecordFields.Then, out var then)
            || then.ValueKind != JsonValueKind.String
            || then.GetString() is not { Length: > 0 })
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} retry with no then.", record.File, record.Line, SubjectOf(record)));
        }
        else if (then.GetString() != "dead-letter")
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} then '{then.GetString()}' outside dead-letter.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>The retry object: positive attempts, ISO 8601 delays and a backoff of exponential or fixed.</summary>
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
