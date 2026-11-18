using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests for LS5004, the mark style.</summary>
public sealed class MarkStyleTests
{
    /// <summary>LS5004 reports an attribute mark when the project declares xml.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "LS5004 reports an attribute mark when the project declares xml")]
    public Task ReportsAnAttributeMarkUnderXml() =>
        Verify.AnalyzerWithRecordsAsync<MarkStyleAnalyzer>(
            [("Shop.Spending/Application/Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                internal sealed class ImplementsAttribute : Attribute
                {
                    public ImplementsAttribute(string id) { }
                }

                [Implements("{|LS5004:use-case/spending.submit-expense|}")]
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase()]);

    /// <summary>LS5004 reports a documentation tag when the project declares attribute.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "LS5004 reports a documentation tag when the project declares attribute")]
    public Task ReportsADocumentationTagUnderAttribute() =>
        Verify.AnalyzerWithRecordsAsync<MarkStyleAnalyzer>(
            [("Shop.Spending/Application/Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <implements>{|LS5004:use-case/spending.submit-expense|}</implements>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(marks: "attribute"), UseCase()]);

    /// <summary>LS5004 accepts a mark in the declared xml style.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "LS5004 accepts a mark in the declared xml style")]
    public Task AcceptsAMarkInXmlStyle() =>
        Verify.AnalyzerWithRecordsAsync<MarkStyleAnalyzer>(
            [("Shop.Spending/Application/Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <implements>use-case/spending.submit-expense</implements>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase()]);

    /// <summary>LS5004 accepts a mark in the declared attribute style.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "LS5004 accepts a mark in the declared attribute style")]
    public Task AcceptsAMarkInAttributeStyle() =>
        Verify.AnalyzerWithRecordsAsync<MarkStyleAnalyzer>(
            [("Shop.Spending/Application/Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                internal sealed class ImplementsAttribute : Attribute
                {
                    public ImplementsAttribute(string id) { }
                }

                [Implements("use-case/spending.submit-expense")]
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(marks: "attribute"), UseCase()]);
}
