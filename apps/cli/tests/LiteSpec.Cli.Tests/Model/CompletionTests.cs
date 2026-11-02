using LiteSpec.Model;
using LiteSpec.Tests.Model.Checks;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the builder computes the completion stage of every requirement, use case, journey, feature and goal.</summary>
/// <follows>litespec/core/model#compute-completion-in-the-builder</follows>
public sealed class CompletionTests
{
    /// <summary>A requirement with no fulfiller is unfulfilled and holds itself.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void Requirement_With_No_Fulfiller_Is_Unfulfilled()
    {
        using var temp = Cli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", "{}"));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["requirement/borrowing.max-loans"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Equal("unknown", completion.Proof);
        Assert.Equal(["requirement/borrowing.max-loans"], completion.HeldBy);
        Assert.Equal(0, completion.Counts["fulfillers implemented"].Of);
    }

    /// <summary>A requirement whose fulfiller has no code is specified.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void Requirement_With_Fulfiller_Without_Code_Is_Specified()
    {
        using var temp = Cli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", "{}"),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"status": "draft", "fulfills": ["requirement/borrowing.max-loans"]}"""));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["requirement/borrowing.max-loans"];

        Assert.Equal("specified", completion.Stage);
        Assert.Equal(["use-case/lending.borrow-copy"], completion.HeldBy);
        Assert.Equal(0, completion.Counts["fulfillers implemented"].Done);
        Assert.Equal(1, completion.Counts["fulfillers implemented"].Of);
    }

    /// <summary>A requirement with code for every fulfiller and no test run is implemented with proof unknown.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void Requirement_Implemented_With_No_Test_Run_Reads_Proof_Unknown()
    {
        using var temp = Cli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", "{}"),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"status": "reviewed", "fulfills": ["requirement/borrowing.max-loans"]}"""));

        model.Status["use-case/lending.borrow-copy"] = new UseCaseStatus(true, false);
        CompletionCalculator.Compute(model);

        var completion = model.Completion["requirement/borrowing.max-loans"];

        Assert.Equal("implemented", completion.Stage);
        Assert.Equal("unknown", completion.Proof);
        Assert.Equal(["use-case/lending.borrow-copy"], completion.HeldBy);
        Assert.Equal(1, completion.Counts["fulfillers implemented"].Done);
        Assert.Equal(0, completion.Counts["fulfillers proven"].Done);
    }

    /// <summary>A requirement proven with fresh proof is proven with proof known.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void Requirement_Proven_With_Test_Run_Reads_Proof_Known()
    {
        using var temp = Cli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", "{}"),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"status": "reviewed", "fulfills": ["requirement/borrowing.max-loans"]}"""));

        model.Status["use-case/lending.borrow-copy"] = new UseCaseStatus(true, true);
        model.RequirementStatus["requirement/borrowing.max-loans"] = new RequirementStatus(true, null);
        model.TestRun = new TestRun();
        CompletionCalculator.Compute(model);

        var completion = model.Completion["requirement/borrowing.max-loans"];

        Assert.Equal("proven", completion.Stage);
        Assert.Equal("known", completion.Proof);
        Assert.Empty(completion.HeldBy);
        Assert.Equal(1, completion.Counts["fulfillers proven"].Done);
    }

    /// <summary>A journey is as far as its weakest step, and the unserved step holds it.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void Journey_Is_As_Far_As_Its_Weakest_Step()
    {
        using var temp = Cli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("journey", "journey/borrowing-a-copy", Path.Combine(temp.Path, "journey.yaml"), """{"status": "approved", "serves": ["goal/borrow"], "steps": [{"id": "s1"}, {"id": "s2"}]}"""),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"status": "reviewed", "serves": ["journey/borrowing-a-copy#s1"]}"""));

        model.Status["use-case/lending.borrow-copy"] = new UseCaseStatus(true, false);
        CompletionCalculator.Compute(model);

        var completion = model.Completion["journey/borrowing-a-copy"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Equal(["journey/borrowing-a-copy#s2"], completion.HeldBy);
        Assert.Equal(1, completion.Counts["journey steps served"].Done);
        Assert.Equal(2, completion.Counts["journey steps served"].Of);
        Assert.Equal(1, completion.Counts["steps implemented"].Done);
        Assert.Equal(0, completion.Counts["steps exercised"].Done);
    }

    /// <summary>A feature takes the stage of its weakest requirement and names a parked requirement in heldBy without counting it.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void Feature_Parked_Requirement_Names_Itself_In_HeldBy_Without_Holding_The_Stage()
    {
        using var temp = Cli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Feature(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", """{"cites": ["feature/borrowing"]}"""),
            Requirement(temp.Path, "requirement/borrowing.no-card", """{"cites": ["feature/borrowing"]}"""),
            TestModel.Record("exception", "exception/borrowing.no-card-gap", Path.Combine(temp.Path, "gap.yaml"), """{"status": "approved", "kind": "gap", "subject": ["requirement/borrowing.no-card"], "covers": ["requirement-fulfillment-missing"], "expiresOn": "2099-01-01"}"""),
            UseCase(temp.Path, "use-case/lending.borrow-copy", """{"status": "reviewed", "fulfills": ["requirement/borrowing.max-loans"]}"""));

        model.Status["use-case/lending.borrow-copy"] = new UseCaseStatus(true, false);
        CompletionCalculator.Compute(model);

        var completion = model.Completion["feature/borrowing"];

        Assert.Equal("implemented", completion.Stage);
        Assert.Contains("requirement/borrowing.max-loans", completion.HeldBy);
        Assert.Contains("requirement/borrowing.no-card", completion.HeldBy);
        Assert.Equal(1, completion.Counts["requirements fulfilled"].Of);
        Assert.Equal(1, completion.Counts["requirements fulfilled"].Done);
        Assert.Equal(0, completion.Counts["requirements proven"].Done);
    }

    /// <summary>A feature with an unfulfilled requirement and no gap is unfulfilled, held by that requirement.</summary>
    /// <proves>litespec/core/model#compute-completion-in-the-builder</proves>
    [Fact]
    public void Feature_With_Unfulfilled_Requirement_Is_Unfulfilled()
    {
        using var temp = Cli.Temp("completion");
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
    public void Goal_Is_As_Far_As_Its_Weakest_Feature()
    {
        using var temp = Cli.Temp("completion");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/borrow", Path.Combine(temp.Path, "goal.yaml"), """{"status": "approved"}"""),
            Feature(temp.Path),
            Requirement(temp.Path, "requirement/borrowing.max-loans", """{"cites": ["feature/borrowing"]}"""));

        CompletionCalculator.Compute(model);

        var completion = model.Completion["goal/borrow"];

        Assert.Equal("unfulfilled", completion.Stage);
        Assert.Contains("feature/borrowing", completion.HeldBy);
        Assert.Equal(0, completion.Counts["features proven"].Done);
        Assert.Equal(1, completion.Counts["features proven"].Of);
    }

    /// <summary>A shipped feature below proven is advisory, and a planned feature below proven is silent.</summary>
    /// <proves>litespec/core/model#flag-a-shipped-feature-below-proven</proves>
    [Fact]
    public void Shipped_Feature_Below_Proven_Reports_Advisory()
    {
        using var temp = Cli.Temp("completion");
        var shipped = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(temp.Path, "feature.yaml"), """{"status": "approved", "lifecycle": "shipped", "solves": ["problem/slow-loans"], "advances": ["goal/borrow"]}"""),
            Requirement(temp.Path, "requirement/borrowing.max-loans", """{"cites": ["feature/borrowing"]}"""));

        var finding = Assert.Single(
            Checker.Check(shipped).Findings, found => found.Code == "feature-shipped-drift");

        Assert.Equal("A", finding.Tier);
        Assert.Equal("feature/borrowing", finding.Subject);
        Assert.Contains("unfulfilled", finding.Message, StringComparison.Ordinal);

        using var plannedTemp = Cli.Temp("completion-planned");
        var planned = TestModel.Model(
            TestModel.Config(plannedTemp.Path),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(plannedTemp.Path, "feature.yaml"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot", "solves": ["problem/slow-loans"], "advances": ["goal/borrow"]}"""),
            Requirement(plannedTemp.Path, "requirement/borrowing.max-loans", """{"cites": ["feature/borrowing"]}"""));

        Assert.DoesNotContain("feature-shipped-drift", TestModel.Codes(planned));
    }

    static LiteSpec.Model.Record Requirement(string root, string id, string fields) =>
        TestModel.Record("requirement", id, Path.Combine(root, id.Replace('/', '.') + ".yaml"), fields == "{}"
            ? """{"status": "approved", "kind": "functional", "statement": "A reader borrows one copy."}"""
            : $$"""{"status": "approved", "kind": "functional", "statement": "A reader borrows one copy.", {{fields[1..^1]}}}""");

    static LiteSpec.Model.Record UseCase(string root, string id, string fields) =>
        TestModel.Record("use-case", id, Path.Combine(root, id.Replace('/', '.') + ".yaml"), fields);

    static LiteSpec.Model.Record Feature(string root) =>
        TestModel.Record("feature", "feature/borrowing", Path.Combine(root, "feature.yaml"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot", "solves": ["problem/slow-loans"], "advances": ["goal/borrow"]}""");
}
