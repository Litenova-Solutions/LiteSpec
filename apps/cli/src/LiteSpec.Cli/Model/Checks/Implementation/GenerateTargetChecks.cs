using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>
///     The generate target check: every generate target writes outside each frontend's
///     generated client folder, and no two targets write the same file.
/// </summary>
/// <follows>litespec/core/model#keep-generate-targets-outside-other-tools-output-folders</follows>
internal static class GenerateTargetChecks
{
    /// <summary>Reports each misconfigured generate target.</summary>
    /// <param name="model">The loaded project model holding the generate targets and frontends.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        if (model.Config.Generate.Count == 0)
        {
            return;
        }

        var plan = GeneratePlan.Build(model);
        var projectFile = ConsumerRoot.ProjectFile(model.Config.Root);

        foreach (var error in plan.ConfigErrors)
        {
            findings.Add(new Finding("generate-target-invalid", error, projectFile, 1, null));
        }
    }
}
