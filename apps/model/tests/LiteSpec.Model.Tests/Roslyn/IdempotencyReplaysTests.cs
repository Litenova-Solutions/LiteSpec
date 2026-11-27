using System;
using LiteSpec.Model.Roslyn;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests for the shared replay extraction the replay check calls.</summary>
public sealed class IdempotencyReplaysTests
{
    /// <summary>A replayResult true argument returns true.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact(DisplayName = "A replayResult true argument returns true")]
    public void ReplayResultTrueReturnsTrue()
    {
        var root = TestTrees.Parse("""
            internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>
            {
                public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(
                    static message => "copy-id",
                    scope: "tenant",
                    replayResult: true);
            }
            """).GetRoot(TestContext.Current.CancellationToken);

        Assert.True(IdempotencyReplays.Of(root));
    }

    /// <summary>A replayResult false argument returns false.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact(DisplayName = "A replayResult false argument returns false")]
    public void ReplayResultFalseReturnsFalse()
    {
        var root = TestTrees.Parse("""
            internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>
            {
                public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(
                    static message => "copy-id",
                    scope: "tenant",
                    replayResult: false);
            }
            """).GetRoot(TestContext.Current.CancellationToken);

        Assert.False(IdempotencyReplays.Of(root));
    }

    /// <summary>A ReplayResult true initializer returns true.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact(DisplayName = "A ReplayResult true initializer returns true")]
    public void ReplayPropertyTrueReturnsTrue()
    {
        var root = TestTrees.Parse("""
            internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>
            {
                public IdempotencyDeclaration Idempotency { get; } = new IdempotencyDeclaration(static message => "copy-id") { ReplayResult = true };
            }
            """).GetRoot(TestContext.Current.CancellationToken);

        Assert.True(IdempotencyReplays.Of(root));
    }

    /// <summary>A ReplayResult false with-expression returns false.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact(DisplayName = "A ReplayResult false with-expression returns false")]
    public void ReplayPropertyWithReturnsFalse()
    {
        var root = TestTrees.Parse("""
            internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>
            {
                public IdempotencyDeclaration Idempotency => IdempotencyDeclaration.Idempotent(static message => "copy-id") with { ReplayResult = false };
            }
            """).GetRoot(TestContext.Current.CancellationToken);

        Assert.False(IdempotencyReplays.Of(root));
    }

    /// <summary>A keyed declaration without the flag replays nothing, the pipeline default.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact(DisplayName = "A keyed declaration without the flag replays nothing")]
    public void MissingFlagReturnsFalse()
    {
        var root = TestTrees.Parse("""
            internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>
            {
                public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(static message => "copy-id");
            }
            """).GetRoot(TestContext.Current.CancellationToken);

        Assert.False(IdempotencyReplays.Of(root));
    }

    /// <summary>An exemption returns null.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact(DisplayName = "An exemption returns null")]
    public void ExemptionReturnsNull()
    {
        var root = TestTrees.Parse("""
            internal sealed class BorrowCopyCommandIdempotencyDefinition : IMessageDefinition<BorrowCopyCommand>
            {
                public void Describe(IMessageDeclarations declarations) => declarations.Exempt<IdempotencyDeclaration>("recognized by state");
            }
            """).GetRoot(TestContext.Current.CancellationToken);

        Assert.Null(IdempotencyReplays.Of(root));
    }

    /// <summary>A NotIdempotent declaration returns null.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact(DisplayName = "A NotIdempotent declaration returns null")]
    public void NotIdempotentReturnsNull()
    {
        var root = TestTrees.Parse("""
            internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>
            {
                public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.NotIdempotent();
            }
            """).GetRoot(TestContext.Current.CancellationToken);

        Assert.Null(IdempotencyReplays.Of(root));
    }

    /// <summary>A non-literal flag returns null.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact(DisplayName = "A non-literal flag returns null")]
    public void NonLiteralFlagReturnsNull()
    {
        var root = TestTrees.Parse("""
            internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>
            {
                public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(static message => "copy-id", replayResult: ShouldReplay);
            }
            """).GetRoot(TestContext.Current.CancellationToken);

        Assert.Null(IdempotencyReplays.Of(root));
    }

    /// <summary>A bare Idempotent call returns null.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact(DisplayName = "A bare Idempotent call returns null")]
    public void BareCallReturnsNull()
    {
        var root = TestTrees.Parse("""
            internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>
            {
                public IdempotencyDeclaration Idempotency { get; } = Idempotent(static message => "copy-id", replayResult: true);
            }
            """).GetRoot(TestContext.Current.CancellationToken);

        Assert.Null(IdempotencyReplays.Of(root));
    }

    /// <summary>The first explicit position in source order wins.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact(DisplayName = "The first explicit position in source order wins")]
    public void FirstPositionWins()
    {
        var root = TestTrees.Parse("""
            internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>
            {
                public IdempotencyDeclaration First { get; } = IdempotencyDeclaration.Idempotent(static message => "copy-id", replayResult: false);
                public IdempotencyDeclaration Second { get; } = IdempotencyDeclaration.Idempotent(static message => "copy-id", replayResult: true);
            }
            """).GetRoot(TestContext.Current.CancellationToken);

        Assert.False(IdempotencyReplays.Of(root));
    }

    /// <summary>Of rejects a null root.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact(DisplayName = "Of rejects a null root")]
    public void OfRejectsNullRoot()
    {
        Assert.Throws<ArgumentNullException>(() => IdempotencyReplays.Of(null!));
    }
}
