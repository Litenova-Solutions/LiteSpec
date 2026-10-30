using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Authoring;

/// <summary>
///     The self-mark check: every class with a method in a source project carries a
///     <c>follows</c> mark, and every <c>follows</c> and <c>proves</c> mark names a provision.
/// </summary>
/// <follows>litespec/core/maintaining#mark-the-tools-with-the-provisions-they-follow</follows>
static partial class StandardsValidator
{
    static void CheckMarks(string root, HashSet<string> provisionIds, Action<string, int, string, string> add)
    {
        var apps = Path.Combine(root, "apps");
        if (!Directory.Exists(apps))
        {
            return;
        }

        // One entry per .NET project: the folder holding its project file, and
        // whether it sits under a src/ or a tests/ folder.
        var projects = new List<(string Folder, bool Source)>();
        foreach (var area in Directory.GetDirectories(apps).OrderBy(name => name, StringComparer.Ordinal))
        {
            foreach (var (kind, source) in new[] { ("src", true), ("tests", false) })
            {
                var kindRoot = Path.Combine(area, kind);
                if (!Directory.Exists(kindRoot))
                {
                    continue;
                }

                foreach (var project in Directory.GetFiles(kindRoot, "*.csproj", SearchOption.AllDirectories).OrderBy(name => name, StringComparer.Ordinal))
                {
                    projects.Add((Path.GetDirectoryName(project)!, source));
                }
            }
        }

        // A project folder nested inside another would scan its files twice, so
        // each file is parsed once no matter how many projects cover it.
        var scanned = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (folder, source) in projects)
        {
            CheckProjectMarks(root, folder, source, provisionIds, add, scanned);
        }
    }

    static void CheckProjectMarks(string root, string folder, bool source, HashSet<string> provisionIds, Action<string, int, string, string> add, HashSet<string> scanned)
    {
        var files = new List<string>();
        CollectSources(folder, files);
        files.Sort(StringComparer.Ordinal);

        // Partial parts share one mark: the first pass records every class key
        // that carries a follows mark on any part, and the second reports the
        // parts of a class that no part marks.
        var trees = new List<(string File, string Relative, SyntaxTree Tree)>();
        var followed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            if (!scanned.Add(file))
            {
                continue;
            }

            var tree = CSharpSyntaxTree.ParseText(ReadText(file));
            trees.Add((file, Slash(Path.GetRelativePath(root, file)), tree));
            foreach (var node in TopLevelClasses(tree))
            {
                if (HasFollowsMark(node))
                {
                    followed.Add(ClassKey(node));
                }
            }
        }

        foreach (var (file, relative, tree) in trees)
        {
            var generated = file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase);
            if (source && !generated)
            {
                foreach (var node in TopLevelClasses(tree))
                {
                    if (node.Members.OfType<MethodDeclarationSyntax>().Any()
                        && !followed.Contains(ClassKey(node))
                        && !HasGeneratedCode(node))
                    {
                        var line = node.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        add(relative, line, "MARK_MISSING", $"class '{node.Identifier.Text}' declares a method and carries no follows mark");
                    }
                }
            }

            foreach (var member in MarkedMembers(tree))
            {
                foreach (var mark in MarkReader.Read(member))
                {
                    if (mark.Verb is MarkVerb.Follows or MarkVerb.Proves && !provisionIds.Contains(mark.RecordId))
                    {
                        add(relative, mark.Line, "MARK_UNRESOLVED", $"mark names '{mark.RecordId}', which is no provision in docs/reference/provisions.md");
                    }
                }
            }
        }
    }

    static void CollectSources(string folder, List<string> files)
    {
        foreach (var entry in Directory.GetFileSystemEntries(folder).OrderBy(name => name, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(entry) ?? "";
            if (Directory.Exists(entry))
            {
                // Build output is never authored, and a Fixtures folder holds
                // consumer-shaped test data that can never carry a provision mark.
                if (name is "bin" or "obj" or "Fixtures")
                {
                    continue;
                }

                // Another checkout nested in this one (a worktree) is not this repository's code.
                if (Path.Exists(Path.Combine(entry, ".git")))
                {
                    continue;
                }

                CollectSources(entry, files);
            }
            else if (name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(entry);
            }
        }
    }

    static List<ClassDeclarationSyntax> TopLevelClasses(SyntaxTree tree) =>
        tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(node => node.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax)
            .ToList();

    static IEnumerable<MemberDeclarationSyntax> MarkedMembers(SyntaxTree tree) =>
        tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>()
            .Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or MethodDeclarationSyntax);

    // Partial parts join by namespace plus class name across the project's files.
    static string ClassKey(ClassDeclarationSyntax node)
    {
        var namespaces = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(current => current.Name.ToString());
        return string.Join(".", namespaces.Append(node.Identifier.Text));
    }

    static bool HasFollowsMark(ClassDeclarationSyntax node) =>
        MarkReader.Read(node).Any(mark => mark.Verb == MarkVerb.Follows);

    static bool HasGeneratedCode(ClassDeclarationSyntax node)
    {
        foreach (var attribute in node.AttributeLists.SelectMany(list => list.Attributes))
        {
            var name = attribute.Name.ToString().Split('.').Last();
            if (name.EndsWith("Attribute", StringComparison.Ordinal))
            {
                name = name[..^"Attribute".Length];
            }

            if (name == "GeneratedCode")
            {
                return true;
            }
        }

        return false;
    }
}
