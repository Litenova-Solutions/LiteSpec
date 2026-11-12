using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Model.Records;
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

    /// <summary>The finding code for a technical decision kept at a level its followers do not match.</summary>
    internal const string HomeDriftCode = "decision-home-drift";

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
            if (ScopeInvalidModule(model, decision) is not { } module)
            {
                continue;
            }

            findings.Add(new Finding(ScopeInvalidCode, $"Decision '{decision.Id}' is followed only from module '{module}', and each follower already carries an implements mark.", decision.File, decision.Line, SubjectOf(decision)));
        }
    }

    /// <summary>The one module a scope-invalid decision is followed from, or null when the decision is no scope finding.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="decision">The decision whose followers are judged.</param>
    /// <returns>The shared module folder when the decision is technical, followed, every follower carries an implements mark and all share one module; otherwise null.</returns>
    internal static string? ScopeInvalidModule(ProjectModel model, Record decision)
    {
        if (decision.Type != RecordTypes.Decision || !MarkChecks.UnderImplementationDecisions(model.Config, decision.File))
        {
            return null;
        }

        var followers = FollowersOf(model, decision.Id);

        if (followers.Count == 0
            || followers.Any(element => !element.Marks.Any(mark => mark.Verb == MarkVerb.Implements)))
        {
            return null;
        }

        return SingleModuleOf(model, followers);
    }

    /// <summary>Every code element following the decision.</summary>
    /// <param name="model">The loaded project model that holds the read code.</param>
    /// <param name="decisionId">The decision identifier the follows marks name.</param>
    /// <returns>The code elements carrying a follows mark on the decision, in read order.</returns>
    internal static IReadOnlyList<MarkedElement> FollowersOf(ProjectModel model, string decisionId) =>
        model.Code.Elements
            .Where(element => element.Marks.Any(mark => mark.Verb == MarkVerb.Follows && mark.RecordId == decisionId))
            .ToList();

    /// <summary>The distinct modules the followers sit in, in ordinal order.</summary>
    /// <param name="model">The loaded project model that holds the project configuration.</param>
    /// <param name="followers">The code elements following the decision.</param>
    /// <returns>The distinct module folders, leaving out every follower outside any module.</returns>
    internal static IReadOnlyList<string> ModulesOf(ProjectModel model, IReadOnlyList<MarkedElement> followers) =>
        followers
            .Select(follower => SourceRoots.ModulePath(model.Config, follower.File))
            .Where(segments => segments is not null)
            .Select(segments => segments![0])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>The module home of a technical decision file, or null when the file sits at product level.</summary>
    /// <param name="model">The loaded project model that holds the project configuration.</param>
    /// <param name="file">The full path of the decision file.</param>
    /// <returns>The module folder under implementation/modules, or null for a product-level decisions file.</returns>
    internal static string? HomeModuleOf(ProjectModel model, string file)
    {
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        foreach (var root in model.Config.RecordRoots)
        {
            var modules = Path.Combine(root, "implementation", "modules") + Path.DirectorySeparatorChar;

            if (file.StartsWith(modules, comparison))
            {
                var rest = file[modules.Length..].Split(Path.DirectorySeparatorChar);

                if (rest.Length >= 3 && string.Equals(rest[1], "decisions", comparison))
                {
                    return rest[0];
                }
            }
        }

        return null;
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

    /// <summary>Reports decision-home-drift for each technical decision kept at a level its followers do not match.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunHome(ProjectModel model, List<Finding> findings)
    {
        foreach (var decision in model.Records.Where(candidate =>
            candidate.Type == RecordTypes.Decision && MarkChecks.UnderImplementationDecisions(model.Config, candidate.File)))
        {
            var followers = FollowersOf(model, decision.Id);

            if (followers.Count == 0)
            {
                continue;
            }

            var modules = ModulesOf(model, followers);
            var home = HomeModuleOf(model, decision.File);

            if (home is null)
            {
                // A product-level decision followed only from one module belongs in that
                // module. A follower outside any module keeps it at product level.
                if (modules.Count == 1 && followers.All(follower => SourceRoots.ModulePath(model.Config, follower.File) is not null))
                {
                    findings.Add(new Finding(HomeDriftCode, $"Decision '{decision.Id}' sits at product level but is followed only from module '{modules[0]}', so it belongs in that module.", decision.File, decision.Line, SubjectOf(decision)));
                }
            }
            else if (modules.Count > 1)
            {
                var list = string.Join(", ", modules.Select(module => $"'{module}'"));

                findings.Add(new Finding(HomeDriftCode, $"Decision '{decision.Id}' sits in module '{home}' but is followed from modules {list}, so it belongs at product level.", decision.File, decision.Line, SubjectOf(decision)));
            }
            else if (modules.Count == 1 && !modules[0].Equals(home, StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new Finding(HomeDriftCode, $"Decision '{decision.Id}' sits in module '{home}' but is followed only from module '{modules[0]}', so it belongs in that module.", decision.File, decision.Line, SubjectOf(decision)));
            }
        }
    }
}
