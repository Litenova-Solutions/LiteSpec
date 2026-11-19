using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The command-edge checks: every commands edge names another module's command use case that lists the sending module in reachedThrough, and every module a use case lists commands it from a record.</summary>
/// <follows>litespec/core/model#connect-across-modules-only-through-declared-dependencies</follows>
internal static class CommandEdgeChecks
{
    /// <summary>Reports a commands edge naming no command use case, and a public use case no listed module commands.</summary>
    /// <param name="model">The loaded project model that holds every use case, reaction and job.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckCommandsTargets(model, findings);
        CheckPublicCommandsCalled(model, findings);
    }

    /// <summary>Reports a commands edge on a use case, which only a reaction or a job declares, and a commands edge naming a record that is no command use case, a command of the sending module itself, or a command whose reachedThrough does not name the sending module.</summary>
    /// <param name="model">The loaded project model that holds every use case, reaction and job.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckCommandsTargets(ProjectModel model, List<Finding> findings)
    {
        foreach (var holder in model.Records.Where(record =>
            !record.Derived && record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job))
        {
            var module = LocalityChecks.ModuleOf(model, holder);

            foreach (var target in holder.EdgeIds(RecordFields.Commands).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            {
                // A command handler never sends a command, so a use case declares no
                // commands edge: the edge belongs to the reaction or job that sends it.
                if (holder.Type == RecordTypes.UseCase)
                {
                    findings.Add(new Finding("command-edge-invalid", $"Use case '{holder.Id}' commands '{target}'; only a reaction or a job commands a use case.", holder.File, holder.Line, SubjectOf(holder)));
                    continue;
                }

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
                        findings.Add(new Finding("command-target-invalid", $"{Label(holder.Type)} '{holder.Id}' commands '{target}', which is a {resolved.Type}; only a command use case is commanded.", holder.File, holder.Line, SubjectOf(holder)));
                    }

                    continue;
                }

                if (resolved.Field(RecordFields.Kind) == "query")
                {
                    findings.Add(new Finding("command-target-invalid", $"{Label(holder.Type)} '{holder.Id}' commands '{target}', which is a query; only a command use case is commanded.", holder.File, holder.Line, SubjectOf(holder)));
                    continue;
                }

                if (resolved.Field(RecordFields.Kind) != "command")
                {
                    continue;
                }

                // A reaction or job runs its own module's commands through a runs
                // edge, so a commands edge stays cross-module.
                if (module is { Length: > 0 }
                    && LocalityChecks.ModuleOf(model, resolved) is { } targetModule
                    && targetModule.Equals(module, StringComparison.Ordinal))
                {
                    findings.Add(new Finding("command-target-invalid", $"{Label(holder.Type)} '{holder.Id}' in module '{module}' commands '{target}', which is a command of its own module; a reaction or job runs its own module's commands through a `runs` edge.", holder.File, holder.Line, SubjectOf(holder)));
                    continue;
                }

                if (module is { Length: > 0 }
                    && !resolved.Ids(RecordFields.ReachedThrough).Contains($"module/{module}", StringComparer.Ordinal))
                {
                    findings.Add(new Finding("command-target-invalid", $"{Label(holder.Type)} '{holder.Id}' in module '{module}' commands '{target}', whose reachedThrough names no 'module/{module}'.", holder.File, holder.Line, SubjectOf(holder)));
                }
            }
        }
    }

    /// <summary>Reports a use case naming a module in reachedThrough that commands it from no reaction or job of that module. A commands edge on a use case is itself invalid, so it never counts as the call.</summary>
    /// <param name="model">The loaded project model that holds every use case, reaction and job.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPublicCommandsCalled(ProjectModel model, List<Finding> findings)
    {
        var commanded = new HashSet<(string Module, string Target)>();

        foreach (var holder in model.Records.Where(record =>
            !record.Derived && record.Type is RecordTypes.Reaction or RecordTypes.Job))
        {
            if (LocalityChecks.ModuleOf(model, holder) is not { Length: > 0 } module)
            {
                continue;
            }

            foreach (var target in holder.EdgeIds(RecordFields.Commands))
            {
                commanded.Add((module, Identifiers.BaseId(target)));
            }
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "command").OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            foreach (var module in ModulesOf(useCase).Order(StringComparer.Ordinal))
            {
                if (!commanded.Contains((module, useCase.Id)))
                {
                    findings.Add(new Finding("command-consumer-missing", $"Use case '{useCase.Id}' is reached through 'module/{module}', which commands it from no record.", useCase.File, useCase.Line, SubjectOf(useCase)));
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
