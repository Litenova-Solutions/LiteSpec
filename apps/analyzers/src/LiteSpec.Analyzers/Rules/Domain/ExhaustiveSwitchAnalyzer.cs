using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1013: a switch over a state or closed-set record hierarchy gives every sealed case its own arm.</summary>
/// <follows>litespec/backend/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</follows>
/// <follows>litespec/backend/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExhaustiveSwitchAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1013);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the exhaustive switch analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var openBases = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var hierarchies = BuildHierarchies(context.Compilation, openBases);
        context.RegisterSyntaxNodeAction(
            nodeContext => Analyze(nodeContext, scope, hierarchies, openBases),
            SyntaxKind.SwitchExpression,
            SyntaxKind.SwitchStatement);
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="hierarchies">The sealed cases of each abstract record base, by base.</param>
    /// <param name="openBases">The abstract record bases that an unsealed case leaves open.</param>
    private static void Analyze(
        SyntaxNodeAnalysisContext context,
        FileScope scope,
        Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> hierarchies,
        HashSet<INamedTypeSymbol> openBases)
    {
        if (!scope.AppliesTo(context.Node.SyntaxTree))
        {
            return;
        }

        switch (context.Node)
        {
            case SwitchExpressionSyntax expression:
                AnalyzeSwitchExpression(context, expression, hierarchies, openBases);
                break;
            case SwitchStatementSyntax statement:
                AnalyzeSwitchStatement(context, statement, hierarchies, openBases);
                break;
        }
    }

    /// <summary>Analyzes the switch expression.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="expression">The switch expression under inspection.</param>
    /// <param name="hierarchies">The sealed cases of each abstract record base, by base.</param>
    /// <param name="openBases">The abstract record bases that an unsealed case leaves open.</param>
    private static void AnalyzeSwitchExpression(
        SyntaxNodeAnalysisContext context,
        SwitchExpressionSyntax expression,
        Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> hierarchies,
        HashSet<INamedTypeSymbol> openBases)
    {
        SyntaxNode? discard = null;
        var coveredTypes = new List<ITypeSymbol>();
        foreach (var arm in expression.Arms)
        {
            if (IsDiscardPattern(arm.Pattern))
            {
                discard ??= arm.Pattern;
                continue;
            }

            if (arm.WhenClause is not null)
            {
                continue;
            }

            CollectPatternTypes(arm.Pattern, context.SemanticModel, coveredTypes);
        }

        if (discard is null)
        {
            return;
        }

        var inputType = context.SemanticModel
            .GetTypeInfo(expression.GoverningExpression, context.CancellationToken).Type;
        Report(context, inputType, coveredTypes, discard, hierarchies, openBases);
    }

    /// <summary>Analyzes the switch statement.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="statement">The switch statement under inspection.</param>
    /// <param name="hierarchies">The sealed cases of each abstract record base, by base.</param>
    /// <param name="openBases">The abstract record bases that an unsealed case leaves open.</param>
    private static void AnalyzeSwitchStatement(
        SyntaxNodeAnalysisContext context,
        SwitchStatementSyntax statement,
        Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> hierarchies,
        HashSet<INamedTypeSymbol> openBases)
    {
        SyntaxNode? discard = null;
        var coveredTypes = new List<ITypeSymbol>();
        foreach (var section in statement.Sections)
        {
            var guarded = false;
            foreach (var label in section.Labels)
            {
                if (label is CasePatternSwitchLabelSyntax patternLabel && patternLabel.WhenClause is not null)
                {
                    guarded = true;
                }
            }

            foreach (var label in section.Labels)
            {
                switch (label)
                {
                    case DefaultSwitchLabelSyntax:
                        discard ??= label;
                        break;
                    case CasePatternSwitchLabelSyntax patternLabel:
                        if (IsDiscardPattern(patternLabel.Pattern))
                        {
                            discard ??= patternLabel.Pattern;
                        }
                        else if (!guarded)
                        {
                            CollectPatternTypes(patternLabel.Pattern, context.SemanticModel, coveredTypes);
                        }

                        break;
                }
            }
        }

        if (discard is null)
        {
            return;
        }

        var inputType = context.SemanticModel
            .GetTypeInfo(statement.Expression, context.CancellationToken).Type;
        Report(context, inputType, coveredTypes, discard, hierarchies, openBases);
    }

    /// <summary>Reports the switch.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="inputType">The type the switch branches on, or null when it does not resolve.</param>
    /// <param name="coveredTypes">The pattern types the switch names without a guard.</param>
    /// <param name="discard">The discard arm or default label the missing cases reach.</param>
    /// <param name="hierarchies">The sealed cases of each abstract record base, by base.</param>
    /// <param name="openBases">The abstract record bases that an unsealed case leaves open.</param>
    private static void Report(
        SyntaxNodeAnalysisContext context,
        ITypeSymbol? inputType,
        List<ITypeSymbol> coveredTypes,
        SyntaxNode discard,
        Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> hierarchies,
        HashSet<INamedTypeSymbol> openBases)
    {
        if (inputType is not INamedTypeSymbol baseType
            || !hierarchies.TryGetValue(baseType, out var sealedCases)
            || sealedCases.Count == 0
            || openBases.Contains(baseType))
        {
            return;
        }

        var missing = new List<string>();
        foreach (var sealedCase in sealedCases)
        {
            if (!IsCovered(sealedCase, coveredTypes))
            {
                missing.Add(sealedCase.Name);
            }
        }

        if (missing.Count == 0)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS1013,
            discard.GetLocation(),
            baseType.Name,
            string.Join(", ", missing)));
    }

    /// <summary>Returns the hierarchies.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <param name="openBases">The abstract record bases that an unsealed case leaves open.</param>
    /// <returns>The hierarchies, in the order this method builds them, or an empty collection when none apply.</returns>
    private static Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> BuildHierarchies(
        Compilation compilation,
        HashSet<INamedTypeSymbol> openBases)
    {
        var types = new List<INamedTypeSymbol>();
        CollectTypes(compilation.Assembly.GlobalNamespace, types);

        var hierarchies = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
        foreach (var type in types)
        {
            if (type.TypeKind != TypeKind.Class || !type.IsRecord || type.IsAbstract)
            {
                continue;
            }

            for (var ancestor = type.BaseType; ancestor is not null; ancestor = ancestor.BaseType)
            {
                if (ancestor is not INamedTypeSymbol baseType
                    || baseType.TypeKind != TypeKind.Class
                    || !baseType.IsRecord
                    || !baseType.IsAbstract)
                {
                    continue;
                }

                if (!type.IsSealed)
                {
                    openBases.Add(baseType);
                    continue;
                }

                if (!hierarchies.TryGetValue(baseType, out var cases))
                {
                    cases = new List<INamedTypeSymbol>();
                    hierarchies.Add(baseType, cases);
                }

                if (!cases.Contains(type))
                {
                    cases.Add(type);
                }
            }
        }

        foreach (var cases in hierarchies.Values)
        {
            cases.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        }

        return hierarchies;
    }

    /// <summary>Collects the types.</summary>
    /// <param name="space">The namespace that contains the types under inspection.</param>
    /// <param name="types">The types collected so far, in discovery order.</param>
    private static void CollectTypes(INamespaceSymbol space, List<INamedTypeSymbol> types)
    {
        foreach (var member in space.GetTypeMembers())
        {
            CollectType(member, types);
        }

        foreach (var child in space.GetNamespaceMembers())
        {
            CollectTypes(child, types);
        }
    }

    /// <summary>Collects the type.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="types">The types collected so far, in discovery order.</param>
    private static void CollectType(INamedTypeSymbol type, List<INamedTypeSymbol> types)
    {
        types.Add(type);
        foreach (var nested in type.GetTypeMembers())
        {
            CollectType(nested, types);
        }
    }

    /// <summary>Returns true when the pattern is discard pattern; otherwise false.</summary>
    /// <param name="pattern">The pattern under inspection.</param>
    /// <returns>True when the pattern is discard pattern; otherwise false.</returns>
    private static bool IsDiscardPattern(PatternSyntax pattern) =>
        pattern is DiscardPatternSyntax or VarPatternSyntax;

    /// <summary>Collects the pattern types.</summary>
    /// <param name="pattern">The pattern under inspection.</param>
    /// <param name="model">The semantic model that resolves symbols for the tree.</param>
    /// <param name="coveredTypes">The pattern types the switch names without a guard.</param>
    private static void CollectPatternTypes(PatternSyntax pattern, SemanticModel model, List<ITypeSymbol> coveredTypes)
    {
        switch (pattern)
        {
            case DeclarationPatternSyntax declaration:
                AddPatternType(declaration.Type, model, coveredTypes);
                break;
            case RecursivePatternSyntax recursive:
                if (recursive.Type is not null)
                {
                    AddPatternType(recursive.Type, model, coveredTypes);
                }

                break;
            case TypePatternSyntax typePattern:
                AddPatternType(typePattern.Type, model, coveredTypes);
                break;
            case ParenthesizedPatternSyntax parenthesized:
                CollectPatternTypes(parenthesized.Pattern, model, coveredTypes);
                break;
            case BinaryPatternSyntax binary:
                CollectPatternTypes(binary.Left, model, coveredTypes);
                CollectPatternTypes(binary.Right, model, coveredTypes);
                break;
        }
    }

    /// <summary>Adds the pattern type.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="model">The semantic model that resolves symbols for the tree.</param>
    /// <param name="coveredTypes">The pattern types the switch names without a guard.</param>
    private static void AddPatternType(TypeSyntax type, SemanticModel model, List<ITypeSymbol> coveredTypes)
    {
        if (model.GetSymbolInfo(type).Symbol is ITypeSymbol symbol)
        {
            coveredTypes.Add(symbol);
        }
    }

    /// <summary>Returns true when the sealed case is covered; otherwise false.</summary>
    /// <param name="sealedCase">The sealed case under inspection.</param>
    /// <param name="coveredTypes">The pattern types the switch names without a guard.</param>
    /// <returns>True when the sealed case is covered; otherwise false.</returns>
    private static bool IsCovered(INamedTypeSymbol sealedCase, List<ITypeSymbol> coveredTypes)
    {
        foreach (var covered in coveredTypes)
        {
            if (covered is INamedTypeSymbol coveredType && DerivesFromOrEquals(sealedCase, coveredType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type derives from or equals the ancestor; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="ancestor">The ancestor type the check compares against.</param>
    /// <returns>True when the type derives from or equals the ancestor; otherwise false.</returns>
    private static bool DerivesFromOrEquals(INamedTypeSymbol type, INamedTypeSymbol ancestor)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }
}
