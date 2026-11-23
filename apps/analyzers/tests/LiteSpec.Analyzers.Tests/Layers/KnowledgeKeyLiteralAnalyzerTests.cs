using LiteSpec.Analyzers.Rules.Layers.Knowledge;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2040, a knowledge key built from a string literal.</summary>
public sealed class KnowledgeKeyLiteralAnalyzerTests
{
    /// <summary>The knowledge model key the tests build.</summary>
    private const string KeyFile = "src/Knowledge/Claimwick.Travel/Keys/CountryKey.cs";

    /// <summary>The knowledge model key source the tests build.</summary>
    private const string KeySource = """
        namespace Claimwick.Travel.Keys;

        public readonly record struct CountryKey(string Code)
        {
            public static CountryKey Parse(string code) => new(code);

            public static CountryKey From(string code) => new(code);
        }
        """;

    /// <summary>LS2040 reports a knowledge key constructed from a string literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-by-role-and-date</proves>
    [Fact(DisplayName = "LS2040 reports a knowledge key constructed from a string literal")]
    public Task ReportsAKeyConstructedFromALiteral() =>
        Verify.AnalyzerAsync<KnowledgeKeyLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Travel.Keys;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public void Handle()
                        {
                            var key = new {|LS2040:CountryKey|}("FR");
                        }
                    }
                    """),
                (KeyFile, KeySource),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2040 reports a knowledge key parsed from a string literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-by-role-and-date</proves>
    [Fact(DisplayName = "LS2040 reports a knowledge key parsed from a string literal")]
    public Task ReportsAKeyParsedFromALiteral() =>
        Verify.AnalyzerAsync<KnowledgeKeyLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Travel.Keys;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public void Handle()
                        {
                            var key = CountryKey.{|LS2040:Parse|}("FR");
                        }
                    }
                    """),
                (KeyFile, KeySource),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2040 reports a knowledge key read from a string literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-by-role-and-date</proves>
    [Fact(DisplayName = "LS2040 reports a knowledge key read from a string literal")]
    public Task ReportsAKeyReadFromALiteral() =>
        Verify.AnalyzerAsync<KnowledgeKeyLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Travel.Keys;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public void Handle()
                        {
                            var key = CountryKey.{|LS2040:From|}("FR");
                        }
                    }
                    """),
                (KeyFile, KeySource),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2040 reports a marker key built from a string literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-by-role-and-date</proves>
    [Fact(DisplayName = "LS2040 reports a marker key built from a string literal")]
    public Task ReportsAMarkerKeyBuiltFromALiteral() =>
        Verify.AnalyzerAsync<KnowledgeKeyLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal interface IKnowledgeKey
                    {
                    }

                    internal readonly record struct RegionKey(string Code) : IKnowledgeKey;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public void Handle()
                        {
                            var key = new {|LS2040:RegionKey|}("north");
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2040 accepts a knowledge key passed in with no literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-by-role-and-date</proves>
    [Fact(DisplayName = "LS2040 accepts a knowledge key passed in with no literal")]
    public Task AcceptsAKeyPassedInWithNoLiteral() =>
        Verify.AnalyzerAsync<KnowledgeKeyLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Travel.Keys;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public string Handle(CountryKey key)
                        {
                            return key.Code;
                        }
                    }
                    """),
                (KeyFile, KeySource),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2040 accepts a knowledge key built from a variable.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-by-role-and-date</proves>
    [Fact(DisplayName = "LS2040 accepts a knowledge key built from a variable")]
    public Task AcceptsAKeyBuiltFromAVariable() =>
        Verify.AnalyzerAsync<KnowledgeKeyLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Travel.Keys;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public CountryKey Handle(string code)
                        {
                            return new CountryKey(code);
                        }
                    }
                    """),
                (KeyFile, KeySource),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2040 stays quiet for a key type outside a knowledge model.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-by-role-and-date</proves>
    [Fact(DisplayName = "LS2040 stays quiet for a key type outside a knowledge model")]
    public Task StaysQuietForAKeyTypeOutsideAModel() =>
        Verify.AnalyzerAsync<KnowledgeKeyLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed record CacheKey(string Value);

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public void Handle()
                        {
                            var key = new CacheKey("spending");
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2040 stays quiet inside a knowledge set.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-by-role-and-date</proves>
    [Fact(DisplayName = "LS2040 stays quiet inside a knowledge set")]
    public Task StaysQuietInsideAKnowledgeSet() =>
        Verify.AnalyzerAsync<KnowledgeKeyLiteralAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    using Claimwick.Travel.Keys;

                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceRates
                    {
                        public CountryKey Default()
                        {
                            return new CountryKey("FR");
                        }
                    }
                    """),
                (KeyFile, KeySource),
            ],
            assemblyName: "Claimwick.Spending");
}
