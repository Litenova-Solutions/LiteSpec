using LiteSpec.Analyzers.Rules.Layers.Knowledge;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2038, a module naming a type of a knowledge set.</summary>
public sealed class KnowledgeSetReferenceAnalyzerTests
{
    /// <summary>LS2038 reports a module naming a type of a knowledge set.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS2038 reports a module naming a type of a knowledge set")]
    public Task ReportsAModuleNamingASetType() =>
        Verify.AnalyzerAsync<KnowledgeSetReferenceAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Travel.France.Rates;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        private {|LS2038:FranceRates|}? _rates;
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceRates
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2038 accepts a module naming a type of a knowledge model.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS2038 accepts a module naming a type of a knowledge model")]
    public Task AcceptsAModuleNamingAModelType() =>
        Verify.AnalyzerAsync<KnowledgeSetReferenceAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Travel.Rates;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        private TravelRates? _rates;
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel/Rates/TravelRates.cs", """
                    namespace Claimwick.Travel.Rates;

                    public sealed class TravelRates
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2038 stays quiet in a host, which references a set to register it.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS2038 stays quiet in a host, which references a set to register it")]
    public Task StaysQuietInAHost() =>
        Verify.AnalyzerAsync<KnowledgeSetReferenceAnalyzer>(
            [
                ("Claimwick.Api/TravelRegistration.cs", """
                    using Claimwick.Travel.France.Rates;

                    namespace Claimwick.Api;

                    public sealed class TravelRegistration
                    {
                        private FranceRates? _rates;
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceRates
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Api");

    /// <summary>LS2038 stays quiet in the set's own compilation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS2038 stays quiet in the set's own compilation")]
    public Task StaysQuietInTheSetsOwnCompilation() =>
        Verify.AnalyzerAsync<KnowledgeSetReferenceAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceRates
                    {
                        private FranceMileage? _mileage;
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceMileage.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceMileage
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Travel.France");
}
