using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

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
                if (AggregateLocalType(invocation, receiver, receivers) is not { } local)
                {
                    continue;
                }

                type = local;
            }

            if (aggregateRoots.Contains(DotnetNames.SimpleName(type)))
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
                || AggregateLocalType(invocation, name, receivers) is not null
                || !aggregateRoots.Contains(name))
            {
                continue;
            }

            found.Add(new CodeCall(
                name,
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
    ///     The type a local holds: what it builds with <c>new</c>, its declared type, or
    ///     the aggregate a repository load returns, inferred from the repository interface.
    /// </summary>
    /// <param name="invocation">The invocation on the local.</param>
    /// <param name="name">The local's name.</param>
    /// <param name="receivers">The injected names the class may call, each with its type.</param>
    /// <returns>The type the local holds, or null when it holds none the reader names.</returns>
    public static string? AggregateLocalType(
        InvocationExpressionSyntax invocation,
        string name,
        Dictionary<string, string> receivers)
    {
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

            if (declarator.Initializer?.Value is AwaitExpressionSyntax awaited
                && awaited.Expression is InvocationExpressionSyntax load
                && load.Expression is MemberAccessExpressionSyntax member
                && member.Expression is IdentifierNameSyntax repository
                && receivers.TryGetValue(repository.Identifier.ValueText, out var repoType)
                && DotnetNames.KindOf(repoType).EndsWith("Repository", StringComparison.Ordinal)
                && member.Name.Identifier.ValueText is { } method
                && method.EndsWith("Async", StringComparison.Ordinal)
                && (method.StartsWith("Get", StringComparison.Ordinal)
                    || method.StartsWith("Find", StringComparison.Ordinal)
                    || method.StartsWith("Load", StringComparison.Ordinal)
                    || method.StartsWith("Single", StringComparison.Ordinal))
                && RepositoryAggregateName(DotnetNames.KindOf(repoType)) is { } aggregate)
            {
                return aggregate;
            }

            if (declarator.Parent is VariableDeclarationSyntax declaration
                && declaration.Type is not IdentifierNameSyntax { Identifier.ValueText: "var" })
            {
                return DotnetNames.SimpleTypeName(declaration.Type);
            }
        }

        return null;
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
