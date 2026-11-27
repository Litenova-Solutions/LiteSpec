using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Model.Links;
using LiteSpec.Cli.Model.Tests;
using LiteSpec.Cli.Tests.Model.Checks;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the builder computes the completion stage of every requirement, use case, flow, feature and goal.</summary>
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
        Assert.Empty(completion.ParkedBy);
        Assert.Equal(0, completion.InScope);
        Assert.Equal(0, completion.Parked);
        Assert.Equal(0, completion.Total);
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
        Assert.Empty(completion.ParkedBy);
        Assert.Equal(1, completion.InScope);
        Assert.Equal(0, completion.Parked);
        Assert.Equal(1, completion.Total);
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
        Assert.Empty(completion.ParkedBy);
        Assert.Equal(1, completion.InScope);
        Assert.Equal(0, completion.Parked);
        Assert.Equal(1, completion.Total);
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
        Assert.Empty(completion.ParkedBy);
        Assert.Equal(1, completion.InScope);
        Assert.Equal(0, completion.Parked);
        Assert.Equal(1, completion.Total);
    }

    /// <summary>A flow is as far as its weakest step, and the unserved step holds it.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void FlowIsAsFarAsItsWeakestStep()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrowing-a-copy", Path.Combine(temp.Path, "flow.yaml"), """{"serves": ["goal/borrow"], "steps": [{"id": "step/borrowing-a-copy.borrow-copy", "title": "Borrow Copy"}, {"id": "step/borrowing-a-copy.return-copy", "title": "Return Copy"}]}"""),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"serves": ["step/borrowing-a-copy.borrow-copy"]}""")]);

        model.Implements.Add(new ImplementsLink("src/Lending/BorrowCopyHandler.cs", "use-case/lending.borrow-copy", "handler"));
        CompletionCalculator.Compute(model);

        var completion = model.Completion["flow/borrowing-a-copy"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Equal(["step/borrowing-a-copy.return-copy"], completion.HeldBy);
        Assert.Equal(1, completion.Counts["flow steps served"].Done);
        Assert.Equal(2, completion.Counts["flow steps served"].Of);
        Assert.Equal(1, completion.Counts["steps coded"].Done);
        Assert.Equal(0, completion.Counts["steps exercised"].Done);
        Assert.Empty(completion.ParkedBy);
        Assert.Equal(2, completion.InScope);
        Assert.Equal(0, completion.Parked);
        Assert.Equal(2, completion.Total);
    }

    /// <summary>A feature holds a parked requirement out of its stage and names it with its waiver and expiry.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void FeatureWithParkedRequirementExcludesItFromStage()
    {
        using var temp = TestCli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Feature(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", """{"basedOn": ["feature/borrowing"]}"""),
            Requirement(temp.Path, "requirement/borrowing.no-card", """{"basedOn": ["feature/borrowing"]}"""),
            TestModel.Record("waiver", "waiver/borrowing.no-card-gap", Path.Combine(temp.Path, "gap.yaml"), """{"kind": "gap", "appliesTo": ["requirement/borrowing.no-card"], "waives": ["requirement-fulfillment-missing"], "reason": "The card check ships later.", "closedBy": ["requirement/borrowing.no-card"], "exit": "The card check ships.", "expiresOn": "2099-01-01"}"""),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        model.Implements.Add(new ImplementsLink("src/Lending/BorrowCopyHandler.cs", "use-case/lending.borrow-copy", "handler"));
        CompletionCalculator.Compute(model);

        var completion = model.Completion["feature/borrowing"];

        Assert.Equal("coded", completion.Stage);
        Assert.Equal(["requirement/borrowing.max-loans"], completion.HeldBy);
        Assert.Equal(1, completion.Counts["requirements fulfilled"].Done);
        Assert.Equal(1, completion.Counts["requirements fulfilled"].Of);
        Assert.Equal(0, completion.Counts["requirements verified"].Done);
        Assert.Equal(1, completion.InScope);
        Assert.Equal(1, completion.Parked);
        Assert.Equal(2, completion.Total);

        var parked = Assert.Single(completion.ParkedBy);

        Assert.Equal("requirement/borrowing.no-card", parked.Id);
        Assert.Equal("waiver/borrowing.no-card-gap", parked.Waiver);
        Assert.Equal("2099-01-01", parked.ExpiresOn);
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
            Requirement(temp.Path, "requirement/borrowing.max-loans", """{"basedOn": ["feature/borrowing"]}"""));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["feature/borrowing"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Equal(["requirement/borrowing.max-loans"], completion.HeldBy);
        Assert.Empty(completion.ParkedBy);
        Assert.Equal(1, completion.InScope);
        Assert.Equal(0, completion.Parked);
        Assert.Equal(1, completion.Total);
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
            Requirement(temp.Path, "requirement/borrowing.max-loans", """{"basedOn": ["feature/borrowing"]}"""));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["goal/borrow"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Contains("feature/borrowing", completion.HeldBy);
        Assert.Equal(0, completion.Counts["features verified"].Done);
        Assert.Equal(1, completion.Counts["features verified"].Of);
        Assert.Empty(completion.ParkedBy);
        Assert.Equal(1, completion.InScope);
        Assert.Equal(0, completion.Parked);
        Assert.Equal(1, completion.Total);
    }

    static LiteSpec.Cli.Model.Records.Record Requirement(string root, string id, string fields) =>
        TestModel.Record("requirement", id, Path.Combine(root, id.Replace('/', '.') + ".yaml"), fields == "{}"
            ? """{"kind": "functional", "statement": "A reader borrows one copy."}"""
            : $$"""{"kind": "functional", "statement": "A reader borrows one copy.", {{fields[1..^1]}}}""");

    static LiteSpec.Cli.Model.Records.Record UseCase(string root, string id, string fields) =>
        TestModel.Record("use-case", id, Path.Combine(root, id.Replace('/', '.') + ".yaml"), fields);

    static LiteSpec.Cli.Model.Records.Record Feature(string root) =>
        TestModel.Record("feature", "feature/borrowing", Path.Combine(root, "feature.yaml"), """{"advances": ["goal/borrow"]}""");
}
