using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The automation access checks: every use case a reaction or job runs, commands, or queries admits the system identity through its grant or runsAs.</summary>
/// <follows>litespec/core/access#admit-the-system-identity-on-every-system-run-use-case</follows>
internal static class AutomationAccessChecks
{
    /// <summary>The access field stating the identity a use case runs as without a person.</summary>
    private const string RunsAs = "runsAs";

    /// <summary>The edge verbs that send a use case with no person behind the call.</summary>
    private static readonly string[] SystemStartedVerbs = [RecordFields.Runs, RecordFields.Commands, RecordFields.Queries];

    /// <summary>Reports each reaction or job that runs, commands, or queries a use case whose access admits no system identity.</summary>
    /// <param name="model">The loaded project model that holds every reaction, job, and use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var holder in model.Records.Where(record =>
            !record.Derived && record.Type is RecordTypes.Reaction or RecordTypes.Job))
        {
            foreach (var verb in SystemStartedVerbs)
            {
                foreach (var target in holder.EdgeIds(verb).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
                {
                    // An unknown target is reference-unknown through the citation checks, and
                    // a target that is no use case is another code's finding through the edge checks.
                    if (!model.ById.TryGetValue(Identifiers.BaseId(target), out var resolved)
                        || resolved.Type != RecordTypes.UseCase)
                    {
                        continue;
                    }

                    if (!AdmitsSystem(resolved))
                    {
                        findings.Add(new Finding(
                            "system-access-missing",
                            $"{Label(holder.Type)} '{holder.Id}' {verb} '{target}', whose access admits no system identity; state `runsAs: system` on the use case.",
                            holder.File,
                            holder.Line,
                            SubjectOf(holder)));
                    }
                }
            }
        }
    }

    /// <summary>Whether the use case admits the system identity: a system or anonymous grant, or runsAs system beside its grant.</summary>
    /// <param name="useCase">The use case record whose access is read.</param>
    /// <returns>True when the grant is system or anonymous, or runsAs is system; a use case with no access object is left to the shape checks.</returns>
    private static bool AdmitsSystem(Record useCase)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Access, out var access)
            || access.ValueKind != JsonValueKind.Object)
        {
            return true;
        }

        if (LinkStrings(access, RecordFields.Grant).Any(grant => grant is "system" or "anonymous"))
        {
            return true;
        }

        return Str(access, RunsAs) == "system";
    }
}
