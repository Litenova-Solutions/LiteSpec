using System.Text.RegularExpressions;

namespace LiteSpec.Model.Readers.TypeScript;

/// <summary>
///     The frontend mark: one <c>litespec</c> constant per use-case module folder,
///     read from TypeScript and checked against the folder path it mirrors.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static partial class FrontendMarks
{
    /// <summary>The modules root: <c>src/modules</c> on React, <c>src/lib/modules</c> on Svelte.</summary>
    public static string ModulesRoot(FrontendConfig frontend) =>
        frontend.Platform == "react"
            ? Path.Combine(frontend.Path, "src", "modules")
            : Path.Combine(frontend.Path, "src", "lib", "modules");

    /// <summary>
    ///     Every use-case folder under the modules root: one folder per backend module,
    ///     aggregate and use case, three levels deep, in a stable order.
    /// </summary>
    public static IReadOnlyList<string> ModuleFolders(string modulesRoot)
    {
        if (!Directory.Exists(modulesRoot))
        {
            return [];
        }

        var found = new List<string>();

        foreach (var module in OrderedDirectories(modulesRoot))
        {
            foreach (var aggregate in OrderedDirectories(module))
            {
                found.AddRange(OrderedDirectories(aggregate));
            }
        }

        return found;
    }

    /// <summary>
    ///     The mark a use-case folder exports: the <c>implements</c> value of its
    ///     <c>litespec</c> constant with its file and line, read from <c>litespec.ts</c>
    ///     first, then from any other <c>.ts</c> file in the folder, or null when no
    ///     file exports one.
    /// </summary>
    public static (string File, string Id, int Line)? ReadModuleMark(string folder)
    {
        var files = Directory.EnumerateFiles(folder, "*.ts", SearchOption.TopDirectoryOnly)
            .OrderBy(file => Path.GetFileName(file).Equals("litespec.ts", StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(file => file, StringComparer.Ordinal);

        foreach (var file in files)
        {
            if (TryReadMark(SourceTree.Read(file), out var id, out var line))
            {
                return (Path.GetFullPath(file), id, line);
            }
        }

        return null;
    }

    /// <summary>
    ///     Whether the text exports a <c>litespec</c> constant with a readable
    ///     <c>implements</c> value, and that value with its line. Comments read as
    ///     nothing, so a quoted example marks nothing.
    /// </summary>
    public static bool TryReadMark(string text, out string id, out int line)
    {
        var match = LiteSpecMark().Match(TypeScriptSource.WithoutComments(text));

        if (!match.Success)
        {
            id = "";
            line = 1;
            return false;
        }

        id = match.Groups["id"].Value;
        line = LineOf(text, match.Groups["id"].Index);
        return true;
    }

    /// <summary>
    ///     The use-case id a path under the modules root mirrors:
    ///     <c>modules/spending/expenses/submit-expense/</c> mirrors
    ///     <c>use-case/spending.submit-expense</c>. Null outside the modules root or
    ///     above the use-case level.
    /// </summary>
    public static string? ExpectedUseCase(FrontendConfig frontend, string path)
    {
        var relative = Path.GetRelativePath(ModulesRoot(frontend), Path.GetFullPath(path));

        if (Path.IsPathRooted(relative)
            || relative is "." or ".."
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            return null;
        }

        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return parts.Length >= 3 ? $"use-case/{parts[0]}.{parts[2]}" : null;
    }

    /// <summary>
    ///     The use-case id the mark's folder mirrors, when the mark names an existing
    ///     use case other than that one. Null for any other element, mark or path, so
    ///     the existing reasons report unknown ids and wrong record types on their own.
    /// </summary>
    public static string? PathMismatch(ProjectModel model, MarkedElement element, CodeMark mark)
    {
        if (element.Kind != CodeKind.FrontendModule)
        {
            return null;
        }

        if (!model.ById.TryGetValue(mark.RecordId, out var record) || record.Type != "use-case")
        {
            return null;
        }

        foreach (var frontend in model.Config.Frontends)
        {
            if (ExpectedUseCase(frontend, mark.File) is { } expected && expected != mark.RecordId)
            {
                return expected;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> OrderedDirectories(string folder) =>
        [.. Directory.EnumerateDirectories(folder).Order(StringComparer.Ordinal)];

    private static int LineOf(string text, int offset)
    {
        var line = 1;

        for (var index = 0; index < offset && index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    [GeneratedRegex(@"export\s+const\s+litespec\b[^=;{]*=\s*\{[^};]*?[""']?implements[""']?\s*:\s*[""'](?<id>[^""'\r\n]+)[""']")]
    private static partial Regex LiteSpecMark();
}
