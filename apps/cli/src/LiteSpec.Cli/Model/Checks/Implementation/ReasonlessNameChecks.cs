using LiteSpec.Cli.Model.Checks.Implementation.Handlers;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>The reasonless-name check: no folder or type is named for the absence of a reason, such as Common, Shared or Helpers.</summary>
/// <follows>litespec/backend/dotnet/naming#name-no-folder-or-type-for-the-absence-of-a-reason</follows>
internal static class ReasonlessNameChecks
{
    /// <summary>Folder and type names that name the absence of a reason.</summary>
    private static readonly HashSet<string> ReasonlessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shared", "Common", "Util", "Utils", "Helpers", "Misc",
    };

    /// <summary>Type suffixes that name the absence of a reason.</summary>
    private static readonly string[] ReasonlessSuffixes = ["Helper", "Manager", "Util", "Utils"];

    /// <summary>Reports each folder or type outside the shared kernel whose name states no reason.</summary>
    /// <param name="model">The loaded project model that holds the scanned code and the shared kernel path.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var kernel = model.Config.SharedKernelRoot;
        var reportedFolders = new HashSet<string>(StringComparer.Ordinal);
        var reportedTypes = new HashSet<string>(StringComparer.Ordinal);
        var files = new List<string>();

        foreach (var handler in model.Code.Handlers)
        {
            files.Add(handler.File);
        }

        foreach (var host in HostRow.All(model))
        {
            files.Add(host.File);
        }

        foreach (var domainEvent in model.Code.Events)
        {
            files.Add(domainEvent.File);
        }

        foreach (var endpoint in model.Code.Endpoints)
        {
            files.Add(endpoint.File);
        }

        foreach (var literal in model.Code.Literals)
        {
            files.Add(literal.File);
        }

        foreach (var comment in model.Code.Comments)
        {
            files.Add(comment.File);
        }

        foreach (var file in files.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            if (UnderSharedKernel(kernel, file))
            {
                continue;
            }

            var relative = PathText.Relative(model.Config.Root, file);
            var parts = relative.Split('/');

            for (var index = 0; index < parts.Length - 1; index++)
            {
                var folder = string.Join('/', parts[..(index + 1)]);

                if (ReasonlessNames.Contains(parts[index]) && reportedFolders.Add(folder))
                {
                    findings.Add(new Finding(
                        "name-reason-missing",
                        $"Folder '{folder}' carries the reasonless name '{parts[index]}'.",
                        file,
                        1,
                        null));
                }
            }
        }

        foreach (var (type, file, line) in ReasonlessTypeSites(model))
        {
            if (UnderSharedKernel(kernel, file) || !reportedTypes.Add(type))
            {
                continue;
            }

            findings.Add(new Finding(
                "name-reason-missing",
                $"Type '{type}' carries a reasonless name.",
                file,
                line,
                null));
        }
    }

    /// <summary>Whether the file sits under the shared kernel project.</summary>
    /// <param name="kernel">The shared kernel project path.</param>
    /// <param name="file">The full path of the file under test.</param>
    /// <returns>True when the file sits under the shared kernel project; otherwise false.</returns>
    private static bool UnderSharedKernel(string kernel, string file)
    {
        if (kernel.Length == 0)
        {
            return false;
        }

        var relative = PathText.Relative(kernel, file);

        return !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative);
    }

    /// <summary>Every reasonless type name with its first site, in file and line order.</summary>
    /// <param name="model">The loaded project model that holds each scanned handler, host, event and literal.</param>
    /// <returns>Each reasonless type name with its first site, in file and line order.</returns>
    private static List<(string Type, string File, int Line)> ReasonlessTypeSites(ProjectModel model)
    {
        var sites = new Dictionary<string, (string File, int Line)>(StringComparer.Ordinal);

        void Add(string? type, string file, int line)
        {
            if (type is not { Length: > 0 } || sites.ContainsKey(type))
            {
                return;
            }

            if (ReasonlessNames.Contains(type)
                || ReasonlessSuffixes.Any(suffix =>
                    type.EndsWith(suffix, StringComparison.Ordinal) && type.Length > suffix.Length))
            {
                sites[type] = (file, line);
            }
        }

        foreach (var handler in model.Code.Handlers)
        {
            Add(handler.Class, handler.File, handler.Line);
        }

        foreach (var host in HostRow.All(model))
        {
            Add(host.Class, host.File, host.Line);
        }

        foreach (var domainEvent in model.Code.Events)
        {
            Add(domainEvent.Class, domainEvent.File, domainEvent.Line);
        }

        foreach (var literal in model.Code.Literals)
        {
            Add(literal.Class, literal.File, literal.Line);
        }

        return [.. sites
            .OrderBy(entry => entry.Value.File, StringComparer.Ordinal)
            .ThenBy(entry => entry.Value.Line)
            .Select(entry => (entry.Key, entry.Value.File, entry.Value.Line))];
    }
}
