using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The query-edge checks: every queries edge names a query use case that lists the calling module in reachedThrough, and every module a use case lists queries it from a record.</summary>
/// <follows>litespec/backend/application#ask-another-module-one-question-through-a-decision-port</follows>
internal static class QueryEdgeChecks
{
    /// <summary>Reports a queries edge naming no query use case, and a public use case no listed module queries.</summary>
    /// <param name="model">The loaded project model that holds every use case, reaction and job.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckQueriesTargets(model, findings);
        CheckPublicQueriesCalled(model, findings);
    }

    /// <summary>Reports a queries edge naming a record that is no query use case, or a query whose reachedThrough does not name the calling module.</summary>
    /// <param name="model">The loaded project model that holds every use case, reaction and job.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckQueriesTargets(ProjectModel model, List<Finding> findings)
    {
        foreach (var holder in model.Records.Where(record =>
            !record.Derived && record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job))
        {
            var module = LocalityChecks.ModuleOf(model, holder);

            foreach (var target in holder.EdgeIds(RecordFields.Queries).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            {
                if (!model.ById.TryGetValue(Identifiers.BaseId(target), out var resolved))
                {
                    continue;
                }

                // An unknown target is reference-unknown, a target outside the effect link
                // types is record-invalid through the link type check, and a use case with
                // no query or command kind is record-invalid through the shape check.
                if (resolved.Type != RecordTypes.UseCase)
                {
                    if (resolved.Type is RecordTypes.Notification or RecordTypes.ExternalSystem)
                    {
                        findings.Add(new Finding("query-target-invalid", $"{Label(holder.Type)} '{holder.Id}' queries '{target}', which is a {resolved.Type}; only a query use case is queried.", holder.File, holder.Line, SubjectOf(holder)));
                    }

                    continue;
                }

                if (resolved.Field(RecordFields.Kind) == "command")
                {
                    findings.Add(new Finding("query-target-invalid", $"{Label(holder.Type)} '{holder.Id}' queries '{target}', which is a command; only a query use case is queried.", holder.File, holder.Line, SubjectOf(holder)));
                    continue;
                }

                if (resolved.Field(RecordFields.Kind) != "query")
                {
                    continue;
                }

                if (module is { Length: > 0 }
                    && !resolved.Ids(RecordFields.ReachedThrough).Contains($"module/{module}", StringComparer.Ordinal))
                {
                    findings.Add(new Finding("query-target-invalid", $"{Label(holder.Type)} '{holder.Id}' in module '{module}' queries '{target}', whose reachedThrough names no 'module/{module}'.", holder.File, holder.Line, SubjectOf(holder)));
                }
            }
        }
    }

    /// <summary>Reports a use case naming a module in reachedThrough that queries it from no record of that module.</summary>
    /// <param name="model">The loaded project model that holds every use case, reaction and job.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPublicQueriesCalled(ProjectModel model, List<Finding> findings)
    {
        var queried = new HashSet<(string Module, string Target)>();

        foreach (var holder in model.Records.Where(record =>
            !record.Derived && record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job))
        {
            if (LocalityChecks.ModuleOf(model, holder) is not { Length: > 0 } module)
            {
                continue;
            }

            foreach (var target in holder.EdgeIds(RecordFields.Queries))
            {
                queried.Add((module, Identifiers.BaseId(target)));
            }
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "query").OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            foreach (var module in ModulesOf(useCase).Order(StringComparer.Ordinal))
            {
                if (!queried.Contains((module, useCase.Id)))
                {
                    findings.Add(new Finding("query-consumer-missing", $"Use case '{useCase.Id}' is reached through 'module/{module}', which queries it from no record.", useCase.File, useCase.Line, SubjectOf(useCase)));
                }
            }
        }
    }

    /// <summary>The modules a use case lists in reachedThrough.</summary>
    /// <param name="useCase">The use case record whose reachedThrough is read.</param>
    /// <returns>Each module name the use case lists, once.</returns>
    private static HashSet<string> ModulesOf(Record useCase)
    {
        var modules = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in useCase.Ids(RecordFields.ReachedThrough))
        {
            if (Identifiers.TrySplit(Identifiers.BaseId(id), out var prefix, out _, out var name)
                && prefix == RecordTypes.Module
                && name.Length > 0)
            {
                modules.Add(name);
            }
        }

        return modules;
    }
}
