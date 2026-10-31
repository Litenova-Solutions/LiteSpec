using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the generated event Classification.</summary>
public sealed class EventValueTests
{
    /// <summary>A partial event with an event mark takes its classification from the record.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A partial event with an event mark takes its classification from the record")]
    public void PartialEventTakesItsClassification()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed partial record ExpenseSubmittedEvent(string ExpenseId) : IDomainEvent;
                """),
            RecordPages.Files("xml", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecEvent.Shop.Orders.ExpenseSubmittedEvent.g.cs", out var part), "The event part is missing.");
        Assert.Contains("partial record ExpenseSubmittedEvent", part, StringComparison.Ordinal);
        Assert.Contains("public const string Classification = \"event/expense.submitted\";", part, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A non-partial event takes no generated classification.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A non-partial event takes no generated classification")]
    public void NonPartialEventTakesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed record ExpenseSubmittedEvent(string ExpenseId) : IDomainEvent;
                """),
            RecordPages.Files("xml", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.DoesNotContain(run.Sources.Keys, key => key.StartsWith("LiteSpecEvent.", StringComparison.Ordinal));
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A hand-written classification takes no generated value.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A hand-written classification takes no generated value")]
    public void HandWrittenClassificationTakesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed partial record ExpenseSubmittedEvent(string ExpenseId) : IDomainEvent
                {
                    public const string Classification = "event/expense.submitted";
                }
                """),
            RecordPages.Files("xml", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.DoesNotContain(run.Sources.Keys, key => key.StartsWith("LiteSpecEvent.", StringComparison.Ordinal));
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A mark to an unknown event takes no generated value.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A mark to an unknown event takes no generated value")]
    public void UnknownEventTakesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public interface IDomainEvent;

                /// <implements>event/expense.unknown</implements>
                public sealed partial record ExpenseUnknownEvent(string ExpenseId) : IDomainEvent;
                """),
            RecordPages.Files("xml", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.DoesNotContain(run.Sources.Keys, key => key.StartsWith("LiteSpecEvent.", StringComparison.Ordinal));
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>No project file generates no event value.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "No project file generates no event value")]
    public void NoProjectFileGeneratesNoEventValue()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed partial record ExpenseSubmittedEvent(string ExpenseId) : IDomainEvent;
                """),
            [("/project/docs/behavior/events.md", RecordPages.EventPage("event/expense.submitted"))]);

        Assert.Empty(run.Sources);
    }

    /// <summary>A partial event nested in a non-partial type takes no generated value.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A partial event nested in a non-partial type takes no generated value")]
    public void NestedInNonPartialTakesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public interface IDomainEvent;

                public sealed class ExpenseEvents
                {
                    /// <implements>event/expense.submitted</implements>
                    public sealed partial record ExpenseSubmittedEvent(string ExpenseId) : IDomainEvent;
                }
                """),
            RecordPages.Files("xml", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.DoesNotContain(run.Sources.Keys, key => key.StartsWith("LiteSpecEvent.", StringComparison.Ordinal));
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A partial event nested in a partial type takes its classification.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A partial event nested in a partial type takes its classification")]
    public void NestedInPartialTakesItsClassification()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public interface IDomainEvent;

                public sealed partial class ExpenseEvents
                {
                    /// <implements>event/expense.submitted</implements>
                    public sealed partial record ExpenseSubmittedEvent(string ExpenseId) : IDomainEvent;
                }
                """),
            RecordPages.Files("xml", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.Contains(run.Sources.Keys, key => key.StartsWith("LiteSpecEvent.", StringComparison.Ordinal));
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }
}
