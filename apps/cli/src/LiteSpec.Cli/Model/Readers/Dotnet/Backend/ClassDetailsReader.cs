using LiteSpec.Cli.Model.Code;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Backend;

/// <summary>The collected details of one class: what it takes, produces, throws, and handles.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class ClassDetailsReader
{
    /// <summary>The collected details of a handler class.</summary>
    /// <param name="declared">The handler declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <param name="helpers">The helper index of this read.</param>
    /// <param name="aggregateRoots">The simple names of known aggregate roots.</param>
    /// <param name="holders">The classes whose constructor takes a repository.</param>
    /// <param name="isQuery">Whether the handler handles a query.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    /// <returns>The collected details of the handler class.</returns>
    public static ClassDetails HandlerDetailsOf(
        BaseTypeDeclarationSyntax declared,
        SyntaxTree tree,
        string file,
        Dictionary<string, List<CalledMethod>> helpers,
        HashSet<string> aggregateRoots,
        HashSet<string> holders,
        bool isQuery,
        QualifiedKinds qualified)
    {
        return new ClassDetails(
            declared.Identifier.ValueText,
            tree.GetLineSpan(declared.Span).StartLinePosition.Line + 1,
            RepositoriesOf(declared, tree, file),
            RefusalsOf(declared, tree, file),
            ThrownOf(declared, tree, file),
            DispatchReader.DispatchesOf(declared, tree, file, helpers, qualified),
            MessageTypeOf(declared),
            DependencyCountOf(declared),
            AggregateCallReader.AggregateCallsOf(declared, tree, file, aggregateRoots),
            AggregateCallReader.FactoryCallsOf(declared, tree, file, aggregateRoots),
            ClassActivityReader.CollaboratorsOf(declared, tree, file, holders),
            ClassActivityReader.LoopReadsOf(declared, tree, file),
            isQuery ? ClassActivityReader.MemoryFiltersOf(declared, tree, file) : []);
    }

    /// <summary>The repository interfaces a class takes in its primary or explicit constructors.</summary>
    /// <param name="declared">The class declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <returns>The repository interfaces the class takes.</returns>
    public static List<CodeRepository> RepositoriesOf(BaseTypeDeclarationSyntax declared, SyntaxTree tree, string file)
    {
        var found = new List<CodeRepository>();

        foreach (var (type, line) in ConstructorTypes(declared, tree))
        {
            var simple = type.Contains('<') ? type[..type.IndexOf('<')] : type;

            if (simple.EndsWith("Repository", StringComparison.Ordinal))
            {
                found.Add(new CodeRepository(type, file, line));
            }
        }

        return found;
    }

    /// <summary>Every constructor parameter type with its line: the primary constructor first, then each explicit one.</summary>
    /// <param name="declared">The class declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <returns>Every constructor parameter type with its line.</returns>
    public static List<(string Type, int Line)> ConstructorTypes(BaseTypeDeclarationSyntax declared, SyntaxTree tree)
    {
        var found = new List<(string Type, int Line)>();

        if (declared is not TypeDeclarationSyntax typed)
        {
            return found;
        }

        if (typed.ParameterList is { } primary)
        {
            foreach (var parameter in primary.Parameters)
            {
                if (parameter.Type is not null)
                {
                    found.Add((DotnetNames.SimpleTypeName(parameter.Type), tree.GetLineSpan(parameter.Span).StartLinePosition.Line + 1));
                }
            }
        }

        foreach (var constructor in typed.Members.OfType<ConstructorDeclarationSyntax>())
        {
            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                if (parameter.Type is not null)
                {
                    found.Add((DotnetNames.SimpleTypeName(parameter.Type), tree.GetLineSpan(parameter.Span).StartLinePosition.Line + 1));
                }
            }
        }

        return found;
    }

    /// <summary>The refusal codes a class produces in declaring positions, earliest line first.</summary>
    /// <param name="declared">The class declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <returns>The refusal codes the class produces, earliest line first.</returns>
    public static List<CodeRefusal> RefusalsOf(BaseTypeDeclarationSyntax declared, SyntaxTree tree, string file)
    {
        var found = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var token in declared.DescendantTokens())
        {
            if (RefusalLiterals.LiteralText(token) is not { } literal || !RefusalLiterals.Declares(token))
            {
                continue;
            }

            foreach (System.Text.RegularExpressions.Match match in Identifiers.RefusalCode().Matches(literal))
            {
                if (match.Value == literal)
                {
                    found.TryAdd(match.Value, tree.GetLineSpan(token.Span).StartLinePosition.Line + 1);
                }
            }
        }

        return [.. found
            .OrderBy(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new CodeRefusal(entry.Key, file, entry.Value))];
    }

    /// <summary>The exception and failure types a class constructs, earliest line first.</summary>
    /// <param name="declared">The class declaration.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="file">The file.</param>
    /// <returns>The exception and failure types the class constructs, earliest line first.</returns>
    public static List<CodeThrown> ThrownOf(BaseTypeDeclarationSyntax declared, SyntaxTree tree, string file)
    {
        var found = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var creation in declared.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var name = DotnetNames.SimpleTypeName(creation.Type);
            var simple = name.Contains('<') ? name[..name.IndexOf('<')] : name;

            if (simple.EndsWith("Exception", StringComparison.Ordinal)
                || simple.EndsWith("Failure", StringComparison.Ordinal)
                || simple.EndsWith("Refusal", StringComparison.Ordinal))
            {
                found.TryAdd(simple, tree.GetLineSpan(creation.Span).StartLinePosition.Line + 1);
            }
        }

        return [.. found
            .OrderBy(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new CodeThrown(entry.Key, file, entry.Value))];
    }

    /// <summary>The message a handler handles: the first parameter type of its Handle method, or empty.</summary>
    /// <param name="declared">The handler declaration.</param>
    /// <returns>The message a handler handles, or empty when it handles none.</returns>
    public static string MessageTypeOf(BaseTypeDeclarationSyntax declared)
    {
        if (declared is not TypeDeclarationSyntax typed)
        {
            return "";
        }

        foreach (var method in typed.Members.OfType<MethodDeclarationSyntax>())
        {
            if (method.Identifier.ValueText is "Handle" or "HandleAsync"
                && method.ParameterList.Parameters.FirstOrDefault()?.Type is { } message)
            {
                return DotnetNames.SimpleTypeName(message);
            }
        }

        return "";
    }

    /// <summary>The constructor dependencies a class declares: the widest constructor's parameter count.</summary>
    /// <param name="declared">The class declaration.</param>
    /// <returns>The constructor dependencies the class declares.</returns>
    public static int DependencyCountOf(BaseTypeDeclarationSyntax declared)
    {
        if (declared is not TypeDeclarationSyntax typed)
        {
            return 0;
        }

        var counts = new List<int>();

        if (typed.ParameterList is { } primary)
        {
            counts.Add(primary.Parameters.Count);
        }

        foreach (var constructor in typed.Members.OfType<ConstructorDeclarationSyntax>())
        {
            counts.Add(constructor.ParameterList.Parameters.Count);
        }

        return counts.Count == 0 ? 0 : counts.Max();
    }

    /// <summary>
    ///     The injected names a class may call: its constructor parameters, the fields and
    ///     properties an explicit constructor assigns from them, and the fields and properties
    ///     a primary-constructor parameter initializes, each with its simple type name.
    /// </summary>
    /// <param name="declared">The class declaration.</param>
    /// <returns>The injected names the class may call, each with its simple type name.</returns>
    public static Dictionary<string, string> ReceiverTypes(BaseTypeDeclarationSyntax declared)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);

        if (declared is not TypeDeclarationSyntax typed)
        {
            return types;
        }

        var primary = new Dictionary<string, string>(StringComparer.Ordinal);

        if (typed.ParameterList is { } primaryList)
        {
            foreach (var parameter in primaryList.Parameters)
            {
                AddParameter(parameter);

                if (parameter.Type is not null && parameter.Identifier.ValueText.Length > 0)
                {
                    primary.TryAdd(parameter.Identifier.ValueText, DotnetNames.SimpleTypeName(parameter.Type));
                }
            }
        }

        foreach (var constructor in typed.Members.OfType<ConstructorDeclarationSyntax>())
        {
            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                AddParameter(parameter);
            }

            IEnumerable<AssignmentExpressionSyntax> assignments = constructor.Body is not null
                ? constructor.Body.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                : constructor.ExpressionBody is not null
                    ? constructor.ExpressionBody.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>()
                    : [];

            foreach (var assignment in assignments)
            {
                if (SourceOf(assignment.Right) is not { } source
                    || !types.TryGetValue(source, out var type))
                {
                    continue;
                }

                var target = assignment.Left switch
                {
                    IdentifierNameSyntax plain => plain.Identifier.ValueText,
                    MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } member => member.Name.Identifier.ValueText,
                    _ => null,
                };

                if (target is not null)
                {
                    types.TryAdd(target, type);
                }
            }
        }

        // A primary-constructor parameter captured in a field or property initializer keeps
        // its type under the member's name. An explicit constructor's parameters are out of
        // scope in an initializer, so only primary parameters match there.
        foreach (var field in typed.Members.OfType<FieldDeclarationSyntax>())
        {
            foreach (var variable in field.Declaration.Variables)
            {
                if (variable.Initializer?.Value is { } value
                    && SourceOf(value) is { } source
                    && primary.TryGetValue(source, out var type))
                {
                    types.TryAdd(variable.Identifier.ValueText, type);
                }
            }
        }

        foreach (var property in typed.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (property.Initializer?.Value is { } value
                && SourceOf(value) is { } source
                && primary.TryGetValue(source, out var type))
            {
                types.TryAdd(property.Identifier.ValueText, type);
            }
        }

        return types;

        void AddParameter(ParameterSyntax parameter)
        {
            if (parameter.Type is not null && parameter.Identifier.ValueText.Length > 0)
            {
                types.TryAdd(parameter.Identifier.ValueText, DotnetNames.SimpleTypeName(parameter.Type));
            }
        }
    }

    /// <summary>The constructor parameter an assigned value comes from: the name itself, through a null guard, a null-forgiving operator, or parentheses.</summary>
    /// <param name="value">The assigned value.</param>
    /// <returns>The source parameter name, or null when the value names none.</returns>
    private static string? SourceOf(ExpressionSyntax value) => value switch
    {
        IdentifierNameSyntax plain => plain.Identifier.ValueText,
        BinaryExpressionSyntax coalesce when coalesce.OperatorToken.ValueText == "??" => SourceOf(coalesce.Left),
        PostfixUnaryExpressionSyntax postfix => SourceOf(postfix.Operand),
        ParenthesizedExpressionSyntax parenthesized => SourceOf(parenthesized.Expression),
        _ => null,
    };
}
