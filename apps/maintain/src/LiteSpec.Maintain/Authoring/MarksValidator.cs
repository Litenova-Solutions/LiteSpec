using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LiteSpec.Authoring.ValidatorSupport;

namespace LiteSpec.Authoring;

/// <summary>
///     The self-mark check: every class with a method in a source project carries a
///     <c>follows</c> mark, and every <c>follows</c> and <c>proves</c> mark names a provision.
/// </summary>
/// <follows>litespec/core/maintaining#mark-the-tools-with-the-provisions-they-follow</follows>
internal sealed class MarksValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MarksValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public MarksValidator(ValidationContext context)
    {
        Context = context;
    }

    /// <summary>Gets the shared validation context.</summary>
    private ValidationContext Context { get; }

    /// <summary>Checks the follows and proves marks of every .NET project under apps.</summary>
    /// <param name="provisionIds">The active provision identifiers.</param>
    internal void CheckMarks(HashSet<string> provisionIds)
    {
        var root = Context.Root;
        var add = Context.Add;
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
            CheckProjectMarks(folder, source, provisionIds, scanned);
        }
    }

    /// <summary>Checks the marks of one project folder.</summary>
    /// <param name="folder">The project folder.</param>
    /// <param name="source">Whether the project is a source project.</param>
    /// <param name="provisionIds">The active provision identifiers.</param>
    /// <param name="scanned">The files already scanned by another project.</param>
    private void CheckProjectMarks(string folder, bool source, HashSet<string> provisionIds, HashSet<string> scanned)
    {
        var root = Context.Root;
        var add = Context.Add;
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

    /// <summary>Collects the C# source files under a folder.</summary>
    /// <param name="folder">The folder to collect from.</param>
    /// <param name="files">The collected files.</param>
    private static void CollectSources(string folder, List<string> files)
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

    /// <summary>Lists the top-level classes of a syntax tree.</summary>
    /// <param name="tree">The syntax tree.</param>
    /// <returns>The top-level classes.</returns>
    private static List<ClassDeclarationSyntax> TopLevelClasses(SyntaxTree tree) =>
        tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(node => node.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax)
            .ToList();

    /// <summary>Lists the members of a syntax tree that can carry a mark.</summary>
    /// <param name="tree">The syntax tree.</param>
    /// <returns>The markable members.</returns>
    private static IEnumerable<MemberDeclarationSyntax> MarkedMembers(SyntaxTree tree) =>
        tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>()
            .Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or MethodDeclarationSyntax);

    // Partial parts join by namespace plus class name across the project's files.
    /// <summary>Computes the namespace-qualified key of a class.</summary>
    /// <param name="node">The class declaration.</param>
    /// <returns>The class key.</returns>
    private static string ClassKey(ClassDeclarationSyntax node)
    {
        var namespaces = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(current => current.Name.ToString());
        return string.Join(".", namespaces.Append(node.Identifier.Text));
    }

    /// <summary>Reports whether a class carries a follows mark.</summary>
    /// <param name="node">The class declaration.</param>
    /// <returns>True when the class carries a follows mark.</returns>
    private static bool HasFollowsMark(ClassDeclarationSyntax node) =>
        MarkReader.Read(node).Any(mark => mark.Verb == MarkVerb.Follows);

    /// <summary>Reports whether a class carries a GeneratedCode attribute.</summary>
    /// <param name="node">The class declaration.</param>
    /// <returns>True when the class is generated code.</returns>
    private static bool HasGeneratedCode(ClassDeclarationSyntax node)
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
