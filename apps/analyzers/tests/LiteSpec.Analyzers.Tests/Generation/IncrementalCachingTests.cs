using LiteSpec.Analyzers.Generation;
using LiteSpec.Analyzers.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the generator's incremental caching.</summary>
public sealed class IncrementalCachingTests
{
    /// <summary>An edit to an unrelated file does not rerun the record-reading step.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "An edit to an unrelated file does not rerun the record-reading step")]
    public void UnrelatedEditDoesNotRerunRecordReading()
    {
        var compilation = GeneratorVerify.CreateCompilation(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed partial record ExpenseSubmittedEvent(string ExpenseId) : IDomainEvent;
                """),
            "Shop.Domain");
        var driver = GeneratorVerify.CreateDriver(
            RecordPages.Files("xml", ("events.md", RecordPages.EventPage("event/expense.submitted"))), null);
        var first = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var firstResult = first.GetRunResult();
        var second = first.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            "namespace Shop.Orders; internal sealed class Unrelated;",
            new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse),
            "Unrelated.cs", cancellationToken: TestContext.Current.CancellationToken)), TestContext.Current.CancellationToken);
        var secondResult = second.GetRunResult();

        Assert.True(
            secondResult.Results.Single().TrackedSteps.TryGetValue(LiteSpecGenerator.RecordsStepName, out var steps),
            "The record-reading step is not tracked.");
        Assert.NotEmpty(steps);
        Assert.All(
            steps.SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
        Assert.Equal(
            SourceOf(firstResult, "LiteSpecRegistry.g.cs"),
            SourceOf(secondResult, "LiteSpecRegistry.g.cs"));
    }

    private static string SourceOf(GeneratorDriverRunResult result, string hint) =>
        result.Results[0].GeneratedSources.First(source => source.HintName == hint).SourceText.ToString();
}
