using System.Text.Json;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.RecordShape.Schema;

/// <summary>The schema reference resolution: every link names a record of the type its field allows, every use-case step cites only rules, results, settings and events, and every fragment names an operation its system holds.</summary>
/// <follows>litespec/core/model#write-links-on-the-record-that-points</follows>
internal static class SchemaLinkResolution
{
    /// <summary>Reports each link whose target record type the linking field forbids.</summary>
    /// <param name="model">The loaded project model that resolves each link target.</param>
    /// <param name="record">The record whose links are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckLinkTypes(ProjectModel model, Record record, List<Finding> findings)
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

                // A flow shows the requirements it carries, and a technical decision serves
                // the requirements it names in basedOn; every other basedOn names no requirement.
                if (property.Name == RecordFields.BasedOn && resolved.Type == RecordTypes.Requirement && record.Type != RecordTypes.Flow && !servesRequirement)
                {
                    findings.Add(new Finding("record-invalid", $"Record '{record.Id}' links basedOn to '{target}', which is a requirement.", record.File, record.Line, SubjectOf(record)));
                }

                CheckFragment(model, record, target, baseId, resolved, findings);
            }
        }

        if (record.Type == RecordTypes.UseCase
            && record.Raw.TryGetProperty(RecordFields.Steps, out var useCaseSteps)
            && useCaseSteps.ValueKind == JsonValueKind.Array)
        {
            var appliedTypes = new HashSet<string>(StringComparer.Ordinal)
            {
                RecordTypes.Rule, RecordTypes.Result, RecordTypes.Setting, RecordTypes.DomainEvent,
                RecordTypes.ReadModel, RecordTypes.ExternalSystem, RecordTypes.Aggregate,
            };

            foreach (var step in useCaseSteps.EnumerateArray())
            {
                if (step.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                // A step applies the rules, results, settings and events it names, and nothing else.
                foreach (var cited in LinkStrings(step, RecordFields.Cites))
                {
                    if (model.ById.TryGetValue(Identifiers.BaseId(cited), out var citedRecord) && !appliedTypes.Contains(citedRecord.Type))
                    {
                        findings.Add(new Finding("record-invalid", $"Use case '{record.Id}' step cites '{cited}', which is a {citedRecord.Type}, not a rule, result, setting, domain event, read model, external system or aggregate.", record.File, record.Line, SubjectOf(record)));
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
    ///     Whether the link fragment names an external-system operation its record holds.
    ///     The citation check reads only the base id, so a missing operation is reported
    ///     here; a step in the old flow/name#step form is reported there instead, so one
    ///     citation draws one finding.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the link target.</param>
    /// <param name="record">The record that holds the link.</param>
    /// <param name="target">The full link target including any step or operation fragment.</param>
    /// <param name="baseId">The target record identifier without its fragment.</param>
    /// <param name="resolved">The record the base identifier resolves to.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckFragment(
        ProjectModel model, Record record, string target, string baseId, Record resolved, List<Finding> findings)
    {
        if (target == baseId)
        {
            return;
        }

        if (Identifiers.TrySplitStep(target, out _, out _))
        {
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
    internal static IReadOnlyList<string> OperationNames(Record system)
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
}
