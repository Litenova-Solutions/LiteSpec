using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>Reads the public methods of every aggregate root a file declares.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class AggregateMethodReader
{
    /// <summary>
    ///     Reads the public methods of every aggregate root the file declares with the
    ///     domain events each one raises and the use-case marks each one carries, the
    ///     raised events read through the shared raised-event rule.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="elements">The marked elements of the file.</param>
    /// <param name="types">The type declarations by identifier span.</param>
    /// <param name="model">The code model an aggregate method adds to.</param>
    public static void ReadAggregateMethods(
        string file,
        SyntaxTree tree,
        IReadOnlyList<MarkableElement> elements,
        IReadOnlyDictionary<TextSpan, TypeDeclarationSyntax> types,
        CodeModel model)
    {
        foreach (var element in elements)
        {
            if (!element.IsType
                || element.Kind != CodeKind.AggregateRoot
                || !types.TryGetValue(element.Span, out var declared))
            {
                continue;
            }

            foreach (var method in declared.Members.OfType<MethodDeclarationSyntax>())
            {
                if (!method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)))
                {
                    continue;
                }

                var uses = MarkReader.Read(method)
                    .Where(mark => mark.RecordId.StartsWith("use-case/", StringComparison.Ordinal))
                    .Select(mark => mark.RecordId)
                    .ToList();

                model.AggregateMethods.Add(new AggregateMethod(
                    declared.Identifier.ValueText,
                    method.Identifier.ValueText,
                    RaisedEvents.Of(method, declared),
                    file,
                    tree.GetLineSpan(method.Span).StartLinePosition.Line + 1)
                {
                    UseCases = uses,
                });
            }
        }
    }
}
