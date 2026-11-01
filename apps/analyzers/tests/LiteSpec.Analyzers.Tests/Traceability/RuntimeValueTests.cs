using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests for LS5005, the hand-written runtime value drifting from its record.</summary>
public sealed class RuntimeValueTests
{
    /// <summary>LS5005 reports an event Classification differing from its event.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "LS5005 reports an event Classification differing from its event")]
    public Task ReportsAnEventClassificationDrift() =>
        Verify.AnalyzerWithRecordsAsync<RuntimeValueAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed partial record ExpenseSubmittedEvent : IDomainEvent
                {
                    public const string {|LS5005:Classification|} = "event/expense.placed";
                }
                """)],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5005 accepts an event without a hand-written Classification.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "LS5005 accepts an event without a hand-written Classification")]
    public Task AcceptsAnEventWithoutAClassification() =>
        Verify.AnalyzerWithRecordsAsync<RuntimeValueAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed partial record ExpenseSubmittedEvent : IDomainEvent;
                """)],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5005 accepts an event Classification equal to its event.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "LS5005 accepts an event Classification equal to its event")]
    public Task AcceptsAnEventClassificationEqualToItsEvent() =>
        Verify.AnalyzerWithRecordsAsync<RuntimeValueAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed partial record ExpenseSubmittedEvent : IDomainEvent
                {
                    public const string Classification = "event/expense.submitted";
                }
                """)],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5005 reports an exception Code differing from its rule refusal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "LS5005 reports an exception Code differing from its rule refusal")]
    public Task ReportsAnExceptionCodeDrift() =>
        Verify.AnalyzerWithRecordsAsync<RuntimeValueAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public abstract class DomainException : Exception
                {
                    public virtual string Code => "base";
                }

                /// <implements>rule/spending.receipt-required</implements>
                public sealed class ReceiptRequiredException : DomainException
                {
                    public override string {|LS5005:Code|} => "spending.other-code";
                }
                """)],
            [ProjectFile(), Rule(), Result()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5005 accepts an exception Code equal to its rule refusal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "LS5005 accepts an exception Code equal to its rule refusal")]
    public Task AcceptsAnExceptionCodeEqualToItsRefusal() =>
        Verify.AnalyzerWithRecordsAsync<RuntimeValueAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public abstract class DomainException : Exception
                {
                    public virtual string Code => "base";
                }

                /// <implements>rule/spending.receipt-required</implements>
                public sealed class ReceiptRequiredException : DomainException
                {
                    public override string Code => "spending.receipt-missing";
                }
                """)],
            [ProjectFile(), Rule(), Result()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5005 accepts an exception without a hand-written Code.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "LS5005 accepts an exception without a hand-written Code")]
    public Task AcceptsAnExceptionWithoutACode() =>
        Verify.AnalyzerWithRecordsAsync<RuntimeValueAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public abstract class DomainException : Exception
                {
                    public virtual string Code => "base";
                }

                /// <implements>rule/spending.receipt-required</implements>
                public sealed class ReceiptRequiredException : DomainException
                {
                }
                """)],
            [ProjectFile(), Rule(), Result()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5005 ignores a rule naming no refusal result with a code.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "LS5005 ignores a rule naming no refusal result with a code")]
    public Task IgnoresARuleWithoutARefusalCode() =>
        Verify.AnalyzerWithRecordsAsync<RuntimeValueAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public abstract class DomainException : Exception
                {
                    public virtual string Code => "base";
                }

                /// <implements>rule/spending.receipt-required</implements>
                public sealed class ReceiptRequiredException : DomainException
                {
                    public override string Code => "spending.other-code";
                }
                """)],
            [ProjectFile(), Rule(refusal: "result/submit-expense.ghost")],
            assemblyName: "Shop.Domain");

    /// <summary>LS5005 reports an exception Code differing from its bare refusal code.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "LS5005 reports an exception Code differing from its bare refusal code")]
    public Task ReportsAnExceptionCodeDriftFromABareRefusal() =>
        Verify.AnalyzerWithRecordsAsync<RuntimeValueAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public abstract class DomainException : Exception
                {
                    public virtual string Code => "base";
                }

                /// <implements>rule/spending.receipt-required</implements>
                public sealed class ReceiptRequiredException : DomainException
                {
                    public override string {|LS5005:Code|} => "spending.other-code";
                }
                """)],
            [ProjectFile(), Rule(refusal: "spending.receipt-missing")],
            assemblyName: "Shop.Domain");

    /// <summary>LS5005 accepts an exception Code equal to its bare refusal code.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "LS5005 accepts an exception Code equal to its bare refusal code")]
    public Task AcceptsAnExceptionCodeEqualToItsBareRefusal() =>
        Verify.AnalyzerWithRecordsAsync<RuntimeValueAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public abstract class DomainException : Exception
                {
                    public virtual string Code => "base";
                }

                /// <implements>rule/spending.receipt-required</implements>
                public sealed class ReceiptRequiredException : DomainException
                {
                    public override string Code => "spending.receipt-missing";
                }
                """)],
            [ProjectFile(), Rule(refusal: "spending.receipt-missing")],
            assemblyName: "Shop.Domain");

    /// <summary>LS5005 ignores a refusal pointing at a success result with a code.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "LS5005 ignores a refusal pointing at a success result with a code")]
    public Task IgnoresARefusalToASuccessResult() =>
        Verify.AnalyzerWithRecordsAsync<RuntimeValueAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public abstract class DomainException : Exception
                {
                    public virtual string Code => "base";
                }

                /// <implements>rule/spending.receipt-required</implements>
                public sealed class ReceiptRequiredException : DomainException
                {
                    public override string Code => "spending.other-code";
                }
                """)],
            [ProjectFile(), Rule(), Result(outcome: "success", code: "spending.receipt-missing")],
            assemblyName: "Shop.Domain");

    /// <summary>LS5005 does not run outside Domain.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "LS5005 does not run outside Domain")]
    public Task DoesNotRunOutsideDomain() =>
        Verify.AnalyzerWithRecordsAsync<RuntimeValueAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed partial record ExpenseSubmittedEvent : IDomainEvent
                {
                    public const string Classification = "event/expense.placed";
                }
                """)],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Application");
}
