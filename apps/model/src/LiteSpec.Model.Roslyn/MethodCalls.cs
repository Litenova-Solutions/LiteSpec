using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     The methods one class calls on other types: each static call on a named type and each
///     instance call on a held value, local or parameter whose type the file states, with the
///     called type and method. Calls onto the class itself read nothing here, because the
///     handler's own read already covers its body. The refusal check reads this, so a code
///     thrown one hop from the handler meets a result its use case declares.
/// </summary>
/// <follows>litespec/backend/application#return-only-declared-results-and-refusals</follows>
public static class MethodCalls
{
    /// <summary>One call onto another type: the called type with the called method.</summary>
    public sealed class TypeCall
    {
        /// <summary>Initializes a new instance of the <see cref="TypeCall" /> class.</summary>
        /// <param name="type">The simple name of the called type.</param>
        /// <param name="method">The called method name.</param>
        public TypeCall(string type, string method)
        {
            Type = type;
            Method = method;
        }

        /// <summary>Gets the simple name of the called type.</summary>
        public string Type { get; }

        /// <summary>Gets the called method name.</summary>
        public string Method { get; }
    }

    /// <summary>
    ///     Reads the methods the class calls on other types, in source order, without duplicates.
    ///     With a semantic model each called type resolves through the invoked symbol, so a call
    ///     on a value the file never names a type for still reads its type; without one a call
    ///     whose receiver type the file never states reads nothing here.
    /// </summary>
    /// <param name="declared">The class declaration to read.</param>
    /// <param name="aggregateRoots">The simple names of the known aggregate roots.</param>
    /// <param name="semanticModel">The semantic model of the declaration, or null to guess receivers from syntax.</param>
    /// <returns>The called types with their methods, in source order, without duplicates.</returns>
    /// <exception cref="ArgumentNullException">The declaration or the aggregate roots are null.</exception>
    public static IReadOnlyList<TypeCall> OfClass(TypeDeclarationSyntax declared, ISet<string> aggregateRoots, SemanticModel? semanticModel = null)
    {
        if (declared is null)
        {
            throw new ArgumentNullException(nameof(declared));
        }

        if (aggregateRoots is null)
        {
            throw new ArgumentNullException(nameof(aggregateRoots));
        }

        var receivers = ReceiverTypes(declared);
        var found = new List<TypeCall>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var self = semanticModel?.GetDeclaredSymbol(declared) as INamedTypeSymbol;

        foreach (var invocation in declared.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member)
            {
                continue;
            }

            var method = member.Name.Identifier.ValueText;

            if (semanticModel is not null && SemanticCallee(semanticModel, invocation, declared, self, out var resolved))
            {
                if (resolved is not null && seen.Add(resolved + "." + method))
                {
                    found.Add(new TypeCall(resolved, method));
                }

                continue;
            }

            if (CalleeOf(invocation, member, declared, receivers, aggregateRoots) is not string type)
            {
                continue;
            }

            if (seen.Add(type + "." + method))
            {
                found.Add(new TypeCall(type, method));
            }
        }

