using LiteSpec.Cli.Sync;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>
///     The sync file check: every file <c>litespec sync</c> writes from the
///     configured targets and frontend clients exists and matches what the records render now.
/// </summary>
/// <follows>litespec/core/model#sync-record-determined-code-from-the-records</follows>
internal static class SyncFileChecks
{
    /// <summary>Reports each synced file that is missing, differs from what the records render, or is replaced and still present.</summary>
    /// <param name="model">The loaded project model holding the targets and frontend clients.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        // An invalid target renders nothing: the project schema owns its shape, and
        // sync names its error when it runs. The check judges the files that render.
        var plan = SyncPlan.Build(model);

        foreach (var file in plan.Files)
        {
            var relative = PathText.Relative(model.Config.Root, file.AbsolutePath);

            if (!File.Exists(file.AbsolutePath))
            {
                findings.Add(new Finding(
                    "sync-drift",
                    $"File '{relative}' is written by `litespec sync` and is missing. Run `litespec sync` and keep the files it writes.",
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
                    "sync-drift",
                    $"File '{relative}' differs from what `litespec sync` renders from the records. Run `litespec sync` and keep the files it writes.",
                    file.AbsolutePath,
                    1,
                    null));
            }
        }

        foreach (var deleted in plan.Deletions)
        {
            var relative = PathText.Relative(model.Config.Root, deleted.AbsolutePath);
            var replacement = PathText.Relative(model.Config.Root, deleted.ReplacedBy);

            findings.Add(new Finding(
                "sync-drift",
                $"File '{relative}' is replaced by '{replacement}' from `litespec sync` and is still present. Run `litespec sync` and keep the files it writes.",
                deleted.AbsolutePath,
                1,
                null));
        }
    }
}
