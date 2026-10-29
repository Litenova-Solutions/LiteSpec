using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests for LS5006, the marked event or exception that is not partial.</summary>
public sealed class PartialTypeTests
{
    /// <proves>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</proves>
    [Fact(DisplayName = "LS5006 reports a marked event that is not partial")]
    public Task ReportsAMarkedEventThatIsNotPartial() =>
        Verify.AnalyzerWithRecordsAsync<PartialTypeAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed record {|LS5006:ExpenseSubmittedEvent|} : IDomainEvent;
                """)],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Domain");

    /// <proves>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</proves>
    [Fact(DisplayName = "LS5006 reports a marked exception that is not partial")]
    public Task ReportsAMarkedExceptionThatIsNotPartial() =>
        Verify.AnalyzerWithRecordsAsync<PartialTypeAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public abstract class DomainException : Exception
                {
                    public virtual string Code => "base";
                }

                /// <implements>rule/spending.receipt-required</implements>
                public sealed class {|LS5006:ReceiptRequiredException|} : DomainException
                {
                }
                """)],
            [ProjectFile(), Rule()],
            assemblyName: "Shop.Domain");

    /// <proves>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</proves>
    [Fact(DisplayName = "LS5006 accepts a partial event")]
    public Task AcceptsAPartialEvent() =>
        Verify.AnalyzerWithRecordsAsync<PartialTypeAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed partial record ExpenseSubmittedEvent : IDomainEvent;
                """)],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Domain");

    /// <proves>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</proves>
    [Fact(DisplayName = "LS5006 ignores an unmarked event")]
    public Task IgnoresAnUnmarkedEvent() =>
        Verify.AnalyzerWithRecordsAsync<PartialTypeAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IDomainEvent;

                public sealed record ExpenseSubmittedEvent : IDomainEvent;
                """)],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Domain");

    /// <proves>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</proves>
    [Fact(DisplayName = "LS5006 does not run outside Domain")]
    public Task DoesNotRunOutsideDomain() =>
        Verify.AnalyzerWithRecordsAsync<PartialTypeAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed record ExpenseSubmittedEvent : IDomainEvent;
                """)],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Application");

    /// <proves>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</proves>
    [Fact(DisplayName = "LS5006 accepts a two-part event with every part partial")]
    public Task AcceptsTwoPartEventWithEveryPartPartial() =>
        Verify.AnalyzerWithRecordsAsync<PartialTypeAnalyzer>(
            [
                ("First.cs", """
                    namespace Claimwick.Spending;

                    public interface IDomainEvent;

                    /// <implements>event/expense.submitted</implements>
                    public sealed partial record ExpenseSubmittedEvent : IDomainEvent;
                    """),
                ("Second.cs", """
                    namespace Claimwick.Spending;

                    public sealed partial record ExpenseSubmittedEvent;
                    """),
            ],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Domain");

    /// <proves>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</proves>
    [Fact(DisplayName = "LS5006 reports the enclosing type of a nested partial event")]
    public Task ReportsEnclosingTypeOfNestedPartialEvent() =>
        Verify.AnalyzerWithRecordsAsync<PartialTypeAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IDomainEvent;

                public sealed class {|LS5006:ExpenseEvents|}
                {
                    /// <implements>event/expense.submitted</implements>
                    public sealed partial record ExpenseSubmittedEvent : IDomainEvent;
                }
                """)],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Domain");

    /// <proves>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</proves>
    [Fact(DisplayName = "LS5006 accepts a nested event with a partial enclosing type")]
    public Task AcceptsNestedEventWithPartialEnclosingType() =>
        Verify.AnalyzerWithRecordsAsync<PartialTypeAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IDomainEvent;

                public sealed partial class ExpenseEvents
                {
                    /// <implements>event/expense.submitted</implements>
                    public sealed partial record ExpenseSubmittedEvent : IDomainEvent;
                }
                """)],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Domain");
}
