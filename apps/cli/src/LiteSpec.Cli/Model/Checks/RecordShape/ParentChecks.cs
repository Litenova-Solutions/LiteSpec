using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Model.Screens;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The parent check: every record with a required written parent names it.</summary>
/// <follows>litespec/core/model#give-every-record-one-parent</follows>
internal static class ParentChecks
{
    /// <summary>Types whose parent is the identifier home, so a missing home is reported here.</summary>
    private static readonly HashSet<string> HomeParentedTypes = new(StringComparer.Ordinal)
    {
        "use-case", "reaction", "job", "notification", "setting", "read-model", "reference-data", "aggregate",
        "rule", "domain-event", "result", "scenario", "screen",
    };

    /// <summary>Whether the record reports a missing identifier home through the parent check.</summary>
    /// <param name="record">The record whose type decides where a missing home is reported.</param>
    /// <returns>True when the record type takes its parent from the identifier home; otherwise false.</returns>
    internal static bool ReportsMissingHome(Record record) => HomeParentedTypes.Contains(record.Type);

    /// <summary>Reports a record whose required parent is missing, at most one finding per record.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var aggregates = new HashSet<string>(StringComparer.Ordinal);

        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            if (Identifiers.TrySplit(aggregate.Id, out _, out _, out var name))
            {
                aggregates.Add(name);
            }
        }

        var unreadableModules = UnreadableModules(model);

        foreach (var record in model.Records)
        {
            if (record.Derived)
            {
                continue;
            }

            switch (record.Type)
            {
                case "feature":
                    CheckFeatureParent(model, record, findings);
                    break;
                case "flow":
                    CheckFlowParent(model, record, findings);
                    break;
                case "scenario":
                    CheckScenarioParent(model, record, findings);
                    break;
                case "screen":
                    CheckScreenParent(model, record, findings);
                    break;
                case "result":
                    CheckResultParent(model, record, findings);
                    break;
                case "use-case":
                case "reaction":
                case "job":
                case "notification":
                case "setting":
                case "read-model":
                case "reference-data":
                case "aggregate":
                    CheckModuleHome(model, record, unreadableModules, findings);
                    break;
                case "rule":
                    CheckRuleHome(model, record, aggregates, unreadableModules, findings);
                    break;
                case "domain-event":
                    CheckEventHome(record, aggregates, findings);
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>Reports a feature outside the product's features folder. A feature stands on its own, so its parent is the product, and the problems it solves are named in solves.</summary>
    /// <param name="model">The loaded project model that holds the feature records.</param>
    /// <param name="record">The feature record whose file path is read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFeatureParent(ProjectModel model, Record record, List<Finding> findings)
    {
        if (!record.File.Replace('\\', '/').Contains("/intent/features/", StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Feature '{record.Id}' sits outside the product's intent/features folder, so it has no parent product.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a flow whose reason, serves or answers, names no goal, problem or constraint.</summary>
    /// <param name="model">The loaded project model that resolves each reason target.</param>
    /// <param name="record">The flow record whose serves and answers fields are inspected.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFlowParent(ProjectModel model, Record record, List<Finding> findings)
    {
        var reasons = record.Ids(RecordFields.Serves).Concat(record.Ids(RecordFields.Answers)).ToList();

        if (reasons.Count == 0)
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Flow '{record.Id}' has no parent and names neither 'serves' nor 'answers'.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (reasons.Any(target =>
            model.ById.TryGetValue(target, out var resolved)
            && resolved.Type is RecordTypes.Goal or RecordTypes.Problem or RecordTypes.Constraint))
        {
            return;
        }

        // A dangling reason target is reference-unknown, so one defect draws one finding.
        if (reasons.Any(target => !ServesResolves(model, target)))
        {
            return;
        }

        findings.Add(new Finding(
            "record-placement-invalid",
            $"Flow '{record.Id}' has no parent and names no goal, problem or constraint.",
            record.File, record.Line, SubjectOf(record)));
    }

    /// <summary>Whether the serves target names a record or an existing flow step.</summary>
    /// <param name="model">The loaded project model that resolves the target record and its flow steps.</param>
    /// <param name="target">The serves identifier under test.</param>
    /// <returns>True when the target names a record or a step of an existing flow; otherwise false.</returns>
    private static bool ServesResolves(ProjectModel model, string target)
    {
        if (model.ById.ContainsKey(target))
        {
            return true;
        }

        return Identifiers.TrySplitStep(target, out var flowId, out _)
            && model.ById.TryGetValue(flowId, out var flow)
            && flow.StepIds().Contains(target, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Reports a scenario whose home names no use case, reaction or job. A use case scenario names its result;
    ///     a reaction or job scenario names its result or its outcome.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the use case, reaction or job named by the home.</param>
    /// <param name="record">The scenario record whose result, outcome and home are inspected.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckScenarioParent(ProjectModel model, Record record, List<Finding> findings)
    {
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Scenario '{record.Id}' has no parent and its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        // A scenario stored in a reaction or job file belongs to that record, even when a use case shares the name.
        var inAutomationFile = model.Records.Any(candidate =>
            candidate.File == record.File
            && candidate.Type is RecordTypes.Reaction or RecordTypes.Job);

        if (!inAutomationFile && ModelBuilder.UseCaseOf(model, home) is not null)
        {
            if (record.Ids(RecordFields.Result).Count == 0)
            {
                findings.Add(new Finding(
                    "record-placement-invalid",
                    $"Scenario '{record.Id}' has no parent and 'result' names no result.",
                    record.File, record.Line, SubjectOf(record)));
            }

            return;
        }

        var automation = inAutomationFile || model.Records.Any(candidate =>
            candidate.Type is RecordTypes.Reaction or RecordTypes.Job
            && Identifiers.TrySplit(candidate.Id, out _, out _, out var name)
            && name == home);

        if (!automation)
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Scenario '{record.Id}' has no parent and its home '{home}' names no use case, reaction or job.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!record.Has(RecordFields.Result) && !record.Has(RecordFields.Outcome))
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Scenario '{record.Id}' has no parent and names neither result nor outcome.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a screen whose home names no entry point.</summary>
    /// <param name="model">The loaded project model that resolves each entry point identifier.</param>
    /// <param name="record">The screen record whose home is inspected.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckScreenParent(ProjectModel model, Record record, List<Finding> findings)
    {
        if (FrontendRecords.EntryPointOf(record) is not { } entryPoint)
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Screen '{record.Id}' has no parent and its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!model.ById.ContainsKey(entryPoint))
        {
            var home = entryPoint[(RecordTypes.EntryPoint.Length + 1)..];

            findings.Add(new Finding(
                "record-placement-invalid",
                $"Screen '{record.Id}' has no parent and its home '{home}' names no entry point.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a result naming a use case no record holds.</summary>
    /// <param name="model">The loaded project model that resolves the use case named by the home.</param>
    /// <param name="record">The result record whose home is inspected.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckResultParent(ProjectModel model, Record record, List<Finding> findings)
    {
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Result '{record.Id}' has no parent and its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (ModelBuilder.UseCaseOf(model, home) is null)
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Result '{record.Id}' has no parent and its home '{home}' names no use case.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Modules whose own record file the reader could not read, so their children report no missing parent.</summary>
    /// <param name="model">The loaded project model whose problems name the files the reader could not read.</param>
    /// <returns>The module names whose module file carries a record-invalid problem.</returns>
    private static HashSet<string> UnreadableModules(ProjectModel model)
    {
        var unreadable = new HashSet<string>(StringComparer.Ordinal);

        foreach (var problem in model.Problems)
        {
            if (problem.Code != "record-invalid")
            {
                continue;
            }

            if (!string.Equals(Path.GetFileName(problem.Path), "module.yaml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (RecordPlacement.ModuleOf(problem.Path, model.Config) is { } module)
            {
                unreadable.Add(module);
            }
        }

        return unreadable;
    }

    /// <summary>Reports a module-homed record naming a module no record holds, unless the module file could not be read.</summary>
    /// <param name="model">The loaded project model that resolves the module named by the home.</param>
    /// <param name="record">The module-homed record whose home is inspected.</param>
    /// <param name="unreadableModules">The modules whose own record file the reader could not read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleHome(ProjectModel model, Record record, HashSet<string> unreadableModules, List<Finding> findings)
    {
        var label = record.Type switch
        {
            RecordTypes.UseCase => "Use case",
            RecordTypes.Reaction => "Reaction",
            RecordTypes.Job => "Job",
            RecordTypes.Notification => "Notification",
            RecordTypes.Setting => "Setting",
            RecordTypes.ReadModel => "Read model",
            RecordTypes.ReferenceData => "Reference data",
            _ => "Aggregate",
        };

        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"{label} '{record.Id}' has no parent and its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (model.ById.ContainsKey($"module/{home}"))
        {
            return;
        }

        // The module file carries the defect, so one defect draws one finding.
        if (unreadableModules.Contains(home))
        {
            return;
        }

        findings.Add(new Finding(
            "record-placement-invalid",
            $"{label} '{record.Id}' has no parent and its home '{home}' names no module.",
            record.File, record.Line, SubjectOf(record)));
    }

    /// <summary>Reports a rule naming as its home no module, aggregate or product a record holds, unless the module file could not be read.</summary>
    /// <param name="model">The loaded project model that resolves the module named by the home.</param>
    /// <param name="record">The rule record whose home is inspected.</param>
    /// <param name="aggregates">The aggregate names held by the project.</param>
    /// <param name="unreadableModules">The modules whose own record file the reader could not read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRuleHome(ProjectModel model, Record record, HashSet<string> aggregates, HashSet<string> unreadableModules, List<Finding> findings)
    {
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Rule '{record.Id}' has no parent and its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (model.ById.ContainsKey($"module/{home}") || aggregates.Contains(home) || model.ById.ContainsKey($"product/{home}"))
        {
            return;
        }

        // The module file carries the defect, so one defect draws one finding.
        if (unreadableModules.Contains(home))
        {
            return;
        }

        findings.Add(new Finding(
            "record-placement-invalid",
            $"Rule '{record.Id}' has no parent and its home '{home}' names no module, aggregate or product.",
            record.File, record.Line, SubjectOf(record)));
    }

    /// <summary>Reports an event naming an aggregate no record holds.</summary>
    /// <param name="record">The event record whose home is inspected.</param>
    /// <param name="aggregates">The aggregate names held by the project.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEventHome(Record record, HashSet<string> aggregates, List<Finding> findings)
    {
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Event '{record.Id}' has no parent and its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!aggregates.Contains(home))
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Event '{record.Id}' has no parent and its home '{home}' names no aggregate.",
                record.File, record.Line, SubjectOf(record)));
        }
    }
}
