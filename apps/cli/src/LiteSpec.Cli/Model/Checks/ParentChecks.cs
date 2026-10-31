using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The parent check: every record with a required written parent names it.</summary>
/// <follows>litespec/core/model#give-every-record-one-parent</follows>
internal static class ParentChecks
{
    /// <summary>Types whose parent is the identifier home, so a missing home is reported here.</summary>
    private static readonly HashSet<string> HomeParentedTypes = new(StringComparer.Ordinal)
    {
        "use-case", "reaction", "job", "notification", "setting", "aggregate",
        "rule", "event", "result", "scenario",
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

        foreach (var record in model.Records)
        {
            if (record.Status == RecordStatuses.Retired || record.Derived)
            {
                continue;
            }

            switch (record.Type)
            {
                case "feature":
                    CheckFeatureParent(record, findings);
                    break;
                case "requirement":
                    CheckRequirementParent(model, record, findings);
                    break;
                case "journey":
                    CheckJourneyParent(model, record, findings);
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
                case "aggregate":
                    CheckModuleHome(model, record, findings);
                    break;
                case "rule":
                    CheckRuleHome(model, record, aggregates, findings);
                    break;
                case "event":
                    CheckEventHome(record, aggregates, findings);
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>Reports a feature solving no problem, refused or not.</summary>
    /// <param name="record">The feature record whose solves field is inspected.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFeatureParent(Record record, List<Finding> findings)
    {
        if (record.Ids(RecordFields.Solves).Count == 0)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Feature '{record.Id}' has no parent and 'solves' names no problem.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a requirement whose topic holds no topic record.</summary>
    /// <param name="model">The loaded project model that resolves the topic record.</param>
    /// <param name="record">The requirement record whose home names its topic.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRequirementParent(ProjectModel model, Record record, List<Finding> findings)
    {
        // A malformed identifier is identifier-invalid, so the parent check stays silent here.
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            return;
        }

        if (!model.ById.TryGetValue($"topic/{home}", out var topic) || topic.Type != RecordTypes.Topic)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Requirement '{record.Id}' has no parent and its home '{home}' names no topic.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a journey serving no goal.</summary>
    /// <param name="model">The loaded project model that resolves each serves target.</param>
    /// <param name="record">The journey record whose serves field is inspected.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckJourneyParent(ProjectModel model, Record record, List<Finding> findings)
    {
        var serves = record.Ids(RecordFields.Serves);

        if (serves.Count == 0)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Journey '{record.Id}' has no parent and 'serves' names no goal.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (serves.Any(target =>
            model.ById.TryGetValue(target, out var resolved) && resolved.Type == RecordTypes.Goal))
        {
            return;
        }

        // A dangling serves target is reference-unknown, so one defect draws one finding.
        if (serves.Any(target => !ServesResolves(model, target)))
        {
            return;
        }

        findings.Add(new Finding(
            "record-parent-missing",
            $"Journey '{record.Id}' has no parent and 'serves' names no goal.",
            record.File, record.Line, SubjectOf(record)));
    }

    /// <summary>Whether the serves target names a record or an existing journey step.</summary>
    /// <param name="model">The loaded project model that resolves the target record and its journey steps.</param>
    /// <param name="target">The serves identifier under test.</param>
    /// <returns>True when the target names a record or a step of an existing journey; otherwise false.</returns>
    private static bool ServesResolves(ProjectModel model, string target)
    {
        if (model.ById.ContainsKey(target))
        {
            return true;
        }

        return Identifiers.TrySplitStep(target, out var journeyId, out _)
            && model.ById.TryGetValue(journeyId, out var journey)
            && journey.StepIds().Contains(target, StringComparer.Ordinal);
    }

    /// <summary>Reports a scenario naming no result, else a use case no record holds.</summary>
    /// <param name="model">The loaded project model that resolves the use case named by the home.</param>
    /// <param name="record">The scenario record whose result field and home are inspected.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckScenarioParent(ProjectModel model, Record record, List<Finding> findings)
    {
        if (record.Ids(RecordFields.Result).Count == 0)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Scenario '{record.Id}' has no parent and 'result' names no result.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Scenario '{record.Id}' has no parent and its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (ModelBuilder.UseCaseOf(model, home) is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Scenario '{record.Id}' has no parent and its home '{home}' names no use case.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a screen naming no entry point, else one disagreeing with its home.</summary>
    /// <param name="model">The loaded project model that resolves each entry point identifier.</param>
    /// <param name="record">The screen record whose entry point field and home are inspected.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckScreenParent(ProjectModel model, Record record, List<Finding> findings)
    {
        var points = record.Ids(RecordFields.EntryPoint);

        if (points.Count == 0)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Screen '{record.Id}' has no parent and 'entryPoint' names no entry point.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            return;
        }

        if (points.Any(point => point == $"entry-point/{home}"))
        {
            return;
        }

        // A dangling entry point is reference-unknown, so one defect draws one finding.
        if (points.Any(point => !model.ById.ContainsKey(point)))
        {
            return;
        }

        findings.Add(new Finding(
            "record-parent-missing",
            $"Screen '{record.Id}' has no parent and 'entryPoint' '{points[0]}' disagrees with its home '{home}'.",
            record.File, record.Line, SubjectOf(record)));
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
                "record-parent-missing",
                $"Result '{record.Id}' has no parent and its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (ModelBuilder.UseCaseOf(model, home) is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Result '{record.Id}' has no parent and its home '{home}' names no use case.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a module-homed record naming a module no record holds.</summary>
    /// <param name="model">The loaded project model that resolves the module named by the home.</param>
    /// <param name="record">The module-homed record whose home is inspected.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleHome(ProjectModel model, Record record, List<Finding> findings)
    {
        var label = record.Type switch
        {
            RecordTypes.UseCase => "Use case",
            RecordTypes.Reaction => "Reaction",
            RecordTypes.Job => "Job",
            RecordTypes.Notification => "Notification",
            RecordTypes.Setting => "Setting",
            _ => "Aggregate",
        };

        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"{label} '{record.Id}' has no parent and its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (model.ById.ContainsKey($"module/{home}"))
        {
            return;
        }

        findings.Add(new Finding(
            "record-parent-missing",
            $"{label} '{record.Id}' has no parent and its home '{home}' names no module.",
            record.File, record.Line, SubjectOf(record)));
    }

    /// <summary>Reports a rule naming a module or aggregate no record holds.</summary>
    /// <param name="model">The loaded project model that resolves the module named by the home.</param>
    /// <param name="record">The rule record whose home is inspected.</param>
    /// <param name="aggregates">The aggregate names held by the project.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRuleHome(ProjectModel model, Record record, HashSet<string> aggregates, List<Finding> findings)
    {
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Rule '{record.Id}' has no parent and its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (model.ById.ContainsKey($"module/{home}") || aggregates.Contains(home))
        {
            return;
        }

        findings.Add(new Finding(
            "record-parent-missing",
            $"Rule '{record.Id}' has no parent and its home '{home}' names no module or aggregate.",
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
                "record-parent-missing",
                $"Event '{record.Id}' has no parent and its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!aggregates.Contains(home))
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Event '{record.Id}' has no parent and its home '{home}' names no aggregate.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>
    ///     Reports a record citing, fulfilling or serving a record that changed
    ///     after its approval: tier A while the record is a draft, tier G once
    ///     approved. The flag stands until the changed record is re-approved.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and its approval state.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunParentChanged(ProjectModel model, List<Finding> findings)
    {
        var changed = model.Records
            .Where(record => !record.Derived
                && record.Status is RecordStatuses.Approved or RecordStatuses.Reviewed
                && record.ApprovedHash is { Length: > 0 }
                && Checker.SemanticHash(record) != record.ApprovedHash)
            .Select(record => record.Id)
            .ToHashSet(StringComparer.Ordinal);

        if (changed.Count == 0)
        {
            return;
        }

        foreach (var record in model.Records.Where(record => !record.Derived && record.Status != RecordStatuses.Retired))
        {
            foreach (var field in new[] { RecordFields.Cites, RecordFields.Fulfills, RecordFields.Serves })
            {
                foreach (var target in record.Ids(field).Order(StringComparer.Ordinal))
                {
                    var baseId = Identifiers.BaseId(target);

                    if (baseId == record.Id || !changed.Contains(baseId))
                    {
                        continue;
                    }

                    findings.Add(new Finding(
                        "parent-reopened",
                        $"Record '{record.Id}' {field} '{target}' which differs after approval.",
                        record.File, record.Line, SubjectOf(record))
                    {
                        TierOverride = record.Status == RecordStatuses.Draft ? "A" : "G",
                    });
                }
            }
        }
    }
}
