using System.Linq;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests for reading one syntax tree in a single pass.</summary>
public sealed class MarkFactsTests
{
    [Fact(DisplayName = "One pass collects every marked or markable type")]
    public void OnePassCollectsMarkedAndMarkableTypes()
    {
        var tree = TestTrees.Parse("""
            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand>;

            internal sealed class OpenExpensesQueryHandler : IQueryHandler<OpenExpensesQuery>;

            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class ExpenseFormatter;

            internal sealed class Money;
            """);

        var elements = MarkFacts.Read(tree, KindContext.Empty);

        Assert.Equal(
            new[] { "SubmitExpenseCommandHandler", "OpenExpensesQueryHandler", "ExpenseFormatter" },
            elements.Select(element => element.Name).ToArray());

        Assert.Equal(CodeKind.Handler, elements[0].Kind);
        Assert.Equal(CodeKind.Handler, elements[1].Kind);
        Assert.Null(elements[2].Kind);

        var mark = Assert.Single(elements[0].Marks);
        Assert.Equal("use-case/spending.submit-expense", mark.RecordId);
        Assert.Equal(1, mark.Line);
        Assert.Empty(elements[1].Marks);
        Assert.Single(elements[2].Marks);

        Assert.All(elements, element => Assert.True(element.IsType));
        Assert.Equal(new[] { 2, 4, 7 }, elements.Select(element => element.Line).ToArray());
        Assert.All(elements, element =>
            Assert.Equal(element.Name, tree.GetText().ToString(element.Span)));
    }

    [Fact(DisplayName = "One pass collects marked and markable methods")]
    public void OnePassCollectsMethods()
    {
        var tree = TestTrees.Parse("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                /// <implements>use-case/spending.submit-expense</implements>
                public void Submit() => RaiseDomainEvent(new ExpenseSubmittedEvent());

                public int Total() => 0;
            }

            internal sealed class SubmitExpenseTests
            {
                /// <proves>scenario/submit-expense.succeeds</proves>
                [Fact]
                public void Submits() { }
            }
            """);

        var elements = MarkFacts.Read(tree, KindContext.Empty);

        Assert.Equal(
            new[] { "Expense", "Submit", "Submits" },
            elements.Select(element => element.Name).ToArray());
        Assert.Equal(
            new CodeKind?[] { CodeKind.AggregateRoot, CodeKind.AggregateMethod, CodeKind.Test },
            elements.Select(element => element.Kind).ToArray());
        Assert.Equal(new[] { true, false, false }, elements.Select(element => element.IsType).ToArray());
        Assert.Empty(elements[0].Marks);
        Assert.Single(elements[1].Marks);
        Assert.Single(elements[2].Marks);
    }

    [Fact(DisplayName = "One pass collects a marked method of no known kind")]
    public void OnePassCollectsMarkedMethodOfNoKnownKind()
    {
        var tree = TestTrees.Parse("""
            public sealed class ExpenseFormatter
            {
                /// <implements>use-case/spending.submit-expense</implements>
                public string Format() => "";
            }
            """);

        var element = Assert.Single(MarkFacts.Read(tree, KindContext.Empty));

        Assert.Equal("Format", element.Name);
        Assert.False(element.IsType);
        Assert.Null(element.Kind);
        Assert.Equal("use-case/spending.submit-expense", Assert.Single(element.Marks).RecordId);
    }

    [Fact(DisplayName = "A tree without declarations reads no elements")]
    public void EmptyTreeReadsNone()
    {
        var tree = TestTrees.Parse("// nothing declared here");

        Assert.Empty(MarkFacts.Read(tree, KindContext.Empty));
    }
}
