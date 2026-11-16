using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The record-graph checks: every behavior record links up to intent, every flow step and notification has a record serving or sending it, every setting default has a requirement behind it, and every scenario exercises a rule.</summary>
/// <follows>litespec/core/model#connect-every-behavior-record-to-intent</follows>
/// <follows>litespec/core/model#serve-every-flow-step</follows>
/// <follows>litespec/core/edges#describe-every-notification-with-audience-channel-and-consent</follows>
/// <follows>litespec/core/use-cases#tie-every-setting-default-to-a-requirement</follows>
/// <follows>litespec/core/use-cases#exercise-rules-from-scenarios</follows>
internal static class GraphChecks
{
    /// <summary>Reports a use case, reaction, job, notification or setting naming neither serves nor fulfills.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunOrphaned(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record =>
            RecordTypes.UpLinkedTypes.Contains(record.Type, StringComparer.Ordinal)))
        {
            if (record.Ids(RecordFields.Serves).Count == 0 && record.Ids(RecordFields.Fulfills).Count == 0)
            {
                findings.Add(new Finding("record-link-missing", $"{Label(record.Type)} '{record.Id}' names neither serves nor fulfills.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports a flow step served by no use case, reaction or job.</summary>
    /// <param name="model">The loaded project model that holds every flow and serving record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunStepUnserved(ProjectModel model, List<Finding> findings)
    {
        var served = model.Records
            .Where(record => RecordTypes.StepServingTypes.Contains(record.Type, StringComparer.Ordinal))
            .SelectMany(record => record.Ids(RecordFields.Serves))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var flow in model.Records.Where(record => record.Type == RecordTypes.Flow))
        {
            foreach (var step in flow.StepIds().Order(StringComparer.Ordinal))
            {
                if (!served.Contains(step))
                {
                    findings.Add(new Finding("step-use-case-missing", $"Flow step '{step}' is served by no {OrList(RecordTypes.StepServingTypes)}.", flow.File, flow.Line, step));
                }
            }
        }
    }

    /// <summary>Reports a setting that sets a default with no requirement behind it, and a setting no use case, reaction or job reads.</summary>
    /// <param name="model">The loaded project model that holds every setting record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunSettingDefault(ProjectModel model, List<Finding> findings)
    {
        var read = model.Records
            .Where(record => record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job)
            .SelectMany(record => record.Ids(RecordFields.Reads))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var setting in model.Records.Where(record => record.Type == RecordTypes.Setting))
        {
            var carries = setting.Raw.ValueKind == JsonValueKind.Object
                && setting.Raw.TryGetProperty(RecordFields.Default, out var value)
                && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);

            if (carries && setting.Ids(RecordFields.Fulfills).Count == 0)
            {
                findings.Add(new Finding("setting-requirement-missing", $"Setting '{setting.Id}' sets a default with no requirement behind it.", setting.File, setting.Line, SubjectOf(setting)));
            }

            CheckSettingShape(setting, findings);

            if (!read.Contains(setting.Id))
            {
                findings.Add(new Finding("setting-unused", $"Setting '{setting.Id}' is read by no use case, reaction or job.", setting.File, setting.Line, SubjectOf(setting)));
            }
        }
    }

    /// <summary>The value types a setting may declare, and the shape its default must take for each.</summary>
    private static readonly string[] SettingValueTypes = ["integer", "duration", "money", "boolean", "text", "choice"];

    /// <summary>
    ///     Reports a setting whose valueType is outside its set, a choice setting with no choices, and a default that does
    ///     not take its value type: an ISO 8601 duration, an amount with its currency, a whole number, true or false, text, or one of the choices.
    /// </summary>
    /// <param name="setting">The setting record whose value type and default are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSettingShape(Record setting, List<Finding> findings)
    {
        var valueType = Str(setting.Raw, RecordFields.ValueType);

        if (valueType is null)
        {
            return;
        }

        if (!SettingValueTypes.Contains(valueType, StringComparer.Ordinal))
        {
            findings.Add(new Finding("field-invalid", $"Setting '{setting.Id}' holds valueType '{valueType}' outside {string.Join(", ", SettingValueTypes)}.", setting.File, setting.Line, SubjectOf(setting)));
            return;
        }

        var choices = setting.Raw.ValueKind == JsonValueKind.Object
            && setting.Raw.TryGetProperty(RecordFields.Choices, out var list)
            && list.ValueKind == JsonValueKind.Array
                ? list.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.String).Select(entry => entry.GetString()!).ToList()
                : new List<string>();

        if (valueType == "choice" && choices.Count == 0)
        {
            findings.Add(new Finding("record-invalid", $"Setting '{setting.Id}' has valueType choice and names no choices.", setting.File, setting.Line, SubjectOf(setting)));
        }

        if (setting.Raw.ValueKind != JsonValueKind.Object
            || !setting.Raw.TryGetProperty(RecordFields.Default, out var value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return;
        }

        var fits = valueType switch
        {
            "duration" => value.ValueKind == JsonValueKind.String && value.GetString() is { } text && IsoDuration.IsMatch(text),
            "money" => value.ValueKind == JsonValueKind.Object
                && value.TryGetProperty(RecordFields.Amount, out var amount)
                && amount.ValueKind is JsonValueKind.Number or JsonValueKind.String
                && Str(value, RecordFields.Currency) is { Length: > 0 },
            "integer" => value.ValueKind == JsonValueKind.Number,
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "text" => value.ValueKind == JsonValueKind.String,
            _ => value.ValueKind == JsonValueKind.String && value.GetString() is { } choice && choices.Contains(choice, StringComparer.Ordinal),
        };

        if (!fits)
        {
            findings.Add(new Finding("field-invalid", $"Setting '{setting.Id}' holds a default that does not take its valueType {valueType}.", setting.File, setting.Line, SubjectOf(setting)));
        }
    }

    /// <summary>Reports a notification sent by no reaction, job or use case.</summary>
    /// <param name="model">The loaded project model that holds every notification and holder record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunNotificationSender(ProjectModel model, List<Finding> findings)
    {
        var sent = new HashSet<string>(StringComparer.Ordinal);

        foreach (var holder in model.Records.Where(record =>
            record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job))
        {
            foreach (var entry in EffectEntries(holder))
            {
                if (Str(entry, RecordFields.Sends) is { Length: > 0 } target)
                {
                    sent.Add(target);
                }
            }
        }

        foreach (var notification in model.Records.Where(record => record.Type == RecordTypes.Notification))
        {
            if (!sent.Contains(notification.Id))
            {
                findings.Add(new Finding("notification-effect-missing", $"Notification '{notification.Id}' is sent by no reaction, job or use case.", notification.File, notification.Line, SubjectOf(notification)));
            }
        }
    }

    /// <summary>
    ///     Reports a scenario exercise naming no rule, a refusal scenario that leaves out the rule
    ///     refusing with its result's code, and an input rule that no scenario of its use case exercises.
    /// </summary>
    /// <param name="model">The loaded project model that holds every scenario record and rule.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunExercises(ProjectModel model, List<Finding> findings)
    {
        var refusingRules = new Dictionary<string, List<Record>>(StringComparer.Ordinal);

        foreach (var rule in model.Records.Where(record => record.Type == RecordTypes.Rule && record.Field(RecordFields.Refusal) is { Length: > 0 }))
        {
            var code = rule.Field(RecordFields.Refusal)!;

            if (!refusingRules.TryGetValue(code, out var rules))
            {
                rules = [];
                refusingRules[code] = rules;
            }

            rules.Add(rule);
        }

        var exercisedByUseCase = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var scenario in model.Records.Where(record => record.Type == RecordTypes.Scenario))
        {
            foreach (var id in scenario.Ids(RecordFields.Exercises).Order(StringComparer.Ordinal))
            {
                if (!model.ById.TryGetValue(id, out var target))
                {
                    findings.Add(new Finding("reference-unknown", $"Scenario '{scenario.Id}' exercises '{id}', which no record holds.", scenario.File, scenario.Line, SubjectOf(scenario)));
                }
                else if (target.Type != RecordTypes.Rule)
                {
                    findings.Add(new Finding("field-invalid", $"Scenario '{scenario.Id}' exercises '{id}', which is a {target.Type}.", scenario.File, scenario.Line, SubjectOf(scenario)));
                }
            }

            if (UseCaseOfScenario(model, scenario) is not { } useCase)
            {
                continue;
            }

            if (!exercisedByUseCase.TryGetValue(useCase.Id, out var exercised))
            {
                exercised = new HashSet<string>(StringComparer.Ordinal);
                exercisedByUseCase[useCase.Id] = exercised;
            }

            exercised.UnionWith(scenario.Ids(RecordFields.Exercises));

            if (RefusalCodeOf(model, scenario) is not { } code || !refusingRules.TryGetValue(code, out var rules))
            {
                continue;
            }

            var named = scenario.Ids(RecordFields.Exercises).ToHashSet(StringComparer.Ordinal);

            foreach (var rule in rules.Where(candidate => !named.Contains(candidate.Id)).OrderBy(candidate => candidate.Id, StringComparer.Ordinal))
            {
                findings.Add(new Finding("scenario-rule-unexercised", $"Scenario '{scenario.Id}' expects refusal '{code}' but does not exercise rule '{rule.Id}', which refuses with it.", scenario.File, scenario.Line, SubjectOf(scenario)));
            }
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase).OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var exercised = exercisedByUseCase.TryGetValue(useCase.Id, out var found) ? found : new HashSet<string>(StringComparer.Ordinal);

            foreach (var rule in InputRules(useCase).Where(candidate => !exercised.Contains(candidate)).Order(StringComparer.Ordinal))
            {
                findings.Add(new Finding("scenario-rule-unexercised", $"Use case '{useCase.Id}' takes input rule '{rule}', which no scenario of the use case exercises.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }

        CheckGoverns(model, findings);
    }

    /// <summary>
    ///     Reports a rule whose governs names a use case its links do not reach, or a use case its links reach that governs
    ///     does not name. The links are an input rule, a consistency boundBy, a scenario exercises, and a result whose code
    ///     the rule refuses with.
    /// </summary>
    /// <param name="model">The loaded project model that holds every rule, use case, scenario and result.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckGoverns(ProjectModel model, List<Finding> findings)
    {
        var reached = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        void Reach(string rule, string useCase)
        {
            if (!reached.TryGetValue(rule, out var useCases))
            {
                useCases = new HashSet<string>(StringComparer.Ordinal);
                reached[rule] = useCases;
            }

            useCases.Add(useCase);
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            foreach (var rule in InputRules(useCase))
            {
                Reach(Identifiers.BaseId(rule), useCase.Id);
            }

            if (useCase.Raw.ValueKind == JsonValueKind.Object
                && useCase.Raw.TryGetProperty(RecordFields.Consistency, out var consistency)
                && consistency.ValueKind == JsonValueKind.Object)
            {
                foreach (var rule in LinkStrings(consistency, RecordFields.BoundBy))
                {
                    Reach(Identifiers.BaseId(rule), useCase.Id);
                }
            }
        }

        foreach (var scenario in model.Records.Where(record => record.Type == RecordTypes.Scenario))
        {
            if (UseCaseOfScenario(model, scenario) is not { } useCase)
            {
                continue;
            }

            foreach (var rule in scenario.Ids(RecordFields.Exercises))
            {
                Reach(rule, useCase.Id);
            }
        }

        foreach (var result in model.Records.Where(record => record.Type == RecordTypes.Result && record.Field(RecordFields.Outcome) == "refusal"))
        {
            if (result.Field(RecordFields.Code) is not { Length: > 0 } code
                || !Identifiers.TrySplit(result.Id, out _, out var home, out _)
                || home is not { Length: > 0 }
                || ModelBuilder.UseCaseOf(model, home) is not { } owner)
            {
                continue;
            }

            foreach (var rule in model.Records.Where(record => record.Type == RecordTypes.Rule && record.Field(RecordFields.Refusal) == code))
            {
                Reach(rule.Id, owner.Id);
            }
        }

        foreach (var rule in model.Records.Where(record => record.Type == RecordTypes.Rule).OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var linked = reached.TryGetValue(rule.Id, out var useCases) ? useCases : new HashSet<string>(StringComparer.Ordinal);
            var governed = GovernedOfType(model, rule, RecordTypes.UseCase);

            if (!governed.SetEquals(linked))
            {
                findings.Add(new Finding("rule-governs-drift", $"Rule '{rule.Id}' governs '{string.Join("', '", governed.Order(StringComparer.Ordinal))}' but its links reach '{string.Join("', '", linked.Order(StringComparer.Ordinal))}'.", rule.File, rule.Line, SubjectOf(rule)));
            }

            // An aggregate or store is reached when the rule binds it, or a use case reaching the rule writes it.
            var reachedAggregatesAndStores = rule.Ids(RecordFields.Binds).ToHashSet(StringComparer.Ordinal);

            foreach (var useCaseId in linked)
            {
                if (model.ById.TryGetValue(useCaseId, out var reachingUseCase))
                {
                    reachedAggregatesAndStores.UnionWith(reachingUseCase.Ids(RecordFields.Writes));
                }
            }

            var governedAggregatesAndStores = GovernedOfType(model, rule, RecordTypes.Aggregate);
            governedAggregatesAndStores.UnionWith(GovernedOfType(model, rule, RecordTypes.Store));

            if (!governedAggregatesAndStores.SetEquals(reachedAggregatesAndStores))
            {
                findings.Add(new Finding("rule-governs-drift", $"Rule '{rule.Id}' governs aggregates and stores '{string.Join("', '", governedAggregatesAndStores.Order(StringComparer.Ordinal))}' but its links reach '{string.Join("', '", reachedAggregatesAndStores.Order(StringComparer.Ordinal))}'.", rule.File, rule.Line, SubjectOf(rule)));
            }
        }
    }

    /// <summary>The governs identifiers of a rule that resolve to records of one type.</summary>
    /// <param name="model">The loaded project model that resolves each governed identifier.</param>
    /// <param name="rule">The rule record whose governs field is read.</param>
    /// <param name="type">The record type the governed identifiers must resolve to.</param>
    /// <returns>The governed identifiers of that type.</returns>
    private static HashSet<string> GovernedOfType(ProjectModel model, Record rule, string type) =>
        rule.Ids(RecordFields.Governs)
            .Where(id => model.ById.TryGetValue(Identifiers.BaseId(id), out var target) && target.Type == type)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>The use case whose name a scenario's identifier home carries.</summary>
    /// <param name="model">The loaded project model that resolves use cases by name.</param>
    /// <param name="scenario">The scenario record whose home is read.</param>
    /// <returns>The use case the scenario belongs to, or null when its home names none.</returns>
    private static Record? UseCaseOfScenario(ProjectModel model, Record scenario) =>
        Identifiers.TrySplit(scenario.Id, out _, out var home, out _) && home is { Length: > 0 }
            ? ModelBuilder.UseCaseOf(model, home)
            : null;

    /// <summary>The refusal code a scenario's expected result carries.</summary>
    /// <param name="model">The loaded project model that resolves the expected result.</param>
    /// <param name="scenario">The scenario record whose result field is read.</param>
    /// <returns>The refusal code of the result, or null when the result is a success or names no result.</returns>
    private static string? RefusalCodeOf(ProjectModel model, Record scenario)
    {
        if (scenario.Field(RecordFields.Result) is not { Length: > 0 } resultId
            || !model.ById.TryGetValue(resultId, out var result)
            || result.Field(RecordFields.Outcome) != "refusal")
        {
            return null;
        }

        return result.Field(RecordFields.Code);
    }

    /// <summary>The rule identifiers each input of a use case names.</summary>
    /// <param name="useCase">The use case record whose input array is read.</param>
    /// <returns>The rule identifiers named by its inputs, in written order.</returns>
    private static IEnumerable<string> InputRules(Record useCase)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Input, out var inputs)
            || inputs.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return inputs.EnumerateArray().SelectMany(input => LinkStrings(input, RecordFields.Rules));
    }
}
