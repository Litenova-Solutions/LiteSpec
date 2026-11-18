using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The frontend placement check: every frontend module folder names a declared module, and every web module of a use case sits in the folder its record home mirrors.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class FrontendPlacementChecks
{
    /// <summary>
    ///     Reports every frontend module folder that names no declared module, and every web module of
    ///     a use case outside its record home's folder. The use case the folder mirrors stays with the
    ///     mirror check, so only the module segment against the declared modules and the aggregate
    ///     segment against the marked use case are reported here. A project with no declared modules
    ///     states no mirror, so it is left out.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and each frontend module.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var modules = ClosedFolderChecks.DeclaredModules(model);

        if (modules.Count == 0)
        {
            return;
        }

        foreach (var element in model.Code.Elements)
        {
            if (element.Kind != CodeKind.FrontendModule)
            {
                continue;
            }

            var folder = Directory.Exists(element.File) ? element.File : Path.GetDirectoryName(element.File) ?? element.File;

            foreach (var frontend in model.Config.Frontends)
            {
                if (SegmentsUnder(frontend, folder) is not { } segments)
                {
                    continue;
                }

                if (!modules.Any(module => SameName(segments[0], module)))
                {
                    var mark = element.Marks.FirstOrDefault(mark => mark.Verb != MarkVerb.Follows);
                    var subject = mark?.RecordId;
                    var file = mark?.File ?? element.File;
                    var line = mark?.Line ?? element.Line;

                    findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in module folder '{segments[0]}', but no declared module names it.", file, line, subject));
                }

                CheckAggregateSegment(model, element, segments, findings);

                break;
            }
        }
    }

    /// <summary>
    ///     Reports a web module of an aggregate use case outside its plural aggregate folder, and a web
    ///     module of a module-level use case nested in one: the module mirrors its use case record's home,
    ///     <c>lib/modules/{module}/{aggregates}/{use-case}/</c> under an aggregate, else
    ///     <c>lib/modules/{module}/{use-case}/</c>. A module or use-case segment naming another record
    ///     stays with its own check, so only the aggregate segment is reported here.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="element">The marked frontend module whose folder is checked.</param>
    /// <param name="segments">The module folder segments under the frontend modules root.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAggregateSegment(ProjectModel model, MarkedElement element, string[] segments, List<Finding> findings)
    {
        var mark = element.Marks.FirstOrDefault(mark => mark.Verb != MarkVerb.Follows);

        if (mark is null
            || !model.ById.TryGetValue(mark.RecordId, out var record)
            || record.Type != RecordTypes.UseCase)
        {
            return;
        }

        var path = PathOf(record);

        if (path.Module is null || path.Name is null)
        {
            return;
        }

        if (path.Aggregate is null)
        {
            // A module-level use case drops the aggregate segment.
            if (segments.Length == 3
                && SameName(segments[0], path.Module)
                && SameName(segments[2], path.Name))
            {
                Drift(element, mark, $"{path.Module}/{path.Name}", findings);
            }

            return;
        }

        if (AggregateFolderNames.OwnerDirectory(record.File) is not { } owner)
        {
            return;
        }

        var expected = AggregateFolderNames.Resolve(model, path.Aggregate, owner).Expected;

        if (segments.Length == 2
            && SameName(segments[0], path.Module)
            && SameName(segments[1], path.Name))
        {
            Drift(element, mark, $"{path.Module}/{expected}/{path.Name}", findings);
        }
        else if (segments.Length == 3
            && SameName(segments[0], path.Module)
            && SameName(segments[2], path.Name)
            && !SameName(segments[1], expected))
        {
            Drift(element, mark, $"{path.Module}/{expected}/{path.Name}", findings);
        }
    }

    /// <summary>Reports the web module outside the folder its marked use case takes.</summary>
    /// <param name="element">The marked frontend module whose folder is checked.</param>
    /// <param name="mark">The mark naming the use case the folder should hold.</param>
    /// <param name="expected">The expected folder under the modules root, with forward slashes.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void Drift(MarkedElement element, CodeMark mark, string expected, List<Finding> findings) =>
        findings.Add(new Finding(
            "code-placement-drift",
            $"{KindName(element.Kind)} '{DisplayName(element)}' marks '{mark.RecordId}', so it belongs in '{expected}/'.",
            mark.File, mark.Line, mark.RecordId));

    /// <summary>The folder segments under the frontend modules root, or null outside it.</summary>
    /// <param name="frontend">The frontend whose modules root the folder sits under.</param>
    /// <param name="folder">The absolute folder path.</param>
    /// <returns>The segments under the modules root, or null outside the root.</returns>
    private static string[]? SegmentsUnder(FrontendConfig frontend, string folder)
    {
        if (!Directory.Exists(frontend.ModulesRoot))
        {
            return null;
        }

        var relative = PathText.Relative(Path.GetFullPath(frontend.ModulesRoot), Path.GetFullPath(folder));

        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            return null;
        }

        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Length > 0 && segments[0] is not "." && segments[0].Length > 0 ? segments : null;
    }
}