        return found;
    }

    /// <summary>
    ///     Whether the invocation resolves through its symbol: true with the containing type name,
    ///     true with null for a call onto the class itself, and false when the symbol names no
    ///     type, so the call falls back to the syntax guess.
    /// </summary>
    /// <param name="semanticModel">The semantic model of the declaration.</param>
    /// <param name="invocation">The invocation.</param>
    /// <param name="declared">The class declaration holding the invocation.</param>
    /// <param name="self">The declared class symbol, or null when it resolves to none.</param>
    /// <param name="type">The simple name of the called type, or null for a call onto the class itself.</param>
    /// <returns>True when the symbol decided the call; otherwise false.</returns>
    private static bool SemanticCallee(SemanticModel semanticModel, InvocationExpressionSyntax invocation, TypeDeclarationSyntax declared, INamedTypeSymbol? self, out string? type)
    {
        type = null;

        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
            || method.ContainingType is not { } containing
            || containing.Name.Length == 0)
        {
            return false;
        }

        if (self is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(containing, self))
            {
                return true;
            }
        }
        else if (containing.Name == declared.Identifier.ValueText)
        {
            return true;
        }

        type = containing.Name;

        return true;
    }

    /// <summary>The called type of one invocation, or null when it calls the class itself or names no type.</summary>
    /// <param name="invocation">The invocation.</param>
    /// <param name="member">The member access the invocation calls.</param>
    /// <param name="declared">The class declaration holding the invocation.</param>
    /// <param name="receivers">The injected and held names with their simple types.</param>
    /// <param name="aggregateRoots">The simple names of the known aggregate roots.</param>
    /// <returns>The simple name of the called type, or null.</returns>
    private static string? CalleeOf(
        InvocationExpressionSyntax invocation,
        MemberAccessExpressionSyntax member,
        TypeDeclarationSyntax declared,
        Dictionary<string, string> receivers,
        ISet<string> aggregateRoots)
    {
        switch (member.Expression)
        {
            case IdentifierNameSyntax plain:
                var name = plain.Identifier.ValueText;

                if (name.Length == 0 || name == declared.Identifier.ValueText)
                {
                    return null;
                }

                if (receivers.TryGetValue(name, out var held) && held.Length > 0)
                {
                    return held;
                }

                if (LocalType(invocation, name, receivers, aggregateRoots) is string local && local.Length > 0)
                {
                    return local;
                }

                return char.IsUpper(name[0]) ? name : null;

            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax }:
                return null;

            case MemberAccessExpressionSyntax chain:
                var type = chain.Name.Identifier.ValueText;

                return type.Length == 0 || type == declared.Identifier.ValueText ? null : type;

            default:
                return null;
        }
    }

    /// <summary>
    ///     The injected names a class may call: its constructor parameters and the fields and
    ///     properties it declares, each with its simple type name.
    /// </summary>
    /// <param name="declared">The class declaration.</param>
    /// <returns>The injected and held names with their simple types.</returns>
    private static Dictionary<string, string> ReceiverTypes(TypeDeclarationSyntax declared)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);

        if (declared.ParameterList is not null)
        {
            foreach (var parameter in declared.ParameterList.Parameters)
            {
                AddParameter(types, parameter);
            }
        }

        foreach (var constructor in declared.Members.OfType<ConstructorDeclarationSyntax>())
        {
            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                AddParameter(types, parameter);
            }
        }

        foreach (var field in declared.Members.OfType<FieldDeclarationSyntax>())
        {
            foreach (var variable in field.Declaration.Variables)
            {
                if (SimpleTypeName(field.Declaration.Type) is string type)
                {
                    AddType(types, variable.Identifier.ValueText, type);
                }
            }
        }

        foreach (var property in declared.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (SimpleTypeName(property.Type) is string type)
            {
                AddType(types, property.Identifier.ValueText, type);
            }
        }

        return types;
    }

    /// <summary>Adds one constructor parameter with its simple type.</summary>
    /// <param name="types">The receiver types read so far.</param>
    /// <param name="parameter">The parameter to add.</param>
    private static void AddParameter(Dictionary<string, string> types, ParameterSyntax parameter)
    {
        if (parameter.Type is not null
            && parameter.Identifier.ValueText.Length > 0
            && SimpleTypeName(parameter.Type) is string type)
        {
            AddType(types, parameter.Identifier.ValueText, type);
        }
    }

    /// <summary>Adds one name with its type when the name is not held yet.</summary>
    /// <param name="types">The receiver types read so far.</param>
    /// <param name="name">The name to add.</param>
    /// <param name="type">The simple type name.</param>
    private static void AddType(Dictionary<string, string> types, string name, string type)
    {
        if (!types.ContainsKey(name))
        {
            types[name] = type;
        }
    }

    /// <summary>
    ///     The type a local holds: what it builds with <c>new</c>, its declared type, the aggregate
    ///     a repository load returns, inferred from the repository interface, the root a static call
    ///     on a root type returns, or a method parameter's type.
    /// </summary>
    /// <param name="invocation">The invocation on the local.</param>
    /// <param name="name">The local's name.</param>
    /// <param name="receivers">The injected and held names with their simple types.</param>
    /// <param name="aggregateRoots">The simple names of the known aggregate roots.</param>
    /// <returns>The type the local holds, or null when it holds none the reader names.</returns>
    private static string? LocalType(
        InvocationExpressionSyntax invocation,
        string name,
        Dictionary<string, string> receivers,
        ISet<string> aggregateRoots)
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

            if (declarator.Initializer?.Value is ObjectCreationExpressionSyntax creation
                && SimpleTypeName(creation.Type) is string built)
            {
                return built;
            }

            if (LoadedAggregate(declarator.Initializer?.Value, receivers) is string loaded)
            {
                return loaded;
            }

            if (declarator.Parent is VariableDeclarationSyntax declaration
                && declaration.Type is not IdentifierNameSyntax { Identifier.ValueText: "var" }
                && SimpleTypeName(declaration.Type) is string stated)
            {
                return stated;
            }

            if (declarator.Initializer?.Value is InvocationExpressionSyntax factory
                && factory.Expression is MemberAccessExpressionSyntax factoryMember
                && factoryMember.Expression is IdentifierNameSyntax factoryType
                && aggregateRoots.Contains(factoryType.Identifier.ValueText))
            {
                return factoryType.Identifier.ValueText;
            }
        }

        SeparatedSyntaxList<ParameterSyntax>? parameters = scope switch
        {
            MethodDeclarationSyntax method => method.ParameterList.Parameters,
            ConstructorDeclarationSyntax constructor => constructor.ParameterList.Parameters,
            _ => null,
        };

        if (parameters is not null)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Identifier.ValueText == name
                    && parameter.Type is not null
                    && SimpleTypeName(parameter.Type) is string held)
                {
                    return held;
                }
            }
        }

        return null;
    }

    /// <summary>
    ///     The aggregate a repository load returns: an awaited <c>Get</c>, <c>Find</c>, <c>Load</c>,
    ///     <c>Single</c> or <c>List</c> call on a held repository, through a null-coalescing fallback
    ///     and a <c>ConfigureAwait</c>, inferred from the repository interface. Null otherwise.
    /// </summary>
    /// <param name="value">The local's initializer value.</param>
    /// <param name="receivers">The injected and held names with their simple types.</param>
    /// <returns>The aggregate the load returns, or null when the value loads none.</returns>
    private static string? LoadedAggregate(ExpressionSyntax? value, Dictionary<string, string> receivers)
    {
        var loaded = value is BinaryExpressionSyntax coalesce && coalesce.IsKind(SyntaxKind.CoalesceExpression)
            ? coalesce.Left
            : value;

        if (loaded is not AwaitExpressionSyntax awaited)
        {
            return null;
        }

        var called = awaited.Expression;

        if (called is InvocationExpressionSyntax configured
            && configured.Expression is MemberAccessExpressionSyntax configuredMember
            && configuredMember.Name.Identifier.ValueText == "ConfigureAwait"
            && configuredMember.Expression is InvocationExpressionSyntax inner)
        {
            called = inner;
        }

        if (called is not InvocationExpressionSyntax load
            || load.Expression is not MemberAccessExpressionSyntax member
            || member.Expression is not IdentifierNameSyntax repository
            || !receivers.TryGetValue(repository.Identifier.ValueText, out var face)
            || !face.EndsWith("Repository", StringComparison.Ordinal))
        {
            return null;
        }

        var name = member.Name.Identifier.ValueText;

        if (!name.EndsWith("Async", StringComparison.Ordinal)
            || !(name.StartsWith("Get", StringComparison.Ordinal)
                || name.StartsWith("Find", StringComparison.Ordinal)
                || name.StartsWith("Load", StringComparison.Ordinal)
                || name.StartsWith("Single", StringComparison.Ordinal)
                || name.StartsWith("List", StringComparison.Ordinal)))
        {
            return null;
        }

        var aggregate = face;

        if (aggregate.StartsWith("I", StringComparison.Ordinal) && aggregate.Length > 1 && char.IsUpper(aggregate[1]))
        {
            aggregate = aggregate.Substring(1);
        }

        return aggregate.Length > "Repository".Length
            ? aggregate.Substring(0, aggregate.Length - "Repository".Length)
            : null;
    }

    /// <summary>Reads the simple name of a type as written, without qualification, generic arguments or nullability.</summary>
    /// <param name="type">The type as written.</param>
    /// <returns>The simple name, or null when the type has none.</returns>
    private static string? SimpleTypeName(TypeSyntax type)
    {
        switch (type)
        {
            case IdentifierNameSyntax plain:
                return plain.Identifier.ValueText;
            case GenericNameSyntax generic:
                return generic.Identifier.ValueText;
            case QualifiedNameSyntax qualified:
                return qualified.Right.Identifier.ValueText;
            case AliasQualifiedNameSyntax alias:
                return alias.Name.Identifier.ValueText;
            case NullableTypeSyntax nullable:
                return SimpleTypeName(nullable.ElementType);
            case ArrayTypeSyntax array:
                return SimpleTypeName(array.ElementType);
            case PredefinedTypeSyntax predefined:
                return predefined.Keyword.ValueText;
            default:
                return null;
        }
    }
}
