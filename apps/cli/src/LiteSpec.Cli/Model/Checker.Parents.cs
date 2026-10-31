namespace LiteSpec.Model;

/// <summary>The parent check: every record with a required written parent names it.</summary>
/// <follows>litespec/core/model#give-every-record-one-parent</follows>
static partial class Checker
{
    /// <summary>Types whose parent is the identifier home, so a missing home is reported here.</summary>
    private static readonly HashSet<string> HomeParentedTypes = new(StringComparer.Ordinal)
    {
        "use-case", "reaction", "job", "notification", "setting", "aggregate",
        "rule", "event", "result", "scenario",
    };

    /// <summary>Whether the record reports a missing identifier home through the parent check.</summary>
    private static bool ReportsMissingHome(Record record) => HomeParentedTypes.Contains(record.Type);

    /// <summary>Reports a record whose required parent is missing, at most one finding per record.</summary>
    private static void CheckMissingParent(ProjectModel model, List<Finding> findings)
    {
        var aggregates = new HashSet<string>(StringComparer.Ordinal);

        foreach (var aggregate in model.Records.Where(record => record.Type == "aggregate"))
        {
            if (Identifiers.TrySplit(aggregate.Id, out _, out _, out var name))
            {
                aggregates.Add(name);
            }
        }

        foreach (var record in model.Records)
        {
            if (record.Status == "retired" || record.Derived)
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
    private static void CheckFeatureParent(Record record, List<Finding> findings)
    {
        if (record.Ids("solves").Count == 0)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Feature '{record.Id}' names no parent: 'solves' names no problem.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a requirement whose topic holds no topic record.</summary>
    private static void CheckRequirementParent(ProjectModel model, Record record, List<Finding> findings)
    {
        // A malformed identifier is identifier-invalid, so the parent check stays silent here.
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            return;
        }

        if (!model.ById.TryGetValue($"topic/{home}", out var topic) || topic.Type != "topic")
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Requirement '{record.Id}' names no parent: its home '{home}' names no topic.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a journey serving no goal.</summary>
    private static void CheckJourneyParent(ProjectModel model, Record record, List<Finding> findings)
    {
        var serves = record.Ids("serves");

        if (serves.Count == 0)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Journey '{record.Id}' names no parent: 'serves' names no goal.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (serves.Any(target =>
            model.ById.TryGetValue(target, out var resolved) && resolved.Type == "goal"))
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
            $"Journey '{record.Id}' names no parent: 'serves' names no goal.",
            record.File, record.Line, SubjectOf(record)));
    }

    /// <summary>Whether the serves target names a record or an existing journey step.</summary>
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
    private static void CheckScenarioParent(ProjectModel model, Record record, List<Finding> findings)
    {
        if (record.Ids("result").Count == 0)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Scenario '{record.Id}' names no parent: 'result' names no result.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Scenario '{record.Id}' names no parent: its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (ModelBuilder.UseCaseOf(model, home) is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Scenario '{record.Id}' names no parent: its home '{home}' names no use case.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a screen naming no entry point, else one disagreeing with its home.</summary>
    private static void CheckScreenParent(ProjectModel model, Record record, List<Finding> findings)
    {
        var points = record.Ids("entryPoint");

        if (points.Count == 0)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Screen '{record.Id}' names no parent: 'entryPoint' names no entry point.",
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
            $"Screen '{record.Id}' names no parent: 'entryPoint' '{points[0]}' disagrees with its home '{home}'.",
            record.File, record.Line, SubjectOf(record)));
    }

    /// <summary>Reports a result naming a use case no record holds.</summary>
    private static void CheckResultParent(ProjectModel model, Record record, List<Finding> findings)
    {
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Result '{record.Id}' names no parent: its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (ModelBuilder.UseCaseOf(model, home) is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Result '{record.Id}' names no parent: its home '{home}' names no use case.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a module-homed record naming a module no record holds.</summary>
    private static void CheckModuleHome(ProjectModel model, Record record, List<Finding> findings)
    {
        var label = record.Type switch
        {
            "use-case" => "Use case",
            "reaction" => "Reaction",
            "job" => "Job",
            "notification" => "Notification",
            "setting" => "Setting",
            _ => "Aggregate",
        };

        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"{label} '{record.Id}' names no parent: its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (model.ById.ContainsKey($"module/{home}"))
        {
            return;
        }

        findings.Add(new Finding(
            "record-parent-missing",
            $"{label} '{record.Id}' names no parent: its home '{home}' names no module.",
            record.File, record.Line, SubjectOf(record)));
    }

    /// <summary>Reports a rule naming a module or aggregate no record holds.</summary>
    private static void CheckRuleHome(ProjectModel model, Record record, HashSet<string> aggregates, List<Finding> findings)
    {
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Rule '{record.Id}' names no parent: its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (model.ById.ContainsKey($"module/{home}") || aggregates.Contains(home))
        {
            return;
        }

        findings.Add(new Finding(
            "record-parent-missing",
            $"Rule '{record.Id}' names no parent: its home '{home}' names no module or aggregate.",
            record.File, record.Line, SubjectOf(record)));
    }

    /// <summary>Reports an event naming an aggregate no record holds.</summary>
    private static void CheckEventHome(Record record, HashSet<string> aggregates, List<Finding> findings)
    {
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _) || home is null)
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Event '{record.Id}' names no parent: its identifier names no home.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!aggregates.Contains(home))
        {
            findings.Add(new Finding(
                "record-parent-missing",
                $"Event '{record.Id}' names no parent: its home '{home}' names no aggregate.",
                record.File, record.Line, SubjectOf(record)));
        }
    }
}
