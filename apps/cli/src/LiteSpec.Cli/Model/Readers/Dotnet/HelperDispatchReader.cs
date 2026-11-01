using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>The dispatches a held receiver carries through a helper, one hop deep.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class HelperDispatchReader
{
    /// <summary>
    ///     The dispatches a held receiver carries through a helper: when the class passes a
    ///     mediator, publisher, messenger or external port it holds to a helper method in the
    ///     same source tree, the helper's dispatches through that receiver count for the class,
    ///     one hop deep and named with the helper. The helper's own helper calls are never
    ///     followed, so an attribution chain can neither recurse nor cycle.
    /// </summary>
    /// <param name="declared">The class declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <param name="receivers">The injected names the class may call, each with its type.</param>
    /// <param name="helpers">The helper index of this read.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    /// <returns>The dispatches a held receiver carries through a helper.</returns>
    public static List<CodeDispatch> HelperDispatches(
        BaseTypeDeclarationSyntax declared,
        SyntaxTree tree,
        string file,
        Dictionary<string, string> receivers,
        Dictionary<string, List<HelperMethod>> helpers,
        QualifiedKinds qualified)
    {
        if (helpers.Count == 0)
        {
            return [];
        }

        var held = receivers
            .Where(entry => DispatchReader.DispatchCapable(entry.Value, declared, qualified))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

        if (held.Count == 0)
        {
            return [];
        }

        var found = new List<CodeDispatch>();
        var seen = new HashSet<(string Via, string Verb, string Target, string Detail)>();

        foreach (var invocation in declared.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var passed = invocation.ArgumentList.Arguments
                .Select(argument => argument.Expression as IdentifierNameSyntax)
                .Where(argument => argument is not null && held.ContainsKey(argument.Identifier.ValueText))
                .Select(argument => held[argument!.Identifier.ValueText])
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (passed.Count == 0)
            {
                continue;
            }

            if (CalleeOf(invocation, declared, receivers) is not { } callee
                || !(helpers.TryGetValue($"{qualified.ResolveName(declared, callee.Type)}.{callee.Method}", out var overloads)
                    || helpers.TryGetValue($"{callee.Type}.{callee.Method}", out overloads)))
            {
                continue;
            }

            var line = tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;

            foreach (var helper in overloads)
            {
                var bound = helper.Parameters
                    .Where(parameter => passed.Contains(parameter.Value, StringComparer.Ordinal))
                    .Select(parameter => parameter.Key)
                    .ToHashSet(StringComparer.Ordinal);

                if (bound.Count == 0)
                {
                    continue;
                }

                var via = $"{helper.Type}.{helper.Method}";

                foreach (var (receiver, dispatch) in helper.Dispatches)
                {
                    if (!bound.Contains(receiver)
                        || !seen.Add((via, dispatch.Verb, dispatch.Target, dispatch.Detail)))
                    {
                        continue;
                    }

                    found.Add(dispatch with { File = file, Line = line, Via = via });
                }
            }
        }

        return found;
    }

    /// <summary>
    ///     The helper a call names: a static <c>Type.Method</c> call, or an instance call on a
    ///     held or locally built value whose type names it. Null for a call onto the class
    ///     itself, whose body the direct scan already reads, and for anything else.
    /// </summary>
    /// <param name="invocation">The invocation.</param>
    /// <param name="declared">The class declaration.</param>
    /// <param name="receivers">The injected names the class may call, each with its type.</param>
    /// <returns>The helper a call names, or null when it names none.</returns>
    public static (string Type, string Method)? CalleeOf(
        InvocationExpressionSyntax invocation,
        BaseTypeDeclarationSyntax declared,
        Dictionary<string, string> receivers)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member)
        {
            return null;
        }

        var method = member.Name.Identifier.ValueText;

        switch (member.Expression)
        {
            case IdentifierNameSyntax plain:
                var name = plain.Identifier.ValueText;

                if (name == declared.Identifier.ValueText)
                {
                    return null;
                }

                if (receivers.TryGetValue(name, out var instance))
                {
                    return InstanceCallee(instance, method, declared);
                }

                if (char.IsUpper(name[0]))
                {
                    return (name, method);
                }

                return LocalType(invocation, name) is { } built
                    ? InstanceCallee(built, method, declared)
                    : null;

            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax }:
                return null;

            case MemberAccessExpressionSyntax chain:
                var type = chain.Name.Identifier.ValueText;

                return type == declared.Identifier.ValueText ? null : (type, method);

            default:
                return null;
        }
    }

    /// <summary>The helper an instance call names, or null when the instance is the class itself.</summary>
    /// <param name="instance">The instance type as written.</param>
    /// <param name="method">The method name.</param>
    /// <param name="declared">The class declaration.</param>
    /// <returns>The instance type and method as a pair, or null when the instance is the declaring class itself.</returns>
    public static (string Type, string Method)? InstanceCallee(string instance, string method, BaseTypeDeclarationSyntax declared)
    {
        var type = DotnetNames.KindOf(instance);
        var dot = type.LastIndexOf('.');

        if (dot >= 0)
        {
            type = type[(dot + 1)..];
        }

        return type.Length == 0 || type == declared.Identifier.ValueText ? null : (type, method);
    }

    /// <summary>The type a local holds: what it builds, or its declared type when it names one.</summary>
    /// <param name="invocation">The invocation on the local.</param>
    /// <param name="name">The local's name.</param>
    /// <returns>The type a local holds, or null when it holds none the reader names.</returns>
    public static string? LocalType(InvocationExpressionSyntax invocation, string name)
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

            if (declarator.Parent is VariableDeclarationSyntax declaration
                && declaration.Type is not IdentifierNameSyntax { Identifier.ValueText: "var" })
            {
                return DotnetNames.SimpleTypeName(declaration.Type);
            }
        }

        return null;
    }
}
