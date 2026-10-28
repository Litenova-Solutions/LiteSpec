using System;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests for the shared raised-event extraction every host calls.</summary>
public sealed class RaisedEventsTests
{
    [Fact(DisplayName = "A direct RaiseDomainEvent of a constructed event returns its simple name")]
    public void DirectRaiseReturnsSimpleName()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit()
                {
                    RaiseDomainEvent(new ExpenseSubmittedEvent());
                }
            }
            """);

        Assert.Equal(new[] { "ExpenseSubmittedEvent" }, RaisedEvents.Of(method, owner));
    }

    [Fact(DisplayName = "A raise through one private helper returns its simple name")]
    public void HelperRaiseReturnsSimpleName()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit() => ApplySubmission();

                private void ApplySubmission()
                {
                    RaiseDomainEvent(new ExpenseSubmittedEvent());
                }
            }
            """);

        Assert.Equal(new[] { "ExpenseSubmittedEvent" }, RaisedEvents.Of(method, owner));
    }

    [Fact(DisplayName = "A call through any other Raise name raises nothing")]
    public void OtherRaiseNamesAreIgnored()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit()
                {
                    RaiseSomething(new ExpenseSubmittedEvent());
                }
            }
            """);

        Assert.Empty(RaisedEvents.Of(method, owner));
    }

    [Fact(DisplayName = "A RaiseDomainEvent without a constructed event raises nothing")]
    public void RaiseWithoutCreationIsIgnored()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit(IDomainEvent fact)
                {
                    RaiseDomainEvent(fact);
                }
            }
            """);

        Assert.Empty(RaisedEvents.Of(method, owner));
    }

    [Fact(DisplayName = "A raise through a public helper is not followed")]
    public void NonPrivateHelperIsIgnored()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit() => ApplySubmission();

                public void ApplySubmission()
                {
                    RaiseDomainEvent(new ExpenseSubmittedEvent());
                }
            }
            """);

        Assert.Empty(RaisedEvents.Of(method, owner));
    }

    [Fact(DisplayName = "A raise through a second helper is not followed")]
    public void SecondLevelHelperIsIgnored()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit() => ApplySubmission();

                private void ApplySubmission() => ApplyDeeper();

                private void ApplyDeeper()
                {
                    RaiseDomainEvent(new ExpenseSubmittedEvent());
                }
            }
            """);

        Assert.Empty(RaisedEvents.Of(method, owner));
    }

    [Fact(DisplayName = "Qualified and generic event names simplify")]
    public void QualifiedAndGenericNamesSimplify()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit()
                {
                    RaiseDomainEvent(new Spending.ExpenseSubmittedEvent());
                    RaiseDomainEvent(new ExpenseWrapper<Money>());
                }
            }
            """);

        Assert.Equal(
            new[] { "ExpenseSubmittedEvent", "ExpenseWrapper" },
            RaisedEvents.Of(method, owner));
    }

    [Fact(DisplayName = "A twice-raised event returns once")]
    public void DuplicateRaisesReturnOnce()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit()
                {
                    RaiseDomainEvent(new ExpenseSubmittedEvent());
                    RaiseDomainEvent(new ExpenseSubmittedEvent());
                }
            }
            """);

        Assert.Equal(new[] { "ExpenseSubmittedEvent" }, RaisedEvents.Of(method, owner));
    }

    [Fact(DisplayName = "Of rejects a null method")]
    public void OfRejectsNullMethod()
    {
        var (_, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit()
                {
                }
            }
            """);

        Assert.Throws<ArgumentNullException>(() => RaisedEvents.Of(null!, owner));
    }

    [Fact(DisplayName = "Of rejects a null owner")]
    public void OfRejectsNullOwner()
    {
        var (method, _) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit()
                {
                }
            }
            """);

        Assert.Throws<ArgumentNullException>(() => RaisedEvents.Of(method, null!));
    }

    private static (MethodDeclarationSyntax Method, TypeDeclarationSyntax Owner) MethodOf(string code)
    {
        var tree = TestTrees.Parse(code);
        var method = TestTrees.First<MethodDeclarationSyntax>(tree);

        return (method, (TypeDeclarationSyntax)method.Parent!);
    }
}
