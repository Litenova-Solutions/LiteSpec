using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1008: Domain keeps double and float out of exact quantities.</summary>
/// <follows>litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FloatingPointAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1008);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the floating point analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.VariableDeclaration,
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.IndexerDeclaration,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.LocalFunctionStatement,
            SyntaxKind.DelegateDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.ConversionOperatorDeclaration,
            SyntaxKind.Parameter,
            SyntaxKind.ForEachStatement,
            SyntaxKind.DeclarationExpression,
            SyntaxKind.DeclarationPattern);

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var target = Target(context.Node);
        if (target.Type is null || target.Name is not string name || name.Length == 0)
        {
            return;
        }

        ReportFloatingPoint(target.Type, name, context);
    }

    /// <summary>Returns the declared type syntax and name the node introduces.</summary>
    /// <param name="node">The syntax node under inspection.</param>
    /// <returns>The type syntax and name pair, with both parts null when the node declares neither.</returns>
    private static (TypeSyntax? Type, string? Name) Target(SyntaxNode node)
    {
        switch (node)
        {
            case VariableDeclarationSyntax declaration:
                return declaration.Variables.Count == 0
                    ? (null, null)
                    : (declaration.Type, declaration.Variables[0].Identifier.ValueText);
            case PropertyDeclarationSyntax property:
                return (property.Type, property.Identifier.ValueText);
            case IndexerDeclarationSyntax indexer:
                return (indexer.Type, "this");
            case MethodDeclarationSyntax method:
                return (method.ReturnType, method.Identifier.ValueText);
            case LocalFunctionStatementSyntax local:
                return (local.ReturnType, local.Identifier.ValueText);
            case DelegateDeclarationSyntax delegateDeclaration:
                return (delegateDeclaration.ReturnType, delegateDeclaration.Identifier.ValueText);
            case OperatorDeclarationSyntax @operator:
                return (@operator.ReturnType, @operator.OperatorToken.Text);
            case ConversionOperatorDeclarationSyntax conversion:
                return (conversion.Type, conversion.ImplicitOrExplicitKeyword.Text);
            case ParameterSyntax parameter:
                return (parameter.Type, parameter.Identifier.ValueText);
            case ForEachStatementSyntax forEach:
                return (forEach.Type, forEach.Identifier.ValueText);
            case DeclarationExpressionSyntax expression:
                return DesignationName(expression.Designation) is string expressionName
                    ? (expression.Type, expressionName)
                    : (null, null);
            case DeclarationPatternSyntax pattern:
                return DesignationName(pattern.Designation) is string patternName
                    ? (pattern.Type, patternName)
                    : (null, null);
            default:
                return (null, null);
        }
    }

    /// <summary>Returns the name.</summary>
    /// <param name="designation">The designation whose name the check reads.</param>
    /// <returns>The name as text, or null when none applies.</returns>
    private static string? DesignationName(VariableDesignationSyntax designation) =>
        designation is SingleVariableDesignationSyntax single
            ? single.Identifier.ValueText
            : null;

    /// <summary>Reports the floating point.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="name">The name to convert or match.</param>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void ReportFloatingPoint(TypeSyntax type, string name, SyntaxNodeAnalysisContext context)
    {
        if (IsFloatingPoint(type, context))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1008, type.GetLocation(), name, type.ToString()));
            return;
        }

        foreach (var node in type.DescendantNodes(descendIntoTrivia: false))
        {
            if (node is TypeSyntax nested && node.Parent is not NameSyntax && IsFloatingPoint(nested, context))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1008, nested.GetLocation(), name, nested.ToString()));
            }
        }
    }

    /// <summary>Returns true when the node is floating point; otherwise false.</summary>
    /// <param name="node">The syntax node under inspection.</param>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <returns>True when the node is floating point; otherwise false.</returns>
    private static bool IsFloatingPoint(TypeSyntax node, SyntaxNodeAnalysisContext context)
    {
        if (node is IdentifierNameSyntax identifier && identifier.IsVar)
        {
            return false;
        }

        var type = context.SemanticModel.GetTypeInfo(node).Type;
        return type is not null
            && (type.SpecialType == SpecialType.System_Double || type.SpecialType == SpecialType.System_Single);
    }
}
