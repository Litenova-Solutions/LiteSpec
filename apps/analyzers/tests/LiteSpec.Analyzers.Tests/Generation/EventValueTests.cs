using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the generated event Classification.</summary>
public sealed class EventValueTests
{
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
}
