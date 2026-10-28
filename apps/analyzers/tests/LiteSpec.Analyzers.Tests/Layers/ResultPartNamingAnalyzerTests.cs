using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2009, result part names.</summary>
public sealed class ResultPartNamingAnalyzerTests
{
    [Fact(DisplayName = "LS2009 accepts a part named after its query result")]
    public Task AcceptsAPartNamedAfterItsQueryResult() =>
        Verify.AnalyzerAsync<ResultPartNamingAnalyzer>("""
            namespace Shop.Loans;

            public sealed record ReadLoanQueryResult(int LoanId);

            public sealed record ReadLoanQueryResultHold(int HoldId);
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2009 accepts a part named after its command result")]
    public Task AcceptsAPartNamedAfterItsCommandResult() =>
        Verify.AnalyzerAsync<ResultPartNamingAnalyzer>("""
            namespace Shop.Spending;

            public sealed record ApproveExpenseCommandResult(int ExpenseId);

            public sealed record ApproveExpenseCommandResultLine(int LineId);
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2009 reports a part not named after its result")]
    public Task ReportsAPartNotNamedAfterItsResult() =>
        Verify.AnalyzerAsync<ResultPartNamingAnalyzer>("""
            namespace Shop.Loans;

            public sealed record ReadLoanQueryResult(int LoanId, LoanHoldItem[] Holds);

            public sealed record {|LS2009:LoanHoldItem|}(int HoldId);
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2009 ignores the message, handler, validator, and definition")]
    public Task IgnoresTheSurroundingTypes() =>
        Verify.AnalyzerAsync<ResultPartNamingAnalyzer>("""
            namespace Shop.Loans;

            public sealed record ReadLoanQuery(int LoanId);

            public sealed record ReadLoanQueryResult(int LoanId);

            internal sealed class ReadLoanQueryHandler
            {
            }

            internal sealed class ReadLoanQueryValidator
            {
            }

            internal sealed class ReadLoanQueryDefinition
            {
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2009 ignores a type where no result is declared")]
    public Task IgnoresATypeWhereNoResultIsDeclared() =>
        Verify.AnalyzerAsync<ResultPartNamingAnalyzer>("""
            namespace Shop.Loans;

            public sealed record LoanHoldItem(int HoldId);
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2009 ignores a part outside Application and Infrastructure")]
    public Task IgnoresAPartOutsideItsLayers() =>
        Verify.AnalyzerAsync<ResultPartNamingAnalyzer>("""
            namespace Shop.Loans;

            public sealed record ReadLoanQueryResult(int LoanId);

            public sealed record LoanHoldItem(int HoldId);
            """, assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS2009 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<ResultPartNamingAnalyzer>("""
            namespace Shop.Core;

            public sealed record ReadLoanQueryResult(int LoanId, LoanHoldItem[] Holds);

            public sealed record {|LS2009:LoanHoldItem|}(int HoldId);
            """, assemblyName: "Shop.Core", layer: "Application");

    [Fact(DisplayName = "LS2009 ignores a helper the result does not reach")]
    public Task IgnoresAHelperTheResultDoesNotReach() =>
        Verify.AnalyzerAsync<ResultPartNamingAnalyzer>("""
            namespace Shop.Loans;

            public sealed record ReadLoanQueryResult(int LoanId);

            internal sealed class LoanPageReader
            {
            }
            """, assemblyName: "Shop.Application");
}
