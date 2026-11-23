using LiteSpec.Analyzers.Rules.Layers.Knowledge;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2039, logic declared in a knowledge set.</summary>
public sealed class KnowledgeSetLogicAnalyzerTests
{
    /// <summary>LS2039 reports a set method choosing with a branch.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact(DisplayName = "LS2039 reports a set method choosing with a branch")]
    public Task ReportsASetMethodChoosingWithABranch() =>
        Verify.AnalyzerAsync<KnowledgeSetLogicAnalyzer>(
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
            assemblyName: "Claimwick.Travel.France");

    /// <summary>LS2039 reports a set field initialized with a call.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact(DisplayName = "LS2039 reports a set field initialized with a call")]
    public Task ReportsASetFieldInitializedWithACall() =>
        Verify.AnalyzerAsync<KnowledgeSetLogicAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceRates
                    {
                        private static readonly decimal {|LS2039:Rate|} = decimal.Parse("1.5");
                    }
                    """),
            ],
            assemblyName: "Claimwick.Travel.France");

    /// <summary>LS2039 reports a set property choosing with a switch.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact(DisplayName = "LS2039 reports a set property choosing with a switch")]
    public Task ReportsASetPropertyChoosingWithASwitch() =>
        Verify.AnalyzerAsync<KnowledgeSetLogicAnalyzer>(
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
            assemblyName: "Claimwick.Travel.France");

    /// <summary>LS2039 accepts rows built through constructors.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact(DisplayName = "LS2039 accepts rows built through constructors")]
    public Task AcceptsRowsBuiltThroughConstructors() =>
        Verify.AnalyzerAsync<KnowledgeSetLogicAnalyzer>(
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
