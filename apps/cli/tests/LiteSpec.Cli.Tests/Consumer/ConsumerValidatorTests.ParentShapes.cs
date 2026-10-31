using System.Text.Json.Nodes;

namespace LiteSpec.Tests.Consumer;

/// <summary>Consumer validator cases for the allowed shapes of records on a parent page.</summary>
public sealed partial class ConsumerValidatorTests
{
    /// <summary>Journey carrying its steps and goal serves.</summary>
    /// <proves>litespec/core/intent#order-each-journey-toward-one-goal</proves>
    [Fact(DisplayName = "journey carrying its steps and goal serves")]
    public void JourneyCarryingItsStepsAndGoalServes()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            "docs/product/journeys/submit-and-reimburse.md",
            ConsumerFixture.Page(
                ConsumerFixture.Meta("""{"type":"journey","id":"journey/submit-and-reimburse","title":"Submit and Reimburse","owner":"fixture","status":"approved","actor":"actor/customer","serves":["goal/faster-delivery"],"finishedWhen":"The customer holds the reimbursement.","steps":[{"id":"s1","statement":"The customer submits the order.","actor":"actor/customer"}],"cites":["requirement/pricing.flat-fee"],"approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}"""),
                "# Submit and Reimburse\n\n## Example\n\nAlex submits a claim on Monday and is reimbursed by Friday.\n\n## Between the Steps\n\nThe shop hands the claim to payment.\n"));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Journey carrying the retired goal sentence.</summary>
    /// <proves>litespec/core/intent#order-each-journey-toward-one-goal</proves>
    [Fact(DisplayName = "journey carrying the retired goal sentence")]
    public void JourneyCarryingTheRetiredGoalSentence()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            "docs/product/journeys/submit-and-reimburse.md",
            ConsumerFixture.Page(
                ConsumerFixture.Meta("""{"type":"journey","id":"journey/submit-and-reimburse","title":"Submit and Reimburse","owner":"fixture","status":"approved","actor":"actor/customer","serves":["goal/faster-delivery"],"goal":"The customer is reimbursed.","finishedWhen":"The customer holds the reimbursement.","steps":[{"id":"s1","statement":"The customer submits the order.","actor":"actor/customer"}],"cites":["requirement/pricing.flat-fee"],"approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}"""),
                "# Submit and Reimburse\n\n## Example\n\nAlex submits a claim on Monday and is reimbursed by Friday.\n\n## Between the Steps\n\nThe shop hands the claim to payment.\n"));
        AssertProblem(RunConsumer(folder.Path), "unknown property 'goal'");
    }

    /// <summary>Feature carrying its lifecycle and advances.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact(DisplayName = "feature carrying its lifecycle and advances")]
    public void FeatureCarryingItsLifecycleAndAdvances()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            "docs/product/features/approval-reminders.md",
            ConsumerFixture.Page(
                ConsumerFixture.Meta("""{"type":"feature","id":"feature/approval-reminders","title":"Approval Reminders","owner":"fixture","status":"approved","lifecycle":"shipped","solves":["problem/slow-approval"],"advances":["goal/faster-delivery"],"approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}"""),
                "# Approval Reminders\n\n## Offering\n\nManagers get a reminder for claims waiting past three days.\n"));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Feature carrying the retired state field.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact(DisplayName = "feature carrying the retired state field")]
    public void FeatureCarryingTheRetiredStateField()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            "docs/product/features/approval-reminders.md",
            ConsumerFixture.Page(
                ConsumerFixture.Meta("""{"type":"feature","id":"feature/approval-reminders","title":"Approval Reminders","owner":"fixture","status":"approved","lifecycle":"shipped","state":"shipped","solves":["problem/slow-approval"],"advances":["goal/faster-delivery"],"approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}"""),
                "# Approval Reminders\n\n## Offering\n\nManagers get a reminder for claims waiting past three days.\n"));
        AssertProblem(RunConsumer(folder.Path), "unknown property 'state'");
    }

    /// <summary>Scenario ending in a derived generic refusal.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact(DisplayName = "scenario ending in a derived generic refusal")]
    public void ScenarioEndingInADerivedGenericRefusal()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, "docs/behavior/modules/orders/cancel-order.md", meta => ScenarioResult(meta, "result/cancel-order.forbidden"));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Scenario ending in a module-prefixed derived refusal.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact(DisplayName = "scenario ending in a module-prefixed derived refusal")]
    public void ScenarioEndingInAModulePrefixedDerivedRefusal()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, "docs/behavior/modules/orders/cancel-order.md", meta => ScenarioResult(meta, "result/orders.cancel-order.forbidden"));
        AssertProblem(RunConsumer(folder.Path), "matches none of the 2 allowed shapes");
    }

    /// <summary>Appends a scenario entry ending in the result id to the use case page.</summary>
    /// <param name="meta">The use-case page frontmatter to append the scenario to.</param>
    /// <param name="result">The result id the appended scenario ends in.</param>
    static void ScenarioResult(JsonObject meta, string result)
    {
        var entry = (JsonObject)ConsumerFixture.Meta("""{"type":"scenario","id":"scenario/cancel-order.canceling-after-shipment-is-refused","title":"Canceling After Shipment Is Refused","given":"The shipment already left the shop.","when":"The customer asks to cancel the claim.","then":"The cancel is refused.","result":"result/cancel-order.forbidden"}""");
        entry["result"] = result;
        ((JsonArray)meta["records"]!).Add(entry);
    }
}
