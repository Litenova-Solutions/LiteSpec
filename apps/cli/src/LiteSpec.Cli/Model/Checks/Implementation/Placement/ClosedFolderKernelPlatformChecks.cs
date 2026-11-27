using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check for the repository roots: the Kernel root, the Platform root with its capability folders, and the Knowledge root.</summary>
internal static partial class ClosedFolderChecks
{
    /// <summary>Reports every folder under the Kernel root outside its grouping folders, and every file directly at the root, at any count: the root holds no files.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckKernelRoot(ConsumerConfig config, List<Finding> findings)
    {
        foreach (var child in ChildFolders(config.KernelRoot))
        {
            var name = Path.GetFileName(child);

            if (KernelFolders.Contains(name))
            {
                CheckUseCaseFilesOnly(config, child, "grouping", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no Kernel folder; the Kernel holds Domain/, Values/ and Auditing/.", child, 1, null));
        }

        foreach (var file in ChildFiles(config.KernelRoot))
        {
            if (Path.GetFileName(file).EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in the Kernel root, which holds no files; move it into Domain/, Values/ or Auditing/.", file, 1, null));
        }
    }

    /// <summary>Reports every folder under the Platform root that breaks its closed set of capability folders, every file directly at the root, at any count, and each capability past 20 files with no sub-area.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPlatformRoot(ConsumerConfig config, List<Finding> findings)
    {
        foreach (var child in ChildFolders(config.PlatformRoot))
        {
            var name = Path.GetFileName(child);

            if (PlatformFolders.Contains(name))
            {
                CheckCapabilityLevel(config, child, name, findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no Platform folder.", child, 1, null));
        }

        foreach (var file in ChildFiles(config.PlatformRoot))
        {
            if (Path.GetFileName(file).EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in the Platform root, which holds no files; move it into its capability folder.", file, 1, null));
        }
    }

    /// <summary>Reports every folder under one Platform capability outside its sub-areas, and the capability past 20 files with none. CountryPacks takes one folder per named pack area; any other capability groups into one folder per sub-area of its own naming.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="capability">The absolute capability folder under the Platform root.</param>
    /// <param name="name">The capability folder name as written on disk.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckCapabilityLevel(ConsumerConfig config, string capability, string name, List<Finding> findings)
    {
        var children = ChildFolders(capability);
        var packs = string.Equals(name, "CountryPacks", StringComparison.OrdinalIgnoreCase);

        foreach (var child in children)
        {
            var area = Path.GetFileName(child);

            if (packs && !CountryPackAreas.Contains(area))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no CountryPacks area; CountryPacks holds Vat/, Charts/, Banking/, Filing/, Documents/, Country/ and Retention/.", child, 1, null));
                continue;
            }

            CheckUseCaseFilesOnly(config, child, "grouping", findings);
        }

        var remedy = packs
            ? "group them into Vat/, Charts/, Banking/, Filing/ or Documents/"
            : "group them into one folder per sub-area";

        CheckUngrouped(config, capability, children, _ => true, remedy, findings, [".cs"]);
    }

    /// <summary>Reports every folder under the Knowledge root that is neither a knowledge model nor a set of a model the folder holds, and every file directly at the root: the root holds one model project per area and one set project per variant.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="knowledge">The absolute Knowledge folder beside Modules.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects</follows>
    private static void CheckKnowledgeRoot(ConsumerConfig config, string knowledge, List<Finding> findings)
    {
        var models = ChildFolders(knowledge)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(SourceRoots.IsKnowledgeModelName)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var child in ChildFolders(knowledge))
        {
            var name = Path.GetFileName(child);

            if (SourceRoots.IsKnowledgeModelName(name))
            {
                continue;
            }

            if (SourceRoots.KnowledgeModelName(child) is { } own && models.Contains(own))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no knowledge model or set of a known model; Knowledge holds one '<Product>.<Area>/' model project per area and one '<Product>.<Area>.<Variant>/' set project per variant.", child, 1, null));
        }

        foreach (var file in ChildFiles(knowledge))
        {
            if (Path.GetFileName(file).EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in the Knowledge root, which holds only model and set projects.", file, 1, null));
        }
    }
}
