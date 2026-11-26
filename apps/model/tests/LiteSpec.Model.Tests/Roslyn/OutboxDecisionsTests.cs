using System;
using LiteSpec.Model.Roslyn;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests for the shared outbox-decision extraction the CLI reader calls.</summary>
public sealed class OutboxDecisionsTests
{
    /// <summary>An IgnoreDomainEvent call on a receiver returns the event as ignored.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact(DisplayName = "An IgnoreDomainEvent call on a receiver returns the event as ignored")]
    public void IgnoreCallReturnsIgnored()
    {
        var tree = TestTrees.Parse("""
            public static class OutboxComposition
            {
                public static void Register(OutboxBuilder outbox)
                {
                    outbox.IgnoreDomainEvent<ExpenseApprovedEvent>();
                }
            }
            """);

        Assert.Equal(
            [new OutboxDecision("ExpenseApprovedEvent", true, 5)],
            OutboxDecisions.Of(tree));
    }

    /// <summary>A TranslateDomainEvent call returns the event as translated.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact(DisplayName = "A TranslateDomainEvent call returns the event as translated")]
    public void TranslateCallReturnsTranslated()
    {
        var tree = TestTrees.Parse("""
            public static class OutboxComposition
            {
                public static void Register(OutboxBuilder outbox)
                {
                    outbox.TranslateDomainEvent<ExpenseSubmittedEvent>(translate => translate.To<ExpenseSubmittedModuleEvent>());
                }
            }
            """);

        Assert.Equal(
            [new OutboxDecision("ExpenseSubmittedEvent", false, 5)],
            OutboxDecisions.Of(tree));
    }

    /// <summary>A bare decision call with no receiver still decides.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact(DisplayName = "A bare decision call with no receiver still decides")]
    public void BareCallDecides()
    {
        var tree = TestTrees.Parse("""
            public static class OutboxComposition
            {
                public static void Register()
                {
                    IgnoreDomainEvent<ExpenseRefusedEvent>();
                }
            }
            """);

        Assert.Equal(
            [new OutboxDecision("ExpenseRefusedEvent", true, 5)],
            OutboxDecisions.Of(tree));
    }

    /// <summary>Qualified and generic event names simplify.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact(DisplayName = "Qualified and generic event names simplify")]
    public void QualifiedAndGenericNamesSimplify()
    {
        var tree = TestTrees.Parse("""
            public static class OutboxComposition
            {
                public static void Register(OutboxBuilder outbox)
                {
                    outbox.IgnoreDomainEvent<Spending.ExpenseApprovedEvent>();
                    outbox.TranslateDomainEvent<EventWrapper<Money>>();
                }
            }
            """);

        Assert.Equal(
            [
                new OutboxDecision("ExpenseApprovedEvent", true, 5),
                new OutboxDecision("EventWrapper", false, 6),
            ],
            OutboxDecisions.Of(tree));
    }

    /// <summary>A call through any other staging name decides nothing.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact(DisplayName = "A call through any other staging name decides nothing")]
    public void OtherStagingNamesAreIgnored()
    {
        var tree = TestTrees.Parse("""
            public static class OutboxComposition
            {
                public static void Register(OutboxBuilder outbox)
                {
                    outbox.StageCollectedEvents();
                    outbox.StageDomainEvent<ExpenseApprovedEvent>();
                }
            }
            """);

        Assert.Empty(OutboxDecisions.Of(tree));
    }

    /// <summary>A decision call without a type argument decides nothing.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact(DisplayName = "A decision call without a type argument decides nothing")]
    public void DecisionWithoutTypeArgumentIsIgnored()
    {
        var tree = TestTrees.Parse("""
            public static class OutboxComposition
            {
                public static void Register(OutboxBuilder outbox)
                {
                    outbox.IgnoreDomainEvent();
                }
            }
            """);

        Assert.Empty(OutboxDecisions.Of(tree));
    }

    /// <summary>Of rejects a null tree.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact(DisplayName = "Of rejects a null tree")]
    public void OfRejectsNullTree()
    {
        Assert.Throws<ArgumentNullException>(() => OutboxDecisions.Of(null!));
    }
}
