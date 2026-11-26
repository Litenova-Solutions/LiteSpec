using LiteSpec.Cli.Model.Checks.Implementation.Handlers;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Backend;

/// <summary>The nested cases a closed-set type declares: each nested case name with the code its Code carries, so a knowledge column rendering a case matches the row value against the declared names and codes.</summary>
/// <follows>litespec/core/model#generate-record-determined-code-from-the-records</follows>
static class ClosedCaseReader
{
    /// <summary>The declared case name by code of a closed-set type: each nested case with the string literal its Code answers.</summary>
    /// <param name="source">The backend source the declaring file is read through.</param>
    /// <param name="knowledgeRoot">The Knowledge folder holding the knowledge models, or empty when the consumer has none.</param>
    /// <param name="qualifiedType">The qualified closed-set type name as the column entry states it.</param>
    /// <returns>The declared case name by code, or an empty map when the backend declares none the reader matches.</returns>
    public static IReadOnlyDictionary<string, string> CasesOf(MessageFieldChecks.MessageSource source, string knowledgeRoot, string qualifiedType)
    {
        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        var simple = qualifiedType.Contains('.', StringComparison.Ordinal)
            ? qualifiedType[(qualifiedType.LastIndexOf('.') + 1)..]
            : qualifiedType;

        if (simple.Length == 0)
        {
            return found;
        }

        var file = source.FileOf(simple, "") ?? KnowledgeFileOf(knowledgeRoot, simple);

        if (file is null)
        {
            return found;
        }

        foreach (var (_, declared) in source.TypesIn(file, simple))
        {
            foreach (var nested in declared.Members.OfType<TypeDeclarationSyntax>())
            {
                if (CodeOf(nested) is { } code)
                {
                    found.TryAdd(code, nested.Identifier.ValueText);
                }
            }
        }

        return found;
    }

    /// <summary>The Knowledge file declaring the type: the one file named for it, or none when none or several hold the name.</summary>
    /// <param name="knowledgeRoot">The Knowledge folder holding the knowledge models, or empty when the consumer has none.</param>
    /// <param name="simple">The simple type name.</param>
    /// <returns>The declaring file, or null when it is missing or ambiguous.</returns>
    static string? KnowledgeFileOf(string knowledgeRoot, string simple)
    {
        if (knowledgeRoot.Length == 0)
        {
            return null;
        }

        List<string> files;

        try
        {
            files = Directory.EnumerateFiles(knowledgeRoot, simple + ".cs", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        return files.Count == 1 ? files[0] : null;
    }

    /// <summary>The code a nested case declares: the string literal its Code answers through an arrow clause, a returning getter, or an initializer.</summary>
    /// <param name="nested">The nested case declaration.</param>
    /// <returns>The declared code, or null when the case states none as a string literal.</returns>
    static string? CodeOf(TypeDeclarationSyntax nested)
    {
        foreach (var property in nested.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (property.Identifier.ValueText != "Code")
            {
                continue;
            }

            if (property.ExpressionBody?.Expression is { } arrow && LiteralOf(arrow) is { } arrowed)
            {
                return arrowed;
            }

            if (property.Initializer?.Value is { } initial && LiteralOf(initial) is { } started)
            {
                return started;
            }

            if (property.AccessorList is { } accessors)
            {
                foreach (var accessor in accessors.Accessors)
                {
                    if (accessor.Body?.Statements is { Count: 1 } statements
                        && statements[0] is ReturnStatementSyntax returned
                        && returned.Expression is { } body
                        && LiteralOf(body) is { } literal)
                    {
                        return literal;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>The string literal an expression holds: its unescaped text, or null when the expression is no string literal.</summary>
    /// <param name="expression">The expression as written.</param>
    /// <returns>The literal text, or null.</returns>
    static string? LiteralOf(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax literal && literal.Token.Value is string ? literal.Token.ValueText : null;
}
