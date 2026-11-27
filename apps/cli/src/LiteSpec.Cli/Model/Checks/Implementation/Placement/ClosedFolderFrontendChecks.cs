using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check for the frontends: each web frontend modules root with kebab-case folders, the shared app folder with its area groupings, and the shared UI package components folder.</summary>
internal static partial class ClosedFolderChecks
{
    /// <summary>Folder names no folder under an application's source tree carries: each accepts anything, so each file belongs in the folder that owns it instead.</summary>
    static readonly HashSet<string> RefusedFrontendFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "shared", "common", "utils", "util", "helpers", "misc", "components", "hooks", "types", "services",
    };

    /// <summary>Reports every module folder under one frontend modules root that names no declared module, every folder under it carrying a refused name, every folder segment outside kebab case, and every shared app folder outside its area groupings. An Android frontend keeps no module mirror, so only its module folders are checked.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="frontend">The frontend whose modules root is checked.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFrontendRoot(ConsumerConfig config, FrontendConfig frontend, List<string> modules, List<Finding> findings)
    {
        if (!frontend.Platform.Equals("android", StringComparison.OrdinalIgnoreCase))
        {
            CheckAppShared(config, frontend, findings);
        }

        if (modules.Count == 0 || !Directory.Exists(frontend.ModulesRoot))
        {
            return;
        }

        foreach (var module in ChildFolders(frontend.ModulesRoot))
        {
            var name = Path.GetFileName(module);

            if (!modules.Any(declared => SameName(name, declared)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, module)}' names no declared module.", module, 1, null));
            }
        }

        if (frontend.Platform.Equals("android", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var folder in WalkFolders(frontend.ModulesRoot))
        {
            var name = Path.GetFileName(folder);

            if (RefusedFrontendFolders.Contains(name))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, folder)}' carries the refused name '{name}'; move its files into the folder that owns them.", folder, 1, null));
            }
            else if (!IsKebabCase(name))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, folder)}' sits outside kebab case; spell it '{Naming.Kebab(name)}'.", folder, 1, null));
            }
        }
    }

    /// <summary>Reports every folder under a web frontend shared app folder outside its area groupings, and every file directly at its root outside the generated records file, at any count. The top keeps only the generated records file; shared session, messages, calls and layout sit in their areas.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="frontend">The frontend whose shared app folder is checked.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAppShared(ConsumerConfig config, FrontendConfig frontend, List<Finding> findings)
    {
        var app = frontend.FrameRoot;

        if (!Directory.Exists(app))
        {
            return;
        }

        foreach (var child in ChildFolders(app))
        {
            var name = Path.GetFileName(child);

            if (AppGroups.Contains(name))
            {
                CheckUseCaseFilesOnly(config, child, "grouping", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no shared app area; the shared app folder holds session/, i18n/, api/ and ui/.", child, 1, null));
        }

        foreach (var file in ChildFiles(app))
        {
            if (string.Equals(Path.GetFileName(file), "litespec-records.ts", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in the shared app folder, which holds only 'litespec-records.ts' and its area folders; move it into session/, i18n/, api/ or ui/.", file, 1, null));
        }
    }

    /// <summary>Whether a folder name is kebab case: lowercase letters and digits in hyphen-separated words.</summary>
    /// <param name="name">The folder name as written on disk.</param>
    /// <returns>True when the name holds lowercase words joined by single hyphens; otherwise false.</returns>
    private static bool IsKebabCase(string name)
    {
        if (name.Length == 0)
        {
            return false;
        }

        var word = false;

        foreach (var character in name)
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                word = true;
            }
            else if (character == '-' && word)
            {
                word = false;
            }
            else
            {
                return false;
            }
        }

        return word;
    }

    /// <summary>Every folder under one root in ordinal order, without build output and tool folders.</summary>
    /// <param name="root">The absolute root to walk.</param>
    /// <returns>The folders under the root in ordinal order.</returns>
    private static List<string> WalkFolders(string root)
    {
        var found = new List<string>();
        var stack = new Stack<string>(ChildFolders(root).Reverse<string>());

        while (stack.Count > 0)
        {
            var folder = stack.Pop();
            found.Add(folder);

            foreach (var child in ChildFolders(folder).Reverse<string>())
            {
                stack.Push(child);
            }
        }

        found.Sort(StringComparer.Ordinal);

        return found;
    }

    /// <summary>Reports every folder under the shared UI package components folder outside its role groupings, and the folder past 20 files with none of them. The top keeps the package index.</summary>
    /// <param name="config">The project configuration that holds the consumer root and the shared package root.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSharedComponents(ConsumerConfig config, List<Finding> findings)
    {
        if (config.SharedUiPackageRoot.Length == 0)
        {
            return;
        }

        var components = Path.Combine(config.SharedUiPackageRoot, "src", "components");

        if (!Directory.Exists(components))
        {
            return;
        }

        var children = ChildFolders(components);

        foreach (var child in children)
        {
            var name = Path.GetFileName(child);

            if (UiComponentGroups.Contains(name))
            {
                CheckUseCaseFilesOnly(config, child, "grouping", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no shared component role; components/ holds fields/, overlays/, layout/, data/, values/ and feedback/.", child, 1, null));
        }

        CheckUngrouped(config, components, children, child => UiComponentGroups.Contains(Path.GetFileName(child)), "group them into fields/, overlays/, layout/, data/, values/ or feedback/", findings);
    }
}
