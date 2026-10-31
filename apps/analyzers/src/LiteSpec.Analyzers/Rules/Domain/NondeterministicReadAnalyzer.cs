using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1009: Domain receives time, randomness and new identifiers as arguments and reads no clock.</summary>
/// <follows>litespec/backend/domain#pass-nondeterministic-values-into-domain</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NondeterministicReadAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1009);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the nondeterministic read analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var types = new WellKnownTypes(context.Compilation);
        context.RegisterOperationAction(types.AnalyzeProperty, OperationKind.PropertyReference);
        context.RegisterOperationAction(types.AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(types.AnalyzeCreation, OperationKind.ObjectCreation);
    }

    /// <summary>Holds the well known types data the nondeterministic read analyzer code works with.</summary>
    private sealed class WellKnownTypes
    {
        /// <summary>Stores the date time used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _dateTime;
        /// <summary>Stores the date time offset used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _dateTimeOffset;
        /// <summary>Stores the time provider used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _timeProvider;
        /// <summary>Stores the guid used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _guid;
        /// <summary>Stores the environment used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _environment;
        /// <summary>Stores the stopwatch used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _stopwatch;
        /// <summary>Stores the random used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _random;
        /// <summary>Stores the random number generator used by the well known types code.</summary>
        private readonly INamedTypeSymbol? _randomNumberGenerator;

        /// <summary>Initializes a new instance of the <see cref="WellKnownTypes"/> class.</summary>
        /// <param name="compilation">The compilation whose types and references the check reads.</param>
        public WellKnownTypes(Compilation compilation)
        {
            _dateTime = compilation.GetTypeByMetadataName("System.DateTime");
            _dateTimeOffset = compilation.GetTypeByMetadataName("System.DateTimeOffset");
            _timeProvider = compilation.GetTypeByMetadataName("System.TimeProvider");
            _guid = compilation.GetTypeByMetadataName("System.Guid");
            _environment = compilation.GetTypeByMetadataName("System.Environment");
            _stopwatch = compilation.GetTypeByMetadataName("System.Diagnostics.Stopwatch");
            _random = compilation.GetTypeByMetadataName("System.Random");
            _randomNumberGenerator = compilation.GetTypeByMetadataName("System.Security.Cryptography.RandomNumberGenerator");
        }

        /// <summary>Analyzes the property.</summary>
        /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
        public void AnalyzeProperty(OperationAnalysisContext context)
        {
            var reference = (IPropertyReferenceOperation)context.Operation;
            if (!IsNondeterministicProperty(reference.Property))
            {
                return;
            }

            Report(context, reference, MemberLocation(reference.Syntax));
        }

        /// <summary>Analyzes the invocation.</summary>
        /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var method = invocation.TargetMethod;
            var containing = method.ContainingType;
            var name = method.Name;
            var nondeterministic =
                IsType(containing, _guid) && (name == "NewGuid" || name == "CreateVersion7")
                || IsType(containing, _stopwatch) && (name == "GetTimestamp" || name == "StartNew")
                || IsType(containing, _randomNumberGenerator);
            if (!nondeterministic)
            {
                return;
            }

            Report(context, invocation, InvocationLocation(invocation));
        }

        /// <summary>Analyzes the creation.</summary>
        /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
        public void AnalyzeCreation(OperationAnalysisContext context)
        {
            var creation = (IObjectCreationOperation)context.Operation;
            if (!IsType(creation.Type, _random))
            {
                return;
            }

            Report(context, creation, CreationLocation(creation));
        }

        /// <summary>Returns true when the property is nondeterministic property; otherwise false.</summary>
        /// <param name="property">The property declaration under inspection.</param>
        /// <returns>True when the property is nondeterministic property; otherwise false.</returns>
        private bool IsNondeterministicProperty(IPropertySymbol property)
        {
            var containing = property.ContainingType;
            var name = property.Name;
            if (IsType(containing, _dateTime))
            {
                return name == "Now" || name == "UtcNow" || name == "Today";
            }

            if (IsType(containing, _dateTimeOffset))
            {
                return name == "Now" || name == "UtcNow";
            }

            if (IsType(containing, _timeProvider))
            {
                return name == "System";
            }

            if (IsType(containing, _environment))
            {
                return name == "TickCount" || name == "TickCount64";
            }

            if (IsType(containing, _random))
            {
                return name == "Shared";
            }

            return false;
        }

        /// <summary>Returns true when the candidate is type; otherwise false.</summary>
        /// <param name="candidate">The candidate supplied for the check, or null when the compilation does not provide it.</param>
        /// <param name="known">The known supplied for the check, or null when the compilation does not provide it.</param>
        /// <returns>True when the candidate is type; otherwise false.</returns>
        private static bool IsType(ISymbol? candidate, INamedTypeSymbol? known) =>
            known is not null && SymbolEqualityComparer.Default.Equals(candidate, known);

        /// <summary>Reports the void.</summary>
        /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
        /// <param name="operation">The operation under inspection.</param>
        /// <param name="location">The source location the diagnostic reports at.</param>
        private static void Report(OperationAnalysisContext context, IOperation operation, Location location)
        {
            if (IsInsideNameof(operation.Syntax))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1009, location, operation.Syntax.ToString()));
        }

        /// <summary>Returns true when the syntax is inside nameof; otherwise false.</summary>
        /// <param name="syntax">The syntax node under inspection.</param>
        /// <returns>True when the syntax is inside nameof; otherwise false.</returns>
        private static bool IsInsideNameof(SyntaxNode syntax)
        {
            for (var node = syntax.Parent; node is not null; node = node.Parent)
            {
                if (node is InvocationExpressionSyntax invocation
                    && invocation.Expression is IdentifierNameSyntax name
                    && name.Identifier.ValueText == "nameof")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns the location.</summary>
        /// <param name="syntax">The syntax node under inspection.</param>
        /// <returns>The location.</returns>
        private static Location MemberLocation(SyntaxNode syntax) =>
            syntax is MemberAccessExpressionSyntax access ? access.Name.GetLocation() : syntax.GetLocation();

        /// <summary>Returns the location.</summary>
        /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
        /// <returns>The location.</returns>
        private static Location InvocationLocation(IInvocationOperation invocation) =>
            invocation.Syntax is InvocationExpressionSyntax syntax && syntax.Expression is MemberAccessExpressionSyntax access
                ? access.Name.GetLocation()
                : invocation.Syntax.GetLocation();

        /// <summary>Returns the location.</summary>
        /// <param name="creation">The object creation operation under inspection.</param>
        /// <returns>The location.</returns>
        private static Location CreationLocation(IObjectCreationOperation creation) =>
            creation.Syntax is ObjectCreationExpressionSyntax syntax && !syntax.Type.IsMissing
                ? syntax.Type.GetLocation()
                : creation.Syntax.GetLocation();
    }
}
