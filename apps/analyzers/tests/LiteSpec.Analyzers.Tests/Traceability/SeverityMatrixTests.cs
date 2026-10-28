using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests the severity matrix: record status, project phase, gaps, deviations and editorconfig.</summary>
public sealed class SeverityMatrixTests
{
    private const string HandlerWithAttributeMark = """
        using System;

        namespace Claimwick.Spending;

        public interface ICommandHandler<TCommand, TResult>;

        public sealed class SubmitExpenseCommand;

        public sealed class SubmitExpenseCommandResult;

        internal sealed class ImplementsAttribute : Attribute
        {
            public ImplementsAttribute(string id) { }
        }

        [Implements("{|LS5004:{0}:use-case/spending.submit-expense|}")]
        internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
        {
        }
        """;

    [Fact(DisplayName = "An approved subject reports an error")]
    public Task ApprovedSubjectReportsError() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", HandlerWithAttributeMark.Replace("{0}", "Error"))],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "An editorconfig lowering turns an approved subject into a warning")]
    public Task EditorconfigLoweringReportsWarning() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", HandlerWithAttributeMark.Replace("{0}", "Warning"))],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application",
            editorconfig: "dotnet_diagnostic.LS5004.severity = warning");

    [Fact(DisplayName = "A draft subject reports a warning")]
    public Task DraftSubjectReportsWarning() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", HandlerWithAttributeMark.Replace("{0}", "Warning"))],
            [ProjectFile(), UseCase(status: "draft")],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "An editorconfig entry overrides a draft warning back to an error")]
    public Task EditorconfigOverridesDraftWarning() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", HandlerWithAttributeMark.Replace("{0}", "Error"))],
            [ProjectFile(), UseCase(status: "draft")],
            assemblyName: "Shop.Application",
            editorconfig: "dotnet_diagnostic.LS5004.severity = error");

    [Fact(DisplayName = "A retired subject stays silent")]
    public Task RetiredSubjectStaysSilent() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
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
            [ProjectFile(), UseCase(status: "retired")],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "A frozen phase turns a missing test mark into a warning")]
    public Task FrozenPhaseOnTestReportsWarning() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    [Fact]
                    public void {|LS5001:Warning:Submitting_with_a_receipt_succeeds|}()
                    {
                    }
                }
                """)],
            [ProjectFile(phase: "frozen"), Scenario()],
            assemblyName: "Shop.Domain.Tests");

    [Fact(DisplayName = "A working phase keeps a missing test mark an error")]
    public Task WorkingPhaseOnTestReportsError() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    [Fact]
                    public void {|LS5001:Error:Submitting_with_a_receipt_succeeds|}()
                    {
                    }
                }
                """)],
            [ProjectFile(), Scenario()],
            assemblyName: "Shop.Domain.Tests");

    [Fact(DisplayName = "A deviation covering the file stays silent")]
    public Task DeviationCoveringFileStaysSilent() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase(), Deviation(["Source.cs"])],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "A deviation covering the subject stays silent")]
    public Task DeviationCoveringSubjectStaysSilent() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
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
            [ProjectFile(), UseCase(), Deviation([], ["use-case/spending.submit-expense"])],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "An open known gap covering the finding stays silent")]
    public Task KnownGapCoveringFindingStaysSilent() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
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
            [ProjectFile(), UseCase(), KnownGap("mark-style-mixed", ["use-case/spending.submit-expense"])],
            assemblyName: "Shop.Application");
}

