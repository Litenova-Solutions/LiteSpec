using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

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
        Dictionary<string, List<HelperMethod>> helpers,
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
    ///     The injected names a class may call: its constructor parameters plus the fields and
    ///     properties an explicit constructor assigns from them, each with its simple type name.
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

        if (typed.ParameterList is { } primary)
        {
            foreach (var parameter in primary.Parameters)
            {
                AddParameter(parameter);
            }
        }

        foreach (var constructor in typed.Members.OfType<ConstructorDeclarationSyntax>())
        {
            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                AddParameter(parameter);
            }

            if (constructor.Body is null)
            {
                continue;
            }

            foreach (var assignment in constructor.Body.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (assignment.Right is not IdentifierNameSyntax source
                    || !types.TryGetValue(source.Identifier.ValueText, out var type))
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

        return types;

        void AddParameter(ParameterSyntax parameter)
        {
            if (parameter.Type is not null && parameter.Identifier.ValueText.Length > 0)
            {
                types.TryAdd(parameter.Identifier.ValueText, DotnetNames.SimpleTypeName(parameter.Type));
            }
        }
    }
}
