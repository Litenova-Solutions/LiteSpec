using LiteSpec.Analyzers.Layers;
using LiteSpec.Analyzers.Records;
using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using LiteSpec.Model;
using Microsoft.CodeAnalysis.CSharp;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests the severity matrix: record status, exceptions and editorconfig.</summary>
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

    /// <proves>litespec/core/model#hold-each-finding-to-its-own-status</proves>
    [Fact(DisplayName = "An approved subject reports an error")]
    public Task ApprovedSubjectReportsError() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", HandlerWithAttributeMark.Replace("{0}", "Error"))],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact(DisplayName = "An editorconfig lowering turns an approved subject into a warning")]
    public Task EditorconfigLoweringReportsWarning() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", HandlerWithAttributeMark.Replace("{0}", "Warning"))],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application",
            editorconfig: "dotnet_diagnostic.LS5004.severity = warning");

    /// <proves>litespec/core/model#hold-each-finding-to-its-own-status</proves>
    [Fact(DisplayName = "A draft subject reports a warning")]
    public Task DraftSubjectReportsWarning() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", HandlerWithAttributeMark.Replace("{0}", "Warning"))],
            [ProjectFile(), UseCase(status: "draft")],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/model#record-a-project-wide-lowering-as-an-exception</proves>
    [Fact(DisplayName = "An editorconfig entry overrides a draft warning back to an error")]
    public Task EditorconfigOverridesDraftWarning() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", HandlerWithAttributeMark.Replace("{0}", "Error"))],
            [ProjectFile(), UseCase(status: "draft")],
            assemblyName: "Shop.Application",
            editorconfig: "dotnet_diagnostic.LS5004.severity = error");

    /// <proves>litespec/core/model#hold-each-finding-to-its-own-status</proves>
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

    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact(DisplayName = "An exception covering the file stays silent")]
    public Task ExceptionCoveringFileStaysSilent() =>
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
            [ProjectFile(), UseCase(), Exception(["mark-missing"], paths: ["Source.cs"])],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact(DisplayName = "An exception covering the subject stays silent")]
    public Task ExceptionCoveringSubjectStaysSilent() =>
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
            [ProjectFile(), UseCase(), Exception(["mark-style-invalid"], subjects: ["use-case/spending.submit-expense"])],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/model#record-accepted-gaps-as-exceptions</proves>
    [Fact(DisplayName = "An open exception covering the finding stays silent")]
    public Task ExceptionCoveringFindingStaysSilent() =>
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
            [ProjectFile(), UseCase(), Exception(["mark-style-invalid"], subjects: ["use-case/spending.submit-expense"])],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "The traceability context is shared per compilation")]
    public void ContextIsSharedPerCompilation()
    {
        var store = StoreWith([ProjectFile(), UseCase()]);
        var compilation = CSharpCompilation.Create("Shop.Application");

        var first = TraceabilityContext.GetOrCreate(compilation, Layer.Application, store);
        var second = TraceabilityContext.GetOrCreate(compilation, Layer.Application, store);

        Assert.Same(first, second);
        Assert.True(TraceabilityContext.TryGet(compilation, out var cached));
        Assert.Same(first, cached);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "The hoisted known sets match the store")]
    public void HoistedKnownSetsMatchStore()
    {
        var store = StoreWith([ProjectFile(), UseCase(), Exception(["mark-missing"], paths: ["Source.cs"], id: "exception/marks-deferred"), Exception(["mark-style-invalid"], subjects: ["use-case/spending.submit-expense"], id: "exception/marks-styled")]);
        var compilation = CSharpCompilation.Create("Shop.Application");
        var context = TraceabilityContext.GetOrCreate(compilation, Layer.Application, store);

        var byFile = new Finding("mark-missing", "Missing.", "Source.cs", 1, null);
        var byGap = new Finding("mark-style-invalid", "Mixed.", "Other.cs", 1, "use-case/spending.submit-expense");
        var open = new Finding("mark-missing", "Missing.", "Other.cs", 1, "use-case/spending.submit-expense");

        Assert.True(context.IsKnown(byFile));
        Assert.True(context.IsKnown(byGap));
        Assert.False(context.IsKnown(open));
        Assert.Equal(store.IsKnown(byFile), context.IsKnown(byFile));
        Assert.Equal(store.IsKnown(byGap), context.IsKnown(byGap));
        Assert.Equal(store.IsKnown(open), context.IsKnown(open));
    }

    private static RecordStore StoreWith(IReadOnlyList<(string Path, string Source)> files)
    {
        var records = new List<RecordFile>();

        foreach (var file in files)
        {
            records.Add(new RecordFile(file.Path, file.Source));
        }

        var store = RecordStore.Load(records);

        Assert.NotNull(store);

        return store!;
    }
}

