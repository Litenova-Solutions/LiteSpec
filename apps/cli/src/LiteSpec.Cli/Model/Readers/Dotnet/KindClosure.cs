using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>
///     Collects the cross-file names kind recognition needs: the aggregate roots,
///     the domain exceptions, the marked ports, the technical classes, and the
///     handler, event handler, job host and domain event contracts.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class KindClosure
{
    /// <summary>
    ///     Collects the cross-file names kind recognition needs from the parsed file
    ///     cache. Roots, exceptions and contracts seed from their direct bases and
    ///     close over inheritance: any type whose declared base list names a type
    ///     already known to carry the contract carries it too. Ports are the
    ///     interfaces carrying an external-system mark. Technical classes are the
    ///     top-level classes outside Domain that declare a method, so a partial part
    ///     without a method reads through its name. Collected once per read.
    /// </summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="trees">The parsed syntax trees by file path.</param>
    /// <returns>The kind context and the namespace-qualified names of the read.</returns>
    public static (KindContext Context, QualifiedKinds Qualified) Of(
        ConsumerConfig config, IReadOnlyDictionary<string, SyntaxTree> trees)
    {
        var backends = new[]
        {
            config.ApplicationRoot,
            config.ApplicationRoot + ".Abstractions",
            config.DomainRoot,
            config.WebApiRoot,
            config.InfrastructureRoot,
            config.WorkerRoot,
        };
        var bySimple = new Dictionary<string, List<DeclaredType>>(StringComparer.Ordinal);
        var declaredTypes = new List<(TypeDeclarationSyntax Declared, string Qualified, bool PortRoot, bool TechnicalRoot, string File)>();

        foreach (var root in backends)
        {
            var portRoot = root == config.ApplicationRoot
                || root == config.ApplicationRoot + ".Abstractions"
                || root == config.InfrastructureRoot;
            var technicalRoot = root != config.DomainRoot;

            foreach (var file in SourceTree.Files(root, ".cs"))
            {
                if (!trees.TryGetValue(file, out var tree))
                {
                    continue;
                }

                foreach (var declared in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    var qualified = DotnetNames.QualifiedNameOf(declared);
                    var simple = declared.Identifier.ValueText;

                    if (!bySimple.TryGetValue(simple, out var candidates))
                    {
                        candidates = [];
                        bySimple[simple] = candidates;
                    }

                    candidates.Add(new DeclaredType(qualified, DotnetNames.NamespaceOf(declared), file));
                    declaredTypes.Add((declared, qualified, portRoot, technicalRoot, file));
                }
            }
        }

        var qualifiedKinds = new QualifiedKinds(bySimple);

        foreach (var (declared, qualified, portRoot, technicalRoot, file) in declaredTypes)
        {
            if (!qualifiedKinds.Bases.TryGetValue(qualified, out var known))
            {
                known = [];
                qualifiedKinds.Bases[qualified] = known;
            }

            foreach (var face in qualifiedKinds.ResolvedBases(declared))
            {
                if (face.Length > 0 && !known.Contains(face, StringComparer.Ordinal))
                {
                    known.Add(face);
                }
            }

            if (portRoot
                && declared is InterfaceDeclarationSyntax faceType
                && MarkReader.Read(faceType).Any(mark =>
                    mark.RecordId.StartsWith("external-system/", StringComparison.Ordinal)))
            {
                qualifiedKinds.MarkedPorts.Add(qualified);
            }

            if (technicalRoot && IsTechnicalCandidate(file, declared))
            {
                qualifiedKinds.TechnicalClasses.Add(qualified);
            }
        }

        qualifiedKinds.Complete();

        static HashSet<string> SimpleNames(HashSet<string> qualified) =>
            [.. qualified.Select(name => DotnetNames.SimpleName(name))];

        var context = new KindContext(
            SimpleNames(qualifiedKinds.AggregateRoots),
            SimpleNames(qualifiedKinds.DomainExceptions),
            SimpleNames(qualifiedKinds.MarkedPorts),
            SimpleNames(qualifiedKinds.CommandHandlers),
            SimpleNames(qualifiedKinds.QueryHandlers),
            SimpleNames(qualifiedKinds.EventHandlers),
            SimpleNames(qualifiedKinds.JobHosts),
            SimpleNames(qualifiedKinds.DomainEvents),
            SimpleNames(qualifiedKinds.TechnicalClasses));

        return (context, qualifiedKinds);
    }

    /// <summary>
    ///     Whether the declaration names a technical class for the kind context: a
    ///     top-level class declaring an ordinary method, outside generated files,
    ///     generated-code attributes, migrations, and model snapshots.
    /// </summary>
    /// <param name="file">The file declaring the type.</param>
    /// <param name="declared">The type declaration.</param>
    /// <returns>True when the declaration names a technical class for the kind context.</returns>
    public static bool IsTechnicalCandidate(string file, TypeDeclarationSyntax declared)
    {
        if (declared is not ClassDeclarationSyntax
            || declared.Parent is TypeDeclarationSyntax
            || !declared.Members.OfType<MethodDeclarationSyntax>().Any())
        {
            return false;
        }

        return !GeneratedCodeFacts.IsGeneratedFileName(file) && !GeneratedCodeFacts.HasGeneratedCode(declared) && !IsMigrationBase(declared);
    }

    /// <summary>Whether the declaration derives from a type named <c>Migration</c> or <c>ModelSnapshot</c>.</summary>
    /// <param name="declared">The type declaration.</param>
    /// <returns>True when the declaration derives from a type named <c>Migration</c> or <c>ModelSnapshot</c>.</returns>
    public static bool IsMigrationBase(TypeDeclarationSyntax declared)
    {
        if (declared.BaseList is null)
        {
            return false;
        }

        foreach (var baseType in declared.BaseList.Types)
        {
            if (DotnetNames.SimpleName(DotnetNames.KindOf(DotnetNames.BaseName(baseType.Type))) is "Migration" or "ModelSnapshot")
            {
                return true;
            }
        }

        return false;
    }
}
