using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>Reads every Infrastructure class implementing a domain repository interface.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class RepositoryImplementationReader
{
    /// <summary>
    ///     Reads every Infrastructure class implementing a domain repository
    ///     interface into the model, through its declared base list: the class
    ///     name, the repository interface it names, and its location. Only files
    ///     under the Infrastructure root are read, and nested types are left out.
    /// </summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="model">The code model a repository implementation adds to.</param>
    public static void ReadRepositoryImplementations(ConsumerConfig config, string file, SyntaxTree tree, CodeModel model)
    {
        if (!SourceRoots.IsUnder(config.InfrastructureRoot, file))
        {
            return;
        }

        foreach (var declared in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (declared.Parent is TypeDeclarationSyntax || declared is InterfaceDeclarationSyntax || declared.BaseList is null)
            {
                continue;
            }

            foreach (var baseType in declared.BaseList.Types)
            {
                var face = DotnetNames.SimpleName(DotnetNames.KindOf(DotnetNames.BaseName(baseType.Type)));

                if (IsRepositoryInterface(face))
                {
                    model.RepositoryImplementations.Add(new CodeRepositoryImplementation(
                        face,
                        declared.Identifier.ValueText,
                        file,
                        tree.GetLineSpan(declared.Identifier.Span).StartLinePosition.Line + 1));
                }
            }
        }
    }

    /// <summary>Whether the name is a domain repository interface: an <c>I</c>-prefixed name ending in <c>Repository</c>.</summary>
    /// <param name="face">The interface name.</param>
    /// <returns>True when the name is a domain repository interface.</returns>
    public static bool IsRepositoryInterface(string face) =>
        face.Length > "IRepository".Length
        && face.StartsWith('I')
        && char.IsUpper(face[1])
        && face.EndsWith("Repository", StringComparison.Ordinal);

    /// <summary>
    ///     The simple names of the classes whose own constructor takes a repository: a
    ///     parameter whose kind starts with <c>I</c> and ends with <c>Repository</c>.
    /// </summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="trees">The parsed syntax trees by file path.</param>
    /// <returns>The simple names of the classes whose own constructor takes a repository.</returns>
    public static HashSet<string> RepositoryHoldersOf(ConsumerConfig config, IReadOnlyDictionary<string, SyntaxTree> trees)
    {
        var holders = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in new[] { config.ApplicationRoot, config.ApplicationRoot + ".Abstractions", config.DomainRoot, config.WebApiRoot, config.InfrastructureRoot, config.WorkerRoot })
        {
            foreach (var file in SourceTree.Files(root, ".cs"))
            {
                if (!trees.TryGetValue(file, out var tree))
                {
                    continue;
                }

                foreach (var declared in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    if (ClassDetailsReader.ConstructorTypes(declared, tree)
                        .Select(entry => DotnetNames.KindOf(entry.Type))
                        .Any(kind => kind.StartsWith('I') && kind.EndsWith("Repository", StringComparison.Ordinal)))
                    {
                        holders.Add(declared.Identifier.ValueText);
                    }
                }
            }
        }

        return holders;
    }
}
