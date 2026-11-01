namespace LiteSpec.Model;

/// <summary>
///     The use-case id a frontend module folder mirrors: three segments
///     <c>{module}/{aggregate}/{use-case}</c> name <c>use-case/&lt;module&gt;.&lt;use-case&gt;</c>,
///     and two segments under <c>reads/</c> name the same from the module and
///     use-case segments alone.
/// </summary>
/// <follows>litespec/frontend/web/structure#mark-each-use-case-module-with-its-use-case</follows>
static class FrontendModuleMirror
{
    /// <summary>The use-case id the folder under a modules root mirrors, or null outside a use-case folder.</summary>
    /// <param name="frontend">The frontend whose modules root the folder sits under.</param>
    /// <param name="folder">The folder path to resolve, absolute or relative to the working directory.</param>
    /// <returns>The mirrored use-case id, or null when the folder lies outside the modules root or names no use case.</returns>
    public static string? ExpectedUseCase(FrontendConfig frontend, string folder)
    {
        var root = Path.GetFullPath(frontend.ModulesRoot);
        var full = Path.GetFullPath(folder);
        var relative = Path.GetRelativePath(root, full);

        if (Path.IsPathRooted(relative)
            || relative is "." or ".."
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            return null;
        }

        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (parts.Length == 3 && parts[1] == "reads")
        {
            return $"use-case/{parts[0]}.{parts[2]}";
        }

        return parts.Length >= 3 ? $"use-case/{parts[0]}.{parts[2]}" : null;
    }

    /// <summary>
    ///     The use-case id the mark's folder mirrors, when the mark names an existing
    ///     use case other than that one. Null for any other element, mark or path.
    /// </summary>
    /// <param name="model">The built project model holding the records the mark names.</param>
    /// <param name="element">The marked code element whose folder is checked.</param>
    /// <param name="mark">The mark on the element, naming the record the folder should mirror.</param>
    /// <returns>The use-case id the folder actually mirrors, or null when it mirrors the named record or no use case.</returns>
    public static string? PathMismatch(ProjectModel model, MarkedElement element, CodeMark mark)
    {
        if (element.Kind != CodeKind.FrontendModule)
        {
            return null;
        }

        if (!model.ById.TryGetValue(mark.RecordId, out var record) || record.Type != RecordTypes.UseCase)
        {
            return null;
        }

        foreach (var frontend in model.Config.Frontends)
        {
            var folder = Directory.Exists(element.File) ? element.File : Path.GetDirectoryName(element.File) ?? element.File;

            if (ExpectedUseCase(frontend, folder) is { } expected && expected != mark.RecordId)
            {
                return expected;
            }
        }

        return null;
    }
}
