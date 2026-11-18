using LiteSpec.Cli.Model.Code;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Backend;

/// <summary>The calls a class makes on an aggregate root it holds, or on a root type.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class AggregateCallReader
{
    /// <summary>
    ///     The calls a handler makes on an aggregate root it holds: an invocation on a
    ///     local, parameter or field whose type is a domain aggregate root, in line order.
    /// </summary>
    /// <param name="declared">The class declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <param name="aggregateRoots">The simple names of known aggregate roots.</param>
    /// <returns>The calls the class makes on a held aggregate root, in line order.</returns>
    public static List<CodeCall> AggregateCallsOf(
        BaseTypeDeclarationSyntax declared,
        SyntaxTree tree,
        string file,
        HashSet<string> aggregateRoots)
    {
        var found = new List<CodeCall>();

        if (aggregateRoots.Count == 0)
        {
            return found;
        }

        var receivers = ClassDetailsReader.ReceiverTypes(declared);
        var fields = FieldTypes(declared);
        var aliases = UsingAliases(tree);

        foreach (var invocation in declared.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member)
            {
                continue;
            }

            var receiver = member.Expression switch
            {
                IdentifierNameSyntax plain => plain.Identifier.ValueText,
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access => access.Name.Identifier.ValueText,
                _ => null,
            };

            // A name the class neither injects, holds nor declares is a static call on a
            // type rather than a call on a held aggregate.
            if (receiver is null)
            {
                continue;
            }

            if (!receivers.TryGetValue(receiver, out var type)
                && !fields.TryGetValue(receiver, out type))
            {
                if (AggregateLocalType(invocation, receiver, receivers, aliases, aggregateRoots) is not { } local)
                {
                    continue;
                }

                type = local;
            }

            if (aggregateRoots.Contains(ResolveAlias(aliases, DotnetNames.SimpleName(type))))
            {
                found.Add(new CodeCall(
                    receiver,
                    member.Name.Identifier.ValueText,
                    file,
                    tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1));
            }
        }

        found.Sort((left, right) => left.Line.CompareTo(right.Line));

        return found;
    }

    /// <summary>
    ///     The static calls a handler makes on aggregate root types: an invocation
    ///     whose bare receiver names a root type rather than a held value, with the
    ///     root type name as receiver, in line order.
    /// </summary>
    /// <param name="declared">The class declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <param name="aggregateRoots">The simple names of known aggregate roots.</param>
    /// <returns>The static calls the class makes on aggregate root types, in line order.</returns>
    public static List<CodeCall> FactoryCallsOf(
        BaseTypeDeclarationSyntax declared,
        SyntaxTree tree,
        string file,
        HashSet<string> aggregateRoots)
    {
        var found = new List<CodeCall>();

        if (aggregateRoots.Count == 0)
        {
            return found;
        }

        var receivers = ClassDetailsReader.ReceiverTypes(declared);
        var fields = FieldTypes(declared);
        var aliases = UsingAliases(tree);

        foreach (var invocation in declared.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member
                || member.Expression is not IdentifierNameSyntax plain)
            {
                continue;
            }

            var name = plain.Identifier.ValueText;

            // A name the class injects, holds or declares is a call on a held
            // aggregate; only a bare root type name is a static factory call.
            if (receivers.ContainsKey(name)
                || fields.ContainsKey(name)
                || AggregateLocalType(invocation, name, receivers, aliases, aggregateRoots) is not null
                || !aggregateRoots.Contains(ResolveAlias(aliases, name)))
            {
                continue;
            }

            found.Add(new CodeCall(
                ResolveAlias(aliases, name),
                member.Name.Identifier.ValueText,
                file,
                tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1));
        }

        found.Sort((left, right) => left.Line.CompareTo(right.Line));

        return found;
    }

    /// <summary>The declared field and property types a class holds, by member name.</summary>
    /// <param name="declared">The class declaration.</param>
    /// <returns>The declared field and property types the class holds, by member name.</returns>
    public static Dictionary<string, string> FieldTypes(BaseTypeDeclarationSyntax declared)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);

        if (declared is not TypeDeclarationSyntax typed)
        {
            return types;
        }

        foreach (var field in typed.Members.OfType<FieldDeclarationSyntax>())
        {
            foreach (var variable in field.Declaration.Variables)
            {
                types.TryAdd(variable.Identifier.ValueText, DotnetNames.SimpleTypeName(field.Declaration.Type));
            }
        }

        foreach (var property in typed.Members.OfType<PropertyDeclarationSyntax>())
        {
            types.TryAdd(property.Identifier.ValueText, DotnetNames.SimpleTypeName(property.Type));
        }

        return types;
    }

    /// <summary>
    ///     The type a local holds: what it builds with <c>new</c>, its declared type, the
    ///     aggregate a repository load returns, inferred from the repository interface, the
    ///     element a loop variable takes from what it enumerates, or a method parameter's type.
    /// </summary>
    /// <param name="invocation">The invocation on the local.</param>
    /// <param name="name">The local's name.</param>
    /// <param name="receivers">The injected names the class may call, each with its type.</param>
    /// <returns>The type the local holds, or null when it holds none the reader names.</returns>
    public static string? AggregateLocalType(
        InvocationExpressionSyntax invocation,
        string name,
        Dictionary<string, string> receivers) =>
        AggregateLocalType(invocation, name, receivers, new Dictionary<string, string>(), new HashSet<string>(), []);

    /// <summary>
    ///     The type a local holds: what it builds with <c>new</c>, its declared type, the
    ///     aggregate a repository load returns, inferred from the repository interface, the
    ///     root a static call on a root type returns, the element a loop variable takes from
    ///     what it enumerates, or a method parameter's type.
    /// </summary>
    /// <param name="invocation">The invocation on the local.</param>
    /// <param name="name">The local's name.</param>
    /// <param name="receivers">The injected names the class may call, each with its type.</param>
    /// <param name="aliases">The file's using aliases, each with the simple name it names.</param>
    /// <param name="aggregateRoots">The simple names of known aggregate roots.</param>
    /// <returns>The type the local holds, or null when it holds none the reader names.</returns>
    public static string? AggregateLocalType(
        InvocationExpressionSyntax invocation,
        string name,
        Dictionary<string, string> receivers,
        Dictionary<string, string> aliases,
        HashSet<string> aggregateRoots) =>
        AggregateLocalType(invocation, name, receivers, aliases, aggregateRoots, []);

    /// <summary>The type a local holds, following loop variables through what they enumerate.</summary>
    /// <param name="invocation">The invocation on the local.</param>
    /// <param name="name">The local's name.</param>
    /// <param name="receivers">The injected names the class may call, each with its type.</param>
    /// <param name="aliases">The file's using aliases, each with the simple name it names.</param>
    /// <param name="aggregateRoots">The simple names of known aggregate roots.</param>
    /// <param name="seen">The names already followed, so a loop enumerating itself ends.</param>
    /// <returns>The type the local holds, or null when it holds none the reader names.</returns>
    static string? AggregateLocalType(
        InvocationExpressionSyntax invocation,
        string name,
        Dictionary<string, string> receivers,
        Dictionary<string, string> aliases,
        HashSet<string> aggregateRoots,
        HashSet<string> seen)
    {
        if (!seen.Add(name))
        {
            return null;
        }

        var scope = invocation.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();

        if (scope is null)
        {
            return null;
        }

        foreach (var declarator in scope.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (declarator.Identifier.ValueText != name)
            {
                continue;
            }

            if (declarator.Initializer?.Value is ObjectCreationExpressionSyntax creation)
            {
                return DotnetNames.SimpleTypeName(creation.Type);
            }

            if (LoadTarget(declarator.Initializer?.Value) is AwaitExpressionSyntax awaited
                && UnwrapAwaited(awaited.Expression) is InvocationExpressionSyntax load
                && load.Expression is MemberAccessExpressionSyntax member
                && member.Expression is IdentifierNameSyntax repository
                && receivers.TryGetValue(repository.Identifier.ValueText, out var repoType)
                && DotnetNames.KindOf(repoType).EndsWith("Repository", StringComparison.Ordinal)
                && member.Name.Identifier.ValueText is { } method
                && method.EndsWith("Async", StringComparison.Ordinal)
                && (method.StartsWith("Get", StringComparison.Ordinal)
                    || method.StartsWith("Find", StringComparison.Ordinal)
                    || method.StartsWith("Load", StringComparison.Ordinal)
                    || method.StartsWith("Single", StringComparison.Ordinal)
                    || method.StartsWith("List", StringComparison.Ordinal))
                && RepositoryAggregateName(DotnetNames.KindOf(repoType)) is { } aggregate)
            {
                return aggregate;
            }

            if (declarator.Parent is VariableDeclarationSyntax declaration
                && declaration.Type is not IdentifierNameSyntax { Identifier.ValueText: "var" })
            {
                return DotnetNames.SimpleTypeName(declaration.Type);
            }

            if (declarator.Initializer?.Value is InvocationExpressionSyntax factory
                && factory.Expression is MemberAccessExpressionSyntax factoryMember
                && factoryMember.Expression is IdentifierNameSyntax factoryType
                && aggregateRoots.Contains(ResolveAlias(aliases, factoryType.Identifier.ValueText)))
            {
                return ResolveAlias(aliases, factoryType.Identifier.ValueText);
            }
        }

        foreach (var loop in scope.DescendantNodes().OfType<ForEachStatementSyntax>())
        {
            if (loop.Identifier.ValueText == name
                && loop.Expression is IdentifierNameSyntax enumerated
                && AggregateLocalType(invocation, enumerated.Identifier.ValueText, receivers, aliases, aggregateRoots, seen) is { } element)
            {
                return ElementType(element);
            }
        }

        if (scope is MethodDeclarationSyntax methodScope)
        {
            foreach (var parameter in methodScope.ParameterList.Parameters)
            {
                if (parameter.Identifier.ValueText == name && parameter.Type is not null)
                {
                    return DotnetNames.SimpleTypeName(parameter.Type);
                }
            }
        }

        return null;
    }

    /// <summary>The using aliases the file declares, each with the simple name it names.</summary>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <returns>The alias name with its target's simple name.</returns>
    static Dictionary<string, string> UsingAliases(SyntaxTree tree)
    {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var directive in tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (directive.Alias is null || directive.Name is null)
            {
                continue;
            }

            var target = directive.Name.ToString();
            var generic = target.IndexOf('<');

            if (generic >= 0)
            {
                target = target[..generic];
            }

            var dot = target.LastIndexOf('.');

            aliases[directive.Alias.Name.Identifier.ValueText] = dot >= 0 ? target[(dot + 1)..] : target;
        }

        return aliases;
    }

    /// <summary>The simple name a file name means: its alias target, or itself when the file aliases nothing by it.</summary>
    /// <param name="aliases">The file's using aliases, each with the simple name it names.</param>
    /// <param name="name">The name as written.</param>
    /// <returns>The alias target's simple name, or the name itself.</returns>
    static string ResolveAlias(Dictionary<string, string> aliases, string name) =>
        aliases.TryGetValue(name, out var target) ? target : name;

    /// <summary>The loaded value of an initializer: the left side of a null-coalescing fallback, or the value itself.</summary>
    /// <param name="value">The local's initializer value.</param>
    /// <returns>The loaded value, or null when the local has no initializer.</returns>
    static ExpressionSyntax? LoadTarget(ExpressionSyntax? value) =>
        value is BinaryExpressionSyntax coalesce && coalesce.IsKind(SyntaxKind.CoalesceExpression)
            ? coalesce.Left
            : value;

    /// <summary>The awaited call without its <c>ConfigureAwait</c>, so an awaited load reads through it.</summary>
    /// <param name="awaited">The awaited expression.</param>
    /// <returns>The repository load call, or the expression itself when it configures no await.</returns>
    static ExpressionSyntax UnwrapAwaited(ExpressionSyntax awaited)
    {
        if (awaited is InvocationExpressionSyntax configured
            && configured.Expression is MemberAccessExpressionSyntax configuredMember
            && configuredMember.Name.Identifier.ValueText == "ConfigureAwait"
            && configuredMember.Expression is InvocationExpressionSyntax inner)
        {
            return inner;
        }

        return awaited;
    }

    /// <summary>The element type a loop variable takes: the type itself, or its first type argument for a collection.</summary>
    /// <param name="enumerated">The resolved type of what the loop enumerates.</param>
    /// <returns>The element type the loop variable holds.</returns>
    static string ElementType(string enumerated)
    {
        var open = enumerated.IndexOf('<');
        var close = enumerated.LastIndexOf('>');

        if (open < 0 || close < open)
        {
            return enumerated;
        }

        var first = enumerated[(open + 1)..close].Split(',')[0].Trim();
        var dot = first.LastIndexOf('.');

        return dot >= 0 ? first[(dot + 1)..] : first;
    }

    /// <summary>The aggregate a repository interface serves, or null when it names none.</summary>
    /// <param name="face">The repository interface name.</param>
    /// <returns>The interface name without its leading I and trailing Repository, or null when it does not end with Repository.</returns>
    public static string? RepositoryAggregateName(string face)
    {
        var type = face;

        if (type.StartsWith('I') && type.Length > 1 && char.IsUpper(type[1]))
        {
            type = type[1..];
        }

        return type.EndsWith("Repository", StringComparison.Ordinal) && type.Length > "Repository".Length
            ? type[..^"Repository".Length]
            : null;
    }
}
