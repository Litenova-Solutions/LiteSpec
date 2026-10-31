using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>
///     The cross-file names one read collects, keyed by namespace-qualified type name
///     including the containing type for a nested type, so two types with the same
///     simple name in different namespaces never share a kind, a helper or a port.
///     The marked ports of the read live here, so no read shares mutable static state.
/// </summary>
/// <param name="bySimple">Every declared type by simple name, each with its qualified name.</param>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
sealed class QualifiedKinds(IReadOnlyDictionary<string, List<DeclaredType>> bySimple)
{
    /// <summary>Gets every declared type by simple name, each with its qualified name.</summary>
    public IReadOnlyDictionary<string, List<DeclaredType>> BySimple { get; } = bySimple;

    /// <summary>Gets the resolved base names by namespace-qualified type name.</summary>
    public Dictionary<string, List<string>> Bases { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the namespace-qualified names of interfaces carrying an external-system mark.</summary>
    public HashSet<string> MarkedPorts { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the namespace-qualified names of technical classes found through another partial part.</summary>
    public HashSet<string> TechnicalClasses { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the namespace-qualified names of known aggregate roots.</summary>
    public HashSet<string> AggregateRoots { get; private set; } = new(StringComparer.Ordinal);

    /// <summary>Gets the namespace-qualified names of known domain exceptions.</summary>
    public HashSet<string> DomainExceptions { get; private set; } = new(StringComparer.Ordinal);

    /// <summary>Gets the namespace-qualified names of types known to carry <c>ICommandHandler</c>.</summary>
    public HashSet<string> CommandHandlers { get; private set; } = new(StringComparer.Ordinal);

    /// <summary>Gets the namespace-qualified names of types known to carry <c>IQueryHandler</c>.</summary>
    public HashSet<string> QueryHandlers { get; private set; } = new(StringComparer.Ordinal);

    /// <summary>Gets the namespace-qualified names of types known to carry an event-handler contract.</summary>
    public HashSet<string> EventHandlers { get; private set; } = new(StringComparer.Ordinal);

    /// <summary>Gets the namespace-qualified names of types known to be job hosts.</summary>
    public HashSet<string> JobHosts { get; private set; } = new(StringComparer.Ordinal);

    /// <summary>Gets the namespace-qualified names of types known to carry <c>IDomainEvent</c>.</summary>
    public HashSet<string> DomainEvents { get; private set; } = new(StringComparer.Ordinal);

    /// <summary>Closes the collected base names over inheritance.</summary>
    public void Complete()
    {
        AggregateRoots = Fixpoint(Bases, parents => parents.Any(parent =>
            DotnetNames.SimpleName(parent).StartsWith("AggregateRoot", StringComparison.Ordinal)));
        DomainExceptions = Fixpoint(Bases, parents => parents.Any(parent =>
            DotnetNames.SimpleName(parent) == "DomainException"));
        CommandHandlers = Fixpoint(Bases, parents => parents.Any(parent =>
            DotnetNames.SimpleName(parent) == "ICommandHandler"));
        QueryHandlers = Fixpoint(Bases, parents => parents.Any(parent =>
            DotnetNames.SimpleName(parent) == "IQueryHandler"));
        EventHandlers = Fixpoint(Bases, parents => parents.Any(parent =>
            DotnetNames.SimpleName(parent) == "IEventHandler" || DotnetNames.SimpleName(parent) == "IDomainEventReaction"));
        JobHosts = Fixpoint(Bases, parents => parents.Any(parent =>
            DotnetNames.SimpleName(parent) == "BackgroundService" || DotnetNames.SimpleName(parent) == "IHostedService"));
        DomainEvents = Fixpoint(Bases, parents => parents.Any(parent =>
            DotnetNames.SimpleName(parent) == "IDomainEvent"));
    }

    /// <summary>
    ///     The resolved base names of one declaration: each base name resolved to the
    ///     namespace-qualified name of the declared type it names, or the simple name
    ///     it was written with when no declared type carries it.
    /// </summary>
    /// <param name="declared">The type declaration.</param>
    /// <returns>The resolved base names of the declaration.</returns>
    public IReadOnlyList<string> ResolvedBases(TypeDeclarationSyntax declared) =>
        declared.BaseList is null
            ? []
            : [.. declared.BaseList.Types.Select(baseType => ResolveName(declared, DotnetNames.SimpleName(DotnetNames.KindOf(DotnetNames.BaseName(baseType.Type)))))];

    /// <summary>
    ///     Resolves a simple type name written in one scope to the namespace-qualified
    ///     name of the declared type it names: the one in the scope's namespace wins,
    ///     then the one in the scope's file, then the only candidate. An ambiguous or
    ///     undeclared name stays simple, so it matches no qualified name of another type.
    /// </summary>
    /// <param name="scope">The scope the name is written in.</param>
    /// <param name="simple">The simple type name.</param>
    /// <returns>The namespace-qualified name the simple name resolves to.</returns>
    public string ResolveName(SyntaxNode scope, string simple)
    {
        if (simple.Length == 0 || !BySimple.TryGetValue(simple, out var candidates))
        {
            return simple;
        }

        if (candidates.Count == 1)
        {
            return candidates[0].Qualified;
        }

        var space = NamespaceOfNode(scope);

        foreach (var candidate in candidates)
        {
            if (string.Equals(candidate.Namespace, space, StringComparison.Ordinal))
            {
                return candidate.Qualified;
            }
        }

        foreach (var candidate in candidates)
        {
            if (string.Equals(candidate.File, scope.SyntaxTree.FilePath, StringComparison.Ordinal))
            {
                return candidate.Qualified;
            }
        }

        return simple;
    }

    /// <summary>Whether the simple receiver type written in the scope names a marked port of this read.</summary>
    /// <param name="scope">The scope the name is written in.</param>
    /// <param name="simple">The simple type name.</param>
    /// <returns>True when the simple receiver type names a marked port of this read.</returns>
    public bool IsExternalPort(SyntaxNode scope, string simple) =>
        MarkedPorts.Contains(ResolveName(scope, simple));

    /// <summary>The namespace holding a syntax node, or empty in the global namespace.</summary>
    /// <param name="node">The syntax node.</param>
    /// <returns>The namespace holding the syntax node, or empty in the global namespace.</returns>
    private static string NamespaceOfNode(SyntaxNode node)
    {
        for (var current = node; current is not null; current = current.Parent)
        {
            if (current is BaseNamespaceDeclarationSyntax space)
            {
                return space.Name.ToString();
            }
        }

        return "";
    }

    /// <summary>Closes the seeded names over direct inheritance: a type deriving a known name is known.</summary>
    /// <param name="bases">The resolved base names by namespace-qualified type name.</param>
    /// <param name="seeds">Whether a type's direct bases seed it as known.</param>
    /// <returns>The namespace-qualified names closed over inheritance.</returns>
    private static HashSet<string> Fixpoint(
        Dictionary<string, List<string>> bases,
        Func<List<string>, bool> seeds)
    {
        var known = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (name, parents) in bases)
        {
            if (seeds(parents))
            {
                known.Add(name);
            }
        }

        var changed = true;

        while (changed)
        {
            changed = false;

            foreach (var (name, parents) in bases)
            {
                if (!known.Contains(name) && parents.Any(known.Contains))
                {
                    known.Add(name);
                    changed = true;
                }
            }
        }

        return known;
    }
}
