using LiteSpec.Analyzers.Records;
using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using LiteSpec.Model;
using Microsoft.CodeAnalysis.CSharp;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests severity: blocking findings, live departures and editorconfig.</summary>
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

    /// <summary>A blocking finding reports an error.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A blocking finding reports an error")]
    public Task BlockingFindingReportsError() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", HandlerWithAttributeMark.Replace("{0}", "Error"))],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    /// <summary>An editorconfig lowering turns the error into a warning.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An editorconfig lowering turns the error into a warning")]
    public Task EditorconfigLoweringReportsWarning() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", HandlerWithAttributeMark.Replace("{0}", "Warning"))],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application",
            editorconfig: "dotnet_diagnostic.LS5004.severity = warning");

    /// <summary>A departure covering the file stays silent.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact(DisplayName = "A departure covering the file stays silent")]
    public Task DepartureCoveringFileStaysSilent() =>
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
            [ProjectFile(), UseCase(), Departure(["mark-missing"], paths: ["Source.cs"])],
            assemblyName: "Shop.Application");

    /// <summary>A lasting departure, with no expiry date, covers the file and stays silent.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact(DisplayName = "A lasting departure covering the file stays silent")]
    public Task LastingDepartureCoveringFileStaysSilent() =>
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
            [ProjectFile(), UseCase(), Departure(["mark-missing"], paths: ["Source.cs"], expiresOn: null)],
            assemblyName: "Shop.Application");

    /// <summary>A departure covering the subject stays silent.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact(DisplayName = "A departure covering the subject stays silent")]
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
            [ProjectFile(), UseCase(), Departure(["mark-style-invalid"], subjects: ["use-case/spending.submit-expense"])],
            assemblyName: "Shop.Application");

    /// <summary>An expired departure covers nothing.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact(DisplayName = "An expired departure covers nothing")]
    public Task ExpiredDepartureCoversNothing() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", HandlerWithAttributeMark.Replace("{0}", "Error"))],
            [ProjectFile(), UseCase(), Departure(["mark-style-invalid"], subjects: ["use-case/spending.submit-expense"], expiresOn: "2000-01-01")],
            assemblyName: "Shop.Application");

    /// <summary>The traceability context is shared per compilation.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "The traceability context is shared per compilation")]
    public void ContextIsSharedPerCompilation()
    {
        var store = StoreWith([ProjectFile(), UseCase()]);
        var compilation = CSharpCompilation.Create("Shop.Application");

        var first = TraceabilityContext.GetOrCreate(compilation, BackendLayout.LayerProjects, null, store);
        var second = TraceabilityContext.GetOrCreate(compilation, BackendLayout.LayerProjects, null, store);

        Assert.Same(first, second);
        Assert.True(TraceabilityContext.TryGet(compilation, out var cached));
        Assert.Same(first, cached);
    }

    /// <summary>The hoisted covered sets match the store.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "The hoisted covered sets match the store")]
    public void HoistedCoveredSetsMatchStore()
    {
        var store = StoreWith([ProjectFile(), UseCase(), Departure(["mark-missing"], paths: ["Source.cs"], id: "departure/marks-deferred"), Departure(["mark-style-invalid"], subjects: ["use-case/spending.submit-expense"], id: "departure/marks-styled"), Departure(["mark-missing"], paths: ["Old.cs"], id: "departure/old-marks", expiresOn: "2000-01-01")]);
        var compilation = CSharpCompilation.Create("Shop.Application");
        var context = TraceabilityContext.GetOrCreate(compilation, BackendLayout.LayerProjects, null, store);

        var byFile = new Finding("mark-missing", "Missing.", "Source.cs", 1, null);
        var byGap = new Finding("mark-style-invalid", "Mixed.", "Other.cs", 1, "use-case/spending.submit-expense");
        var open = new Finding("mark-missing", "Missing.", "Other.cs", 1, "use-case/spending.submit-expense");
        var expired = new Finding("mark-missing", "Missing.", "Old.cs", 1, null);

        Assert.True(context.IsKnown(byFile));
        Assert.True(context.IsKnown(byGap));
        Assert.False(context.IsKnown(open));
        Assert.False(context.IsKnown(expired));
        Assert.Equal(store.IsKnown(byFile), context.IsKnown(byFile));
        Assert.Equal(store.IsKnown(byGap), context.IsKnown(byGap));
        Assert.Equal(store.IsKnown(open), context.IsKnown(open));
        Assert.Equal(store.IsKnown(expired), context.IsKnown(expired));
    }

    private static CompilationRecords StoreWith(IReadOnlyList<(string Path, string Source)> files)
    {
        var records = new List<RecordFile>();

        foreach (var file in files)
        {
            records.Add(new RecordFile(file.Path, file.Source));
        }

        var store = CompilationRecords.Load(records);

        Assert.NotNull(store);

        return store!;
    }
}
