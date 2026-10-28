using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Tests;

/// <summary>LS4001: a test method contains at least one assertion, directly or in a step it calls.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestAssertionAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS4001);

    private protected override bool AppliesTo(Layer layer) => layer == Layer.Test;

    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterOperationBlockAction(Analyze);

    private static void Analyze(OperationBlockAnalysisContext context)
    {
        if (context.OwningSymbol is not IMethodSymbol method
            || !IsTestMethod(method)
            || ContainsAssertion(context))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS4001, method.Locations[0], method.Name));
    }

    private static bool IsTestMethod(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            if (IsTestAttribute(attribute.AttributeClass))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTestAttribute(INamedTypeSymbol? attribute)
    {
        if (attribute is null)
        {
            return false;
        }

        var name = attribute.Name;
        const string suffix = "Attribute";
        if (name.EndsWith(suffix, StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - suffix.Length);
        }

        return name is "Fact" or "Theory" or "Scenario" or "Test" or "TestCase" or "TestCaseSource" or "TestMethod" or "DataTestMethod";
    }

    /// <summary>
    ///     Whether the test asserts in its own body, or in the body of a method it calls that is
    ///     declared in this compilation: a scenario asserts in the step methods it runs.
    /// </summary>
    private static bool ContainsAssertion(OperationBlockAnalysisContext context)
    {
        foreach (var block in context.OperationBlocks)
        {
            foreach (var operation in block.Descendants())
            {
                if (operation is not IInvocationOperation invocation)
                {
                    continue;
                }

                if (IsAssertion(invocation.TargetMethod) || StepAsserts(invocation.TargetMethod, context.CancellationToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsAssertion(IMethodSymbol target) =>
        target.ContainingType.Name == "Assert"
        || IsAssertionName(target.Name);

    private static bool IsAssertionName(string name) => name.StartsWith("Assert", StringComparison.Ordinal);

    /// <summary>Whether a called method declared in source calls an assertion in its body.</summary>
    private static bool StepAsserts(IMethodSymbol target, System.Threading.CancellationToken cancellationToken)
    {
        foreach (var reference in target.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(cancellationToken) is not BaseMethodDeclarationSyntax declaration)
            {
                continue;
            }

            foreach (var node in declaration.DescendantNodes())
            {
                if (node is InvocationExpressionSyntax call && IsAssertionCall(call.Expression))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsAssertionCall(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "Assert" } } => true,
        MemberAccessExpressionSyntax member => IsAssertionName(member.Name.Identifier.ValueText),
        IdentifierNameSyntax identifier => IsAssertionName(identifier.Identifier.ValueText),
        GenericNameSyntax generic => IsAssertionName(generic.Identifier.ValueText),
        _ => false,
    };
}
