using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Tests;

/// <summary>LS4001: a test method contains at least one assertion, directly or in a step it calls.</summary>
/// <follows>litespec/backend/dotnet/testing#prove-each-scenario-with-a-test-that-calls-its-use-case</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestAssertionAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS4001);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Test;

    /// <summary>
    ///     Registers the analysis actions for the test assertion analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterOperationBlockAction(Analyze);

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
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

    /// <summary>Returns true when the method is test method; otherwise false.</summary>
    /// <param name="method">The method under inspection.</param>
    /// <returns>True when the method is test method; otherwise false.</returns>
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

    /// <summary>Returns true when the attribute is test attribute; otherwise false.</summary>
    /// <param name="attribute">The attribute supplied for the check, or null when the compilation does not provide it.</param>
    /// <returns>True when the attribute is test attribute; otherwise false.</returns>
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
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <returns>True when the contains-assertion condition holds; otherwise false.</returns>
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

    /// <summary>Returns true when the target is assertion; otherwise false.</summary>
    /// <param name="target">The element the check reports on.</param>
    /// <returns>True when the target is assertion; otherwise false.</returns>
    private static bool IsAssertion(IMethodSymbol target) =>
        target.ContainingType.Name == "Assert"
        || IsAssertionName(target.Name);

    /// <summary>Returns true when the name is assertion name; otherwise false.</summary>
    /// <param name="name">The name to convert or match.</param>
    /// <returns>True when the name is assertion name; otherwise false.</returns>
    private static bool IsAssertionName(string name) => name.StartsWith("Assert", StringComparison.Ordinal);

    /// <summary>Whether a called method declared in source calls an assertion in its body.</summary>
    /// <param name="target">The element the check reports on.</param>
    /// <param name="cancellationToken">The token that cancels the operation when the host aborts it.</param>
    /// <returns>True when the step-asserts condition holds; otherwise false.</returns>
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

    /// <summary>Returns true when the expression is assertion call; otherwise false.</summary>
    /// <param name="expression">The expression under inspection.</param>
    /// <returns>True when the expression is assertion call; otherwise false.</returns>
    private static bool IsAssertionCall(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "Assert" } } => true,
        MemberAccessExpressionSyntax member => IsAssertionName(member.Name.Identifier.ValueText),
        IdentifierNameSyntax identifier => IsAssertionName(identifier.Identifier.ValueText),
        GenericNameSyntax generic => IsAssertionName(generic.Identifier.ValueText),
        _ => false,
    };
}
