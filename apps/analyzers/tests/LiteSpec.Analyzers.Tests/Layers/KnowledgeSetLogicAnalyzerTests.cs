using LiteSpec.Analyzers.Rules.Layers.Knowledge;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2039, logic declared in a knowledge set.</summary>
public sealed class KnowledgeSetLogicAnalyzerTests
{
    /// <summary>The knowledge model the set compilations reference.</summary>
    private static readonly Verify.ReferencedAssembly Model = new(
        "Claimwick.Travel",
        [
            ("src/Knowledge/Claimwick.Travel/TravelModel.cs", """
                namespace Claimwick.Travel;

                public sealed class TravelModel
                {
                }
                """),
        ]);

    /// <summary>The knowledge model with a typed key the sets build from literals.</summary>
    private static readonly Verify.ReferencedAssembly KeyedModel = new(
        "Claimwick.Travel",
        [
            ("src/Knowledge/Claimwick.Travel/RateKey.cs", """
                namespace Claimwick.Travel;

                public sealed record RateKey(string Value)
                {
                    public static RateKey Parse(string value) => new(value);
                }
                """),
        ]);

    /// <summary>LS2039 stays quiet on a row building its typed key through the model's factory from a literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact(DisplayName = "LS2039 allows a model key factory over a literal")]
    public Task AllowsAModelKeyFactoryOverALiteral() =>
        Verify.AnalyzerWithMetadataAsync<KnowledgeSetLogicAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    using Claimwick.Travel;

                    namespace Claimwick.Travel.France.Rates;

                    public static class FranceRates
                    {
                        public static RateKey FullDay { get; } = RateKey.Parse("full-day");
                    }
                    """),
            ],
            [KeyedModel],
            assemblyName: "Claimwick.Travel.France");

    /// <summary>LS2039 reports a model factory called with a value that is not a constant.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact(DisplayName = "LS2039 reports a model factory over a computed value")]
    public Task ReportsAModelFactoryOverAComputedValue() =>
        Verify.AnalyzerWithMetadataAsync<KnowledgeSetLogicAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    using Claimwick.Travel;

                    namespace Claimwick.Travel.France.Rates;

                    public static class FranceRates
                    {
                        private static readonly string Prefix = "full";

                        public static RateKey {|LS2039:FullDay|} { get; } = RateKey.Parse(Prefix + "-day");
                    }
                    """),
            ],
            [KeyedModel],
            assemblyName: "Claimwick.Travel.France");

    /// <summary>LS2039 reports a set method choosing with a branch.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact(DisplayName = "LS2039 reports a set method choosing with a branch")]
    public Task ReportsASetMethodChoosingWithABranch() =>
        Verify.AnalyzerWithMetadataAsync<KnowledgeSetLogicAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceRates
                    {
                        public decimal {|LS2039:For|}(string code)
                        {
                            if (code.Length == 0)
                            {
                                return 0m;
                            }

                            return 1m;
                        }
                    }
                    """),
            ],
            [Model],
            assemblyName: "Claimwick.Travel.France");

    /// <summary>LS2039 reports a set field initialized with a call.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact(DisplayName = "LS2039 reports a set field initialized with a call")]
    public Task ReportsASetFieldInitializedWithACall() =>
        Verify.AnalyzerWithMetadataAsync<KnowledgeSetLogicAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceRates
                    {
                        private static readonly decimal {|LS2039:Rate|} = decimal.Parse("1.5");
                    }
                    """),
            ],
            [Model],
            assemblyName: "Claimwick.Travel.France");

    /// <summary>LS2039 reports a set property choosing with a switch.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact(DisplayName = "LS2039 reports a set property choosing with a switch")]
    public Task ReportsASetPropertyChoosingWithASwitch() =>
        Verify.AnalyzerWithMetadataAsync<KnowledgeSetLogicAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceRates
                    {
                        private readonly string _code = "FR";

                        public decimal {|LS2039:Amount|}
                        {
                            get
                            {
                                switch (_code)
                                {
                                    case "FR": return 42m;
                                    default: return 0m;
                                }
                            }
                        }
                    }
                    """),
            ],
            [Model],
            assemblyName: "Claimwick.Travel.France");

    /// <summary>LS2039 accepts rows built through constructors.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact(DisplayName = "LS2039 accepts rows built through constructors")]
    public Task AcceptsRowsBuiltThroughConstructors() =>
        Verify.AnalyzerWithMetadataAsync<KnowledgeSetLogicAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed record PerDiemRow(string Country, decimal Amount);

                    public sealed class FranceRates
                    {
                        private static readonly PerDiemRow Default = new("FR", 42m);

                        public PerDiemRow Current { get; } = new("FR", 42m);

                        public PerDiemRow For() => new("FR", 42m);
                    }
                    """),
            ],
            [Model],
            assemblyName: "Claimwick.Travel.France");

    /// <summary>LS2039 stays quiet outside a knowledge set.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact(DisplayName = "LS2039 stays quiet outside a knowledge set")]
    public Task StaysQuietOutsideAKnowledgeSet() =>
        Verify.AnalyzerAsync<KnowledgeSetLogicAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public decimal For(string code)
                        {
                            if (code.Length == 0)
                            {
                                return 0m;
                            }

                            return 1m;
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
