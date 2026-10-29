using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2014: a command handler never finds a record and creates it in the same method.</summary>
/// <follows>litespec/backend/application#implement-one-outcome-path-per-command</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FindOrCreateAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2014);

    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure;

    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterOperationBlockAction(AnalyzeBlock);

    private static void AnalyzeBlock(OperationBlockAnalysisContext context)
    {
        if (context.OwningSymbol is not IMethodSymbol method)
        {
            return;
        }

        if (!IsCommandHandler(method.ContainingType))
        {
            return;
        }

        // Each local that holds what a repository find returned, with the type it found.
        var found = new Dictionary<ILocalSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var block in context.OperationBlocks)
        {
            foreach (var operation in block.Descendants())
            {
                switch (operation)
                {
                    case IVariableDeclaratorOperation declarator
                        when declarator.Initializer != null && FoundType(declarator.Initializer.Value) is { } declared:
                        found[declarator.Symbol] = declared;
                        break;
                    case ISimpleAssignmentOperation { Target: ILocalReferenceOperation target } assignment
                        when FoundType(assignment.Value) is { } assigned:
                        found[target.Local] = assigned;
                        break;
                }
            }
        }

        foreach (var block in context.OperationBlocks)
        {
            foreach (var operation in block.Descendants())
            {
                switch (operation)
                {
                    case IObjectCreationOperation creation
                        when creation.Type != null && OnAbsentPath(creation, Unwrap(creation.Type), found):
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.LS2014,
                            CreationLocation(creation),
                            method.ContainingType.Name,
                            creation.Type.Name));
                        break;
                    case IInvocationOperation creation
                        when creation.TargetMethod.IsStatic
                        && !IsVoid(creation.TargetMethod.ReturnType)
                        && OnAbsentPath(creation, Unwrap(creation.TargetMethod.ReturnType), found):
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.LS2014,
                            MemberLocation(creation),
                            method.ContainingType.Name,
                            Unwrap(creation.TargetMethod.ReturnType).Name));
                        break;
                }
            }
        }
    }

    // The type a repository find returned, when the value is that find, awaited or converted.
    private static ITypeSymbol? FoundType(IOperation value)
    {
        var inner = Strip(value);
        return inner is IInvocationOperation invocation && IsRepositoryFind(invocation, out var type) ? type : null;
    }

    // Whether the creation runs only where a find of the created type came back empty: on the null
    // side of a test of the found value, after a branch that leaves when it was found, or as the
    // fallback of a coalesce.
    private static bool OnAbsentPath(IOperation creation, ITypeSymbol created, Dictionary<ILocalSymbol, ITypeSymbol> found)
    {
        // A creation gated by an independent policy switch runs only in one policy arm,
        // not whenever the find came back empty, so it is not get-or-create.
        if (InsideUnrelatedSwitch(creation, created, found))
        {
            return false;
        }

        var child = creation;
        for (var parent = creation.Parent; parent != null; child = parent, parent = parent.Parent)
        {
            switch (parent)
            {
                case IConditionalOperation conditional when child == conditional.WhenTrue:
                    if (Holds(conditional.Condition, created, found, whenTrue: true))
                    {
                        return true;
                    }

                    break;
                case IConditionalOperation conditional when child == conditional.WhenFalse:
                    if (Holds(conditional.Condition, created, found, whenTrue: false))
                    {
                        return true;
                    }

                    break;
                case ICoalesceOperation coalesce when child == coalesce.WhenNull:
                    if (RefersToFound(coalesce.Value, created, found))
                    {
                        return true;
                    }

                    break;
                case ICoalesceAssignmentOperation coalesce when child == coalesce.Value:
                    if (RefersToFound(coalesce.Target, created, found))
                    {
                        return true;
                    }

                    break;
                case IBlockOperation body:
                    foreach (var earlier in body.Operations.TakeWhile(statement => statement != child))
                    {
                        if (earlier is IConditionalOperation { WhenFalse: null } guard
                            && Leaves(guard.WhenTrue)
                            && Holds(guard.Condition, created, found, whenTrue: false))
                        {
                            return true;
                        }
                    }

                    break;
            }
        }

        return false;
    }

    private static bool InsideUnrelatedSwitch(IOperation creation, ITypeSymbol created, Dictionary<ILocalSymbol, ITypeSymbol> found)
    {
        for (var parent = creation.Parent; parent != null; parent = parent.Parent)
        {
            switch (parent)
            {
                case ISwitchOperation statement when !RefersToFound(statement.Value, created, found):
                    return true;
                case ISwitchExpressionOperation expression when !RefersToFound(expression.Value, created, found):
                    return true;
            }
        }

        return false;
    }

    // Whether taking the given branch of the condition means the found value is absent.
    private static bool Holds(
        IOperation condition,
        ITypeSymbol created,
        Dictionary<ILocalSymbol, ITypeSymbol> found,
        bool whenTrue)
    {
        condition = Strip(condition);

        if (condition is IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } not)
        {
            return Holds(not.Operand, created, found, !whenTrue);
        }

        if (condition is IBinaryOperation binary
            && binary.OperatorKind is BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr)
        {
            // Taking the true side of an and, or the false side of an or, means both operands took
            // that side, so either one settles it.
            var both = (binary.OperatorKind == BinaryOperatorKind.ConditionalAnd) == whenTrue;
            return both
                && (Holds(binary.LeftOperand, created, found, whenTrue)
                    || Holds(binary.RightOperand, created, found, whenTrue));
        }

        if (NullTest(condition, created, found) is not { } testsNull)
        {
            return false;
        }

        // The true side of a null test, or the false side of a presence test, means absent.
        return testsNull == whenTrue;
    }

    // True when the condition is true exactly when the found value is null, false when it is true
    // exactly when the value is present, and null when it tests no found value.
    private static bool? NullTest(IOperation condition, ITypeSymbol created, Dictionary<ILocalSymbol, ITypeSymbol> found)
    {
        switch (condition)
        {
            case IIsPatternOperation pattern when RefersToFound(pattern.Value, created, found):
                return PatternMatchesNull(pattern.Pattern);
            case IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals } binary:
                var equals = binary.OperatorKind == BinaryOperatorKind.Equals;
                if (IsNull(binary.RightOperand) && RefersToFound(binary.LeftOperand, created, found))
                {
                    return equals;
                }

                if (IsNull(binary.LeftOperand) && RefersToFound(binary.RightOperand, created, found))
                {
                    return equals;
                }

                return null;
            default:
                return null;
        }
    }

    private static bool? PatternMatchesNull(IPatternOperation pattern) =>
        pattern switch
        {
            IConstantPatternOperation constant => IsNull(constant.Value) ? true : null,
            INegatedPatternOperation negated => PatternMatchesNull(negated.Pattern) is { } inner ? !inner : null,
            IRecursivePatternOperation or IDeclarationPatternOperation or ITypePatternOperation => false,
            _ => null,
        };

    private static bool RefersToFound(IOperation value, ITypeSymbol created, Dictionary<ILocalSymbol, ITypeSymbol> found)
    {
        var inner = Strip(value);

        if (inner is ILocalReferenceOperation local && found.TryGetValue(local.Local, out var type))
        {
            return SymbolEqualityComparer.Default.Equals(type, created);
        }

        return inner is IInvocationOperation invocation
            && IsRepositoryFind(invocation, out var direct)
            && SymbolEqualityComparer.Default.Equals(direct, created);
    }

    // Whether a branch always leaves the method or the loop around it.
    private static bool Leaves(IOperation branch) =>
        branch switch
        {
            IReturnOperation or IThrowOperation or IBranchOperation => true,
            IExpressionStatementOperation { Operation: IThrowOperation } => true,
            IBlockOperation block => block.Operations.Length > 0 && Leaves(block.Operations[block.Operations.Length - 1]),
            _ => false,
        };

    private static bool IsNull(IOperation operation) =>
        Strip(operation) is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } };

    private static IOperation Strip(IOperation operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation conversion:
                    operation = conversion.Operand;
                    continue;
                case IAwaitOperation awaited:
                    operation = awaited.Operation;
                    continue;
                case IParenthesizedOperation parenthesized:
                    operation = parenthesized.Operand;
                    continue;
                default:
                    return operation;
            }
        }
    }

    private static bool IsRepositoryFind(IInvocationOperation invocation, out ITypeSymbol found)
    {
        found = invocation.TargetMethod.ReturnType;
        if (!invocation.TargetMethod.Name.StartsWith("Find", StringComparison.Ordinal))
        {
            return false;
        }

        var receiver = invocation.Instance?.Type;
        if (receiver == null || !receiver.Name.EndsWith("Repository", StringComparison.Ordinal))
        {
            return false;
        }

        found = Unwrap(invocation.TargetMethod.ReturnType);
        return !IsVoid(found);
    }

    private static bool IsVoid(ITypeSymbol type) => type.SpecialType == SpecialType.System_Void;

    private static ITypeSymbol Unwrap(ITypeSymbol type)
    {
        while (type is INamedTypeSymbol named && named.TypeArguments.Length == 1)
        {
            if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                type = named.TypeArguments[0];
                continue;
            }

            if ((named.Name is "Task" or "ValueTask") && InSystem(named.ContainingNamespace))
            {
                type = named.TypeArguments[0];
                continue;
            }

            break;
        }

        return type;
    }

    private static bool InSystem(INamespaceSymbol? space)
    {
        var name = space?.ToDisplayString();
        return name != null
            && (name.Equals("System", StringComparison.Ordinal)
                || name.StartsWith("System.", StringComparison.Ordinal));
    }

    private static bool IsCommandHandler(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        if (type.Name.EndsWith("CommandHandler", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "ICommandHandler")
            {
                return true;
            }
        }

        return false;
    }

    private static Location CreationLocation(IObjectCreationOperation creation) =>
        creation.Syntax is ObjectCreationExpressionSyntax syntax && syntax.Type != null
            ? syntax.Type.GetLocation()
            : creation.Syntax.GetLocation();

    private static Location MemberLocation(IInvocationOperation invocation)
    {
        if (invocation.Syntax is InvocationExpressionSyntax syntax)
        {
            if (syntax.Expression is MemberAccessExpressionSyntax access)
            {
                return access.Name.GetLocation();
            }

            if (syntax.Expression is MemberBindingExpressionSyntax binding)
            {
                return binding.Name.GetLocation();
            }

            return syntax.Expression.GetLocation();
        }

        return invocation.Syntax.GetLocation();
    }
}
