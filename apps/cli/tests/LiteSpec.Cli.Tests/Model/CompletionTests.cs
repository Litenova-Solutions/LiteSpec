using LiteSpec.Model;
using LiteSpec.Cli.Tests.Model.Checks;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the builder computes the completion stage of every requirement, use case, journey, feature and goal.</summary>
/// <follows>litespec/core/model#compute-completion-in-the-builder</follows>
public sealed class CompletionTests
{
    /// <summary>A requirement with no fulfiller is unfulfilled and holds itself.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void RequirementWithNoFulfillerIsUnfulfilled()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", "{}"));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["requirement/borrowing.max-loans"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Equal("unknown", completion.Proof);
        Assert.Equal(["requirement/borrowing.max-loans"], completion.HeldBy);
        Assert.Equal(0, completion.Counts["fulfillers coded"].Of);
    }

    /// <summary>A requirement whose fulfiller has no code is specified.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void RequirementWithFulfillerWithoutCodeIsSpecified()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", "{}"),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["requirement/borrowing.max-loans"];

        Assert.Equal("specified", completion.Stage);
        Assert.Equal(["use-case/lending.borrow-copy"], completion.HeldBy);
        Assert.Equal(0, completion.Counts["fulfillers coded"].Done);
        Assert.Equal(1, completion.Counts["fulfillers coded"].Of);
    }

    /// <summary>A requirement with code for every fulfiller and no test run is coded with proof unknown.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void RequirementCodedWithNoTestRunReadsProofUnknown()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", "{}"),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        model.Implements.Add(new ImplementsLink("src/Lending/BorrowCopyHandler.cs", "use-case/lending.borrow-copy", "handler"));
        CompletionCalculator.Compute(model);

        var completion = model.Completion["requirement/borrowing.max-loans"];

        Assert.Equal("coded", completion.Stage);
        Assert.Equal("unknown", completion.Proof);
        Assert.Equal(["use-case/lending.borrow-copy"], completion.HeldBy);
        Assert.Equal(1, completion.Counts["fulfillers coded"].Done);
        Assert.Equal(0, completion.Counts["fulfillers verified"].Done);
    }

    /// <summary>A requirement verified with fresh proof is verified with proof known.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void RequirementVerifiedWithTestRunReadsProofKnown()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", "{}"),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"fulfills": ["requirement/borrowing.max-loans"]}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(temp.Path, "scenario.yaml"), """{"result": "result/borrow-copy.borrowed"}"""));

        model.Proves.Add(new ProvesLink("BorrowCopyTests.Borrowing_An_Available_Copy_Succeeds", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", true, true, false));
        model.TestRun = new TestRun();
        CompletionCalculator.Compute(model);

        var completion = model.Completion["requirement/borrowing.max-loans"];

        Assert.Equal("verified", completion.Stage);
        Assert.Equal("known", completion.Proof);
        Assert.Empty(completion.HeldBy);
        Assert.Equal(1, completion.Counts["fulfillers verified"].Done);
    }

    /// <summary>A journey is as far as its weakest step, and the unserved step holds it.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void JourneyIsAsFarAsItsWeakestStep()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("journey", "journey/borrowing-a-copy", Path.Combine(temp.Path, "journey.yaml"), """{"serves": ["goal/borrow"], "steps": [{"id": "s1"}, {"id": "s2"}]}"""),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"serves": ["journey/borrowing-a-copy#s1"]}"""));

        model.Implements.Add(new ImplementsLink("src/Lending/BorrowCopyHandler.cs", "use-case/lending.borrow-copy", "handler"));
        CompletionCalculator.Compute(model);

        var completion = model.Completion["journey/borrowing-a-copy"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Equal(["journey/borrowing-a-copy#s2"], completion.HeldBy);
        Assert.Equal(1, completion.Counts["journey steps served"].Done);
        Assert.Equal(2, completion.Counts["journey steps served"].Of);
        Assert.Equal(1, completion.Counts["steps coded"].Done);
        Assert.Equal(0, completion.Counts["steps exercised"].Done);
    }

    /// <summary>A feature with an unfulfilled requirement is unfulfilled even when a gap departure names it: completion follows proof alone.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void FeatureWithUnfulfilledRequirementIgnoresItsGapDeparture()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Feature(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", """{"cites": ["feature/borrowing"]}"""),
            Requirement(temp.Path, "requirement/borrowing.no-card", """{"cites": ["feature/borrowing"]}"""),
            TestModel.Record("departure", "departure/borrowing.no-card-gap", Path.Combine(temp.Path, "gap.yaml"), """{"subject": ["requirement/borrowing.no-card"], "covers": ["requirement-fulfillment-missing"], "expiresOn": "2099-01-01"}"""),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        model.Implements.Add(new ImplementsLink("src/Lending/BorrowCopyHandler.cs", "use-case/lending.borrow-copy", "handler"));
        CompletionCalculator.Compute(model);

        var completion = model.Completion["feature/borrowing"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Equal(["requirement/borrowing.no-card"], completion.HeldBy);
        Assert.Equal(1, completion.Counts["requirements fulfilled"].Done);
        Assert.Equal(2, completion.Counts["requirements fulfilled"].Of);
        Assert.Equal(0, completion.Counts["requirements verified"].Done);
    }

    /// <summary>A feature with an unfulfilled requirement and no gap is unfulfilled, held by that requirement.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void FeatureWithUnfulfilledRequirementIsUnfulfilled()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Feature(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", """{"cites": ["feature/borrowing"]}"""));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["feature/borrowing"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Equal(["requirement/borrowing.max-loans"], completion.HeldBy);
    }

    /// <summary>A goal is as far as its weakest feature.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void GoalIsAsFarAsItsWeakestFeature()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/borrow", Path.Combine(temp.Path, "goal.yaml"), "{}"),
            Feature(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", """{"cites": ["feature/borrowing"]}"""));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["goal/borrow"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Contains("feature/borrowing", completion.HeldBy);
        Assert.Equal(0, completion.Counts["features verified"].Done);
        Assert.Equal(1, completion.Counts["features verified"].Of);
    }

    static LiteSpec.Model.Record Requirement(string root, string id, string fields) =>
        TestModel.Record("requirement", id, Path.Combine(root, id.Replace('/', '.') + ".yaml"), fields == "{}"
            ? """{"kind": "functional", "statement": "A reader borrows one copy."}"""
            : $$"""{"kind": "functional", "statement": "A reader borrows one copy.", {{fields[1..^1]}}}""");

    static LiteSpec.Model.Record UseCase(string root, string id, string fields) =>
        TestModel.Record("use-case", id, Path.Combine(root, id.Replace('/', '.') + ".yaml"), fields);

    static LiteSpec.Model.Record Feature(string root) =>
        TestModel.Record("feature", "feature/borrowing", Path.Combine(root, "feature.yaml"), """{"solves": ["problem/slow-loans"], "advances": ["goal/borrow"]}""");
}
