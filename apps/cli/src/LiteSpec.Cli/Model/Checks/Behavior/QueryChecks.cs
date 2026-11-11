using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The query checks: each query use case declares its output in include groups and its paging, and widens a group only through a requirement.</summary>
/// <follows>litespec/core/use-cases#declare-every-query-output</follows>
/// <follows>litespec/core/use-cases#declare-paging-on-every-collection-query</follows>
/// <follows>litespec/core/use-cases#justify-every-wider-visibility</follows>
internal static class QueryChecks
{
    /// <summary>Reports a query use case declaring no output in include groups.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunQueryOutput(ProjectModel model, List<Finding> findings)
    {
        foreach (var query in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "query"))
        {
            if (!HasIncludeGroups(query))
            {
                findings.Add(new Finding("query-output-missing", $"Use case '{query.Id}' declares no output in include groups.", query.File, query.Line, SubjectOf(query)));
            }
        }
    }

    /// <summary>Decides whether a query declares output in include groups.</summary>
    /// <param name="query">The query use case record whose output field is inspected.</param>
    /// <returns>True when the output holds a non-empty include object; otherwise false.</returns>
    private static bool HasIncludeGroups(Record query) =>
        query.Raw.ValueKind == JsonValueKind.Object
        && query.Raw.TryGetProperty(RecordFields.Output, out var output)
        && output.ValueKind == JsonValueKind.Object
        && output.TryGetProperty(RecordFields.Include, out var include)
        && include.ValueKind == JsonValueKind.Object
        && include.EnumerateObject().Any();

    /// <summary>Reports a query use case declaring no paging.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunPaging(ProjectModel model, List<Finding> findings)
    {
        foreach (var query in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "query"))
        {
            if (!query.Has(RecordFields.Paging)
                || (query.Raw.TryGetProperty(RecordFields.Paging, out var paging) && paging.ValueKind == JsonValueKind.Null))
            {
                findings.Add(new Finding("paging-undeclared", $"Use case '{query.Id}' declares no paging.", query.File, query.Line, SubjectOf(query)));
            }
        }
    }

    /// <summary>Reports a query widening a group through a cited record that is no requirement.</summary>
    /// <param name="model">The loaded project model that holds every use case record and each cited target.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunWidening(ProjectModel model, List<Finding> findings)
    {
        foreach (var query in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "query"))
        {
            if (query.Raw.ValueKind != JsonValueKind.Object
                || !query.Raw.TryGetProperty(RecordFields.Output, out var output)
                || output.ValueKind != JsonValueKind.Object
                || !output.TryGetProperty(RecordFields.Include, out var include)
                || include.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var group in include.EnumerateObject().OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                foreach (var cited in LinkStrings(group.Value, RecordFields.Cites).Order(StringComparer.Ordinal))
                {
                    // An unknown target is left to reference-unknown.
                    if (!model.ById.TryGetValue(cited, out var target))
                    {
                        continue;
                    }

                    if (target.Type != RecordTypes.Requirement)
                    {
                        findings.Add(new Finding("widening-cites-missing", $"Use case '{query.Id}' widens '{group.Name}' through '{cited}', which is no requirement.", query.File, query.Line, SubjectOf(query)));
                    }
                }
            }
        }
    }
}
