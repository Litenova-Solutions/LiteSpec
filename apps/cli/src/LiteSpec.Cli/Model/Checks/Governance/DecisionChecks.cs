using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Governance;

/// <summary>
///     The technical decision checks: a decision states its kind, and a decision whose followers
///     all sit in one module and already implement a record is reported as local.
/// </summary>
/// <follows>litespec/core/model#record-each-technical-choice-as-a-decision</follows>
internal static class DecisionChecks
{
    /// <summary>The finding code for a technical decision with no <c>kind</c>.</summary>
    internal const string KindMissingCode = "decision-kind-missing";

    /// <summary>The finding code for a technical decision followed only inside one module.</summary>
    internal const string ScopeInvalidCode = "decision-scope-invalid";

    /// <summary>Reports decision-kind-missing for each technical decision that states no kind.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunKind(ProjectModel model, List<Finding> findings)
    {
        foreach (var decision in model.Records.Where(candidate =>
            candidate.Type == RecordTypes.Decision && MarkChecks.UnderImplementationDecisions(model.Config, candidate.File)))
        {
            if (string.IsNullOrWhiteSpace(decision.Field(RecordFields.Kind)))
            {
                findings.Add(new Finding(KindMissingCode, $"Decision '{decision.Id}' states no kind.", decision.File, decision.Line, SubjectOf(decision)));
            }
        }
    }

    /// <summary>Reports decision-scope-invalid for each technical decision whose followers all sit in one module and each implement a record.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunScope(ProjectModel model, List<Finding> findings)
    {
        foreach (var decision in model.Records.Where(candidate =>
            candidate.Type == RecordTypes.Decision && MarkChecks.UnderImplementationDecisions(model.Config, candidate.File)))
        {
            var followers = model.Code.Elements
                .Where(element => element.Marks.Any(mark => mark.Verb == MarkVerb.Follows && mark.RecordId == decision.Id))
                .ToList();

            if (followers.Count == 0
                || followers.Any(element => !element.Marks.Any(mark => mark.Verb == MarkVerb.Implements)))
            {
                continue;
            }

            if (SingleModuleOf(model, followers) is not { } module)
            {
                continue;
            }

            findings.Add(new Finding(ScopeInvalidCode, $"Decision '{decision.Id}' is followed only from module '{module}', and each follower already carries an implements mark.", decision.File, decision.Line, SubjectOf(decision)));
        }
    }

    /// <summary>The one module every follower sits in, or null when a follower sits outside any module or two followers sit in different modules.</summary>
    /// <param name="model">The loaded project model that holds the project configuration.</param>
    /// <param name="followers">The code elements following the decision, each carrying an implements mark.</param>
    /// <returns>The shared module folder, or null when the followers share none.</returns>
    private static string? SingleModuleOf(ProjectModel model, IReadOnlyList<MarkedElement> followers)
    {
        string? module = null;

        foreach (var follower in followers)
        {
            if (SourceRoots.ModulePath(model.Config, follower.File) is not { } segments)
            {
                return null;
            }

            if (module is null)
            {
                module = segments[0];
            }
            else if (!module.Equals(segments[0], StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return module;
    }
}
