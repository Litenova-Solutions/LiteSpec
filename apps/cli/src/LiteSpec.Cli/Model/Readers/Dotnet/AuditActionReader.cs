using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>Reads the audit action literal each message class declares.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class AuditActionReader
{
    /// <summary>
    ///     Reads the audit action literal each message class declares in its
    ///     <c>[Audited("...")]</c> attribute, keyed by the message stem.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="classes">The type declarations of the file.</param>
    /// <param name="model">The code model an audit action adds to.</param>
    public static void ReadAuditActions(
        string file,
        SyntaxTree tree,
        IReadOnlyList<BaseTypeDeclarationSyntax> classes,
        CodeModel model)
    {
        foreach (var declared in classes)
        {
            foreach (var attribute in declared.AttributeLists.SelectMany(list => list.Attributes))
            {
                var name = attribute.Name.ToString();
                var dot = name.LastIndexOf('.');
                var simple = dot >= 0 ? name[(dot + 1)..] : name;

                if (simple is not ("Audited" or "AuditedAttribute"))
                {
                    continue;
                }

                if (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression
                        is LiteralExpressionSyntax literal
                    && literal.Token.Value is string action
                    && action.Length > 0)
                {
                    var operation = declared.Identifier.ValueText;

                    foreach (var suffix in new[] { "Command", "Query" })
                    {
                        if (operation.EndsWith(suffix, StringComparison.Ordinal) && operation.Length > suffix.Length)
                        {
                            operation = operation[..^suffix.Length];
                            break;
                        }
                    }

                    model.AuditActions.Add(new CodeAuditAction(
                        operation,
                        action,
                        file,
                        tree.GetLineSpan(attribute.Span).StartLinePosition.Line + 1));
                }
            }
        }
    }
}
