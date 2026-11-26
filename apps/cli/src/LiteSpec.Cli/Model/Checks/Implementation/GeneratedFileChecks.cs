using LiteSpec.Cli.Generate;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>
///     The generated file check: every file <c>litespec generate</c> writes from the
///     configured targets exists and matches what the records render now.
/// </summary>
/// <follows>litespec/core/model#generate-record-determined-code-from-the-records</follows>
internal static class GeneratedFileChecks
{
    /// <summary>Reports each generated file that is missing or differs from what the records render.</summary>
    /// <param name="model">The loaded project model holding the generate targets.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        if (model.Config.Generate.Count == 0)
        {
            return;
        }

        // An invalid target renders nothing: the project schema owns its shape, and
        // generate names its error when it runs. The check judges the files that render.
        var plan = GeneratePlan.Build(model);

        foreach (var file in plan.Files)
        {
            var relative = PathText.Relative(model.Config.Root, file.AbsolutePath);

            if (!File.Exists(file.AbsolutePath))
            {
                findings.Add(new Finding(
                    "generated-drift",
                    $"File '{relative}' is written by `litespec generate` and is missing. Run `litespec generate` and keep the files it writes.",
                    file.AbsolutePath,
                    1,
                    null));
                continue;
            }

            string current;

            try
            {
                current = File.ReadAllText(file.AbsolutePath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                current = "";
            }

            if (current != file.Content)
            {
                findings.Add(new Finding(
                    "generated-drift",
                    $"File '{relative}' differs from what `litespec generate` renders from the records. Run `litespec generate` and keep the files it writes.",
                    file.AbsolutePath,
                    1,
                    null));
            }
        }
    }
}
