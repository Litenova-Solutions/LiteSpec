using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the generated assembly registry.</summary>
public sealed class RegistryTests
{
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "The registry maps marked types to records and use cases to grants")]
    public void RegistryMapsTypesAndGrants()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Spending;

                public interface IDomainEvent;

                public interface ICommandHandler<TCommand, TResult>;

                /// <implements>event/expense.submitted</implements>
                public sealed partial record ExpenseSubmittedEvent(string ExpenseId) : IDomainEvent;

                /// <implements>use-case/spending.submit-expense</implements>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<object, object>
                {
                }
                """),
            RecordPages.Files(
                "xml",
                ("events.md", RecordPages.EventPage("event/expense.submitted")),
                ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", "permission/submit-expense"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("[\"Shop.Spending.ExpenseSubmittedEvent\"] = new string[] { \"event/expense.submitted\" },", registry, StringComparison.Ordinal);
        Assert.Contains("[\"Shop.Spending.SubmitExpenseCommandHandler\"] = new string[] { \"use-case/spending.submit-expense\" },", registry, StringComparison.Ordinal);
        Assert.Contains("[\"use-case/spending.submit-expense\"] = \"permission/submit-expense\",", registry, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "The registry leaves unmarked types out")]
    public void RegistryLeavesUnmarkedTypesOut()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Spending;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed partial record ExpenseSubmittedEvent(string ExpenseId) : IDomainEvent;

                internal sealed class UnmarkedHelper
                {
                }
                """),
            RecordPages.Files("xml", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.DoesNotContain("UnmarkedHelper", registry, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A use case with no known record takes no grant entry")]
    public void UnknownUseCaseTakesNoGrant()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                /// <implements>use-case/spending.unknown</implements>
                internal sealed class UnknownCommandHandler : ICommandHandler<object, object>
                {
                }
                """),
            RecordPages.Files(
                "xml",
                ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", "permission/submit-expense"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("[\"Shop.Spending.UnknownCommandHandler\"] = new string[] { \"use-case/spending.unknown\" },", registry, StringComparison.Ordinal);
        Assert.DoesNotContain("use-case/spending.unknown\"] = ", registry, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "The registry is empty with no marked types")]
    public void RegistryIsEmptyWithNoMarkedTypes()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("new System.Collections.Generic.Dictionary<string, string[]>();", registry, StringComparison.Ordinal);
        Assert.Contains("new System.Collections.Generic.Dictionary<string, string>();", registry, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "The registry lists no follows targets")]
    public void RegistryListsNoFollowsTargets()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                /// <implements>use-case/spending.submit-expense</implements>
                /// <follows>decision/0002-store-expenses-as-documents</follows>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<object, object>
                {
                }

                /// <follows>decision/0002-store-expenses-as-documents</follows>
                internal sealed class CommitStep
                {
                    public void Commit()
                    {
                    }
                }
                """),
            RecordPages.Files(
                "xml",
                ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", null)),
                ("0002-store-expenses-as-documents.md", RecordPages.DecisionPage("decision/0002-store-expenses-as-documents"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("[\"Shop.Spending.SubmitExpenseCommandHandler\"] = new string[] { \"use-case/spending.submit-expense\" },", registry, StringComparison.Ordinal);
        Assert.DoesNotContain("decision/0002-store-expenses-as-documents", registry, StringComparison.Ordinal);
        Assert.DoesNotContain("CommitStep", registry, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A project file with no record pages writes no registry")]
    public void NoRecordPagesWritesNoRegistry()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml"));

        Assert.Empty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
    }
}
