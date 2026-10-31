using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The constituent check: an unmarked Domain type under no aggregate folder and outside the kernel.</summary>
/// <follows>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</follows>
internal static class ConstituentChecks
{
    /// <summary>Runs the constituent check.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var orphan in Constituents.Orphans(model))
        {
            findings.Add(new Finding(
                "constituent-parent-missing",
                $"Domain type '{orphan.Name}' sits in '{FolderShown(model, orphan.File)}', which holds no aggregate root and is not the shared kernel.",
                orphan.File,
                orphan.Line,
                null));
        }
    }

    /// <summary>
    ///     The folder a constituent finding shows: the type's folder relative to the consumer root,
    ///     with forward slashes. A type at the root shows <c>.</c>.
    /// </summary>
    /// <param name="model">The loaded project model that supplies the consumer root for the relative path.</param>
    /// <param name="file">The full path of the source file whose folder is shown.</param>
    /// <returns>The folder relative to the consumer root with forward slashes, or a dot for a file at the root.</returns>
    private static string FolderShown(ProjectModel model, string file)
    {
        var folder = Path.GetDirectoryName(file);

        if (folder is null)
        {
            return ".";
        }

        var relative = model.Config.Relative(folder);

        return relative.Length == 0 ? "." : relative;
    }
}
