using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>Reads one endpoint's operations: the route and operation literals its class maps.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class EndpointReader
{
    /// <summary>Reads one endpoint's operations: the route and operation literals its class maps.</summary>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="declared">The endpoint declaration.</param>
    /// <param name="marks">The marks the endpoint carries.</param>
    /// <param name="model">The code model an operation adds to.</param>
    public static void ReadEndpoint(
        string file,
        SyntaxTree tree,
        TypeDeclarationSyntax declared,
        IReadOnlyList<CodeMark> marks,
        CodeModel model)
    {
        var useCase = ElementReader.SingleMark(marks, "use-case/") ?? "";

        foreach (var invocation in declared.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var called = CalledName(invocation);

            if (called == "WithName" && FirstLiteral(invocation) is { } operation)
            {
                model.Endpoints.Add(new EndpointOperation(
                    operation, useCase, "", "", file,
                    tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1));
            }
            else if (called is "MapGet" or "MapPost" or "MapPut" or "MapDelete" or "MapPatch"
                && FirstLiteral(invocation) is { } route)
            {
                model.Endpoints.Add(new EndpointOperation(
                    "", useCase, called["Map".Length..].ToUpperInvariant(), route, file,
                    tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1));
            }
        }
    }

    /// <summary>The name an invocation calls, or null when it calls no name.</summary>
    /// <param name="invocation">The invocation.</param>
    /// <returns>The name the invocation calls, or null when it calls no name.</returns>
    public static string? CalledName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax plain => plain.Identifier.ValueText,
            _ => null,
        };

    /// <summary>The first string literal argument of an invocation, or null when it carries none.</summary>
    /// <param name="invocation">The invocation.</param>
    /// <returns>The first string literal argument of the invocation, or null when it carries none.</returns>
    public static string? FirstLiteral(InvocationExpressionSyntax invocation)
    {
        var first = invocation.ArgumentList.Arguments.FirstOrDefault();

        return first?.Expression is LiteralExpressionSyntax literal
            && literal.Token.Value is string text
                ? text
                : null;
    }
}
