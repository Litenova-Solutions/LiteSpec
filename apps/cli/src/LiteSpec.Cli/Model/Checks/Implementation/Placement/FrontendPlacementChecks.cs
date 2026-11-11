using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The frontend placement check: every frontend module folder names a declared module.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class FrontendPlacementChecks
{
    /// <summary>
    ///     Reports every frontend module folder that names no declared module. The use case the folder
    ///     mirrors stays with the mirror check, so only the module segment against the declared modules
    ///     is reported here. A project with no declared modules states no mirror, so it is left out.
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
                if (ModuleSegment(frontend, folder) is not { } segment)
                {
                    continue;
                }

                if (!modules.Any(module => SameName(segment, module)))
                {
                    var mark = element.Marks.FirstOrDefault(mark => mark.Verb != MarkVerb.Follows);
                    var subject = mark?.RecordId;
                    var file = mark?.File ?? element.File;
                    var line = mark?.Line ?? element.Line;

                    findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in module folder '{segment}', but no declared module names it.", file, line, subject));
                }

                break;
            }
        }
    }

    /// <summary>The module segment of a frontend module folder under the frontend modules root, or null outside it.</summary>
    /// <param name="frontend">The frontend whose modules root the folder sits under.</param>
    /// <param name="folder">The absolute folder path.</param>
    /// <returns>The first folder segment under the modules root, or null outside the root.</returns>
    private static string? ModuleSegment(FrontendConfig frontend, string folder)
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

        return segments.Length > 0 && segments[0] is not "." && segments[0].Length > 0 ? segments[0] : null;
    }
}
