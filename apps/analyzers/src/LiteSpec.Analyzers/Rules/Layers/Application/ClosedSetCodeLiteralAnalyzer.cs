using System;
using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>LS2041: code outside a closed set compares the typed case or names a synced constant, never a string literal.</summary>
/// <follows>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ClosedSetCodeLiteralAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2041);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the closed set code literal analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterOperationAction(c => AnalyzeBinary(c, scope), OperationKind.Binary);
        context.RegisterOperationAction(c => AnalyzeInvocation(c, scope), OperationKind.Invocation);
        context.RegisterSymbolAction(c => AnalyzeType(c, scope), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the binary.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeBinary(OperationAnalysisContext context, FileScope scope)
    {
        if (context.Operation.Syntax is null || !scope.AppliesTo(context.Operation.Syntax.SyntaxTree))
        {
            return;
        }

        var binary = (IBinaryOperation)context.Operation;
        if (binary.OperatorKind != BinaryOperatorKind.Equals && binary.OperatorKind != BinaryOperatorKind.NotEquals)
        {
            return;
        }

        ReportWhenCodeMeetsLiteral(context, [binary.LeftOperand, binary.RightOperand]);
    }

    /// <summary>Analyzes the invocation.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeInvocation(OperationAnalysisContext context, FileScope scope)
    {
        if (context.Operation.Syntax is null || !scope.AppliesTo(context.Operation.Syntax.SyntaxTree))
        {
            return;
        }

        var invocation = (IInvocationOperation)context.Operation;
        if (!invocation.TargetMethod.Name.Equals("Equals", StringComparison.Ordinal))
        {
            return;
        }

        var operands = new System.Collections.Generic.List<IOperation>(invocation.Arguments.Length + 1);
        if (invocation.Instance is not null)
        {
            operands.Add(invocation.Instance);
        }

        foreach (var argument in invocation.Arguments)
        {
            operands.Add(argument.Value);
        }

        ReportWhenCodeMeetsLiteral(context, operands);
    }

    /// <summary>Reports the literal when a closed-set Code meets a string literal.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="operands">The compared values, such as the two sides of == or the receiver and arguments of Equals.</param>
    private static void ReportWhenCodeMeetsLiteral(
        OperationAnalysisContext context, System.Collections.Generic.IReadOnlyList<IOperation> operands)
    {
        IPropertySymbol? code = null;
        LiteralExpressionSyntax? literal = null;
        foreach (var operand in operands)
        {
            if (IsClosedSetCode(operand, out var property))
            {
                code = property;
            }
            else if (StringLiteral(operand) is { } syntax)
            {
                literal = syntax;
            }
        }

        if (code is null || literal is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2041,
            literal.GetLocation(),
            literal.Token.ValueText,
            code.ContainingType.Name,
            "compare the typed case or a CodeValue constant instead"));
    }

    /// <summary>Analyzes the type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeType(SymbolAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        if (context.Symbol is not INamedTypeSymbol type || !DerivesFromDomainException(type))
        {
            return;
        }

        foreach (var member in type.GetMembers("Code"))
        {
            if (member is not IPropertySymbol property
                || property.Type.SpecialType != SpecialType.System_String
                || !SymbolEqualityComparer.Default.Equals(property.ContainingType, type))
            {
                continue;
            }

            foreach (var declaration in property.DeclaringSyntaxReferences)
            {
                ReportPropertyLiterals(context, type, declaration.GetSyntax());
            }
        }
    }

    /// <summary>Reports every string literal the Code property returns.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="type">The refusal type that declares the Code property.</param>
    /// <param name="node">The property declaration under inspection.</param>
    private static void ReportPropertyLiterals(SymbolAnalysisContext context, INamedTypeSymbol type, SyntaxNode node)
    {
        if (node is not PropertyDeclarationSyntax property)
        {
            return;
        }

        if (property.ExpressionBody?.Expression is LiteralExpressionSyntax arrow && IsStringLiteral(arrow))
        {
            ReportRefusalLiteral(context, type, arrow);
        }

        if (property.Initializer?.Value is LiteralExpressionSyntax initializer && IsStringLiteral(initializer))
        {
            ReportRefusalLiteral(context, type, initializer);
        }

        if (property.AccessorList is null)
        {
            return;
        }

        foreach (var accessor in property.AccessorList.Accessors)
        {
            if (!accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
            {
                continue;
            }

            foreach (var literal in accessor.DescendantNodes().OfType<LiteralExpressionSyntax>())
            {
                if (IsStringLiteral(literal))
                {
                    ReportRefusalLiteral(context, type, literal);
                }
            }
        }
    }

    /// <summary>Reports the refusal literal.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="type">The refusal type that declares the Code property.</param>
    /// <param name="literal">The string literal that carries the refusal code.</param>
    private static void ReportRefusalLiteral(
        SymbolAnalysisContext context, INamedTypeSymbol type, LiteralExpressionSyntax literal) =>
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2041,
            literal.GetLocation(),
            literal.Token.ValueText,
            type.Name,
            "name the synced refusal constant instead"));

    /// <summary>Returns true when the operation reads the Code of a closed set; otherwise false.</summary>
    /// <param name="operation">The operation under inspection.</param>
    /// <param name="property">The Code property, or null when the operation reads none.</param>
    /// <returns>True when the operation reads a string Code declared on an abstract type.</returns>
    private static bool IsClosedSetCode(IOperation operation, out IPropertySymbol? property)
    {
        property = null;
        if (Unwrap(operation) is not IPropertyReferenceOperation reference
            || reference.Property is not IPropertySymbol candidate
            || !candidate.Name.Equals("Code", StringComparison.Ordinal)
            || candidate.Type.SpecialType != SpecialType.System_String
            || candidate.ContainingType is not { IsAbstract: true })
        {
            return false;
        }

        property = candidate;
        return true;
    }

    /// <summary>The string literal syntax the operation carries, or null.</summary>
    /// <param name="operation">The operation under inspection.</param>
    /// <returns>The literal syntax, or null when the operation carries no string literal.</returns>
    private static LiteralExpressionSyntax? StringLiteral(IOperation operation)
    {
        var unwrapped = Unwrap(operation);
        if (unwrapped is not ILiteralOperation { ConstantValue: { HasValue: true, Value: string } }
            || unwrapped.Syntax is not LiteralExpressionSyntax literal
            || !IsStringLiteral(literal))
        {
            return null;
        }

        return literal;
    }

    /// <summary>Returns true when the syntax is a string literal; otherwise false.</summary>
    /// <param name="literal">The literal expression under inspection.</param>
    /// <returns>True when the literal is a string literal expression.</returns>
    private static bool IsStringLiteral(LiteralExpressionSyntax literal) =>
        literal.IsKind(SyntaxKind.StringLiteralExpression);

    /// <summary>Returns true when the type derives from DomainException; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when a base type is named DomainException.</returns>
    private static bool DerivesFromDomainException(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name.Equals("DomainException", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the operation with its conversions removed.</summary>
    /// <param name="operation">The operation under inspection.</param>
    /// <returns>The innermost operand.</returns>
    private static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }
}
