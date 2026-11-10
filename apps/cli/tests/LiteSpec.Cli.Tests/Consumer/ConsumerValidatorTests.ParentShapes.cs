using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Consumer;

/// <summary>Consumer validator cases for the allowed shapes of records on a parent page.</summary>
public sealed partial class ConsumerValidatorTests
{
    /// <summary>Journey carrying its steps and goal serves.</summary>
    /// <proves>litespec/core/intent#order-each-journey-toward-one-goal</proves>
    [Fact(DisplayName = "journey carrying its steps and goal serves")]
    public void JourneyCarryingItsStepsAndGoalServes()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/product/journeys/submit-and-reimburse.yaml",
            """{"type":"journey","id":"journey/submit-and-reimburse","title":"Submit and Reimburse","actor":"actor/customer","serves":["goal/faster-delivery"],"summary":"A customer submits a claim and is reimbursed.","finishedWhen":"The customer holds the reimbursement.","betweenSteps":"The shop hands the claim to payment.","steps":[{"id":"s1","statement":"The customer submits the order.","actor":"actor/customer"}],"cites":["requirement/pricing.flat-fee"]}""");
        TestRecords.Explain(
            folder.Path,
            ".litespec/product/journeys/submit-and-reimburse.md",
            "# Submit and Reimburse\n\n## Example\n\nAlex submits a claim on Monday and is reimbursed by Friday.\n");
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Journey carrying a field its type does not declare.</summary>
    /// <proves>litespec/core/intent#order-each-journey-toward-one-goal</proves>
    [Fact(DisplayName = "journey carrying a field its type does not declare")]
    public void JourneyCarryingAnUndeclaredField()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/product/journeys/submit-and-reimburse.yaml",
            """{"type":"journey","id":"journey/submit-and-reimburse","title":"Submit and Reimburse","actor":"actor/customer","serves":["goal/faster-delivery"],"destination":"The customer is reimbursed.","summary":"A customer submits a claim and is reimbursed.","finishedWhen":"The customer holds the reimbursement.","betweenSteps":"The shop hands the claim to payment.","steps":[{"id":"s1","statement":"The customer submits the order.","actor":"actor/customer"}],"cites":["requirement/pricing.flat-fee"]}""");
        AssertProblem(RunConsumer(folder.Path), "unknown property 'destination'");
    }

    /// <summary>Feature carrying its offering and advances.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-offering</proves>
    [Fact(DisplayName = "feature carrying its offering and advances")]
    public void FeatureCarryingItsOfferingAndAdvances()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/product/features/approval-reminders.yaml",
            """{"type":"feature","id":"feature/approval-reminders","title":"Approval Reminders","offering":"Managers get a reminder for claims waiting past three days.","solves":["problem/slow-approval"],"advances":["goal/faster-delivery"]}""");
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Feature carrying a field its type does not declare.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-offering</proves>
    [Fact(DisplayName = "feature carrying a field its type does not declare")]
    public void FeatureCarryingAnUndeclaredField()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/product/features/approval-reminders.yaml",
            """{"type":"feature","id":"feature/approval-reminders","title":"Approval Reminders","offering":"Managers get a reminder for claims waiting past three days.","state":"shipped","solves":["problem/slow-approval"],"advances":["goal/faster-delivery"]}""");
        AssertProblem(RunConsumer(folder.Path), "unknown property 'state'");
    }

    /// <summary>Scenario ending in a derived generic refusal.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact(DisplayName = "scenario ending in a derived generic refusal")]
    public void ScenarioEndingInADerivedGenericRefusal()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, ".litespec/behavior/modules/orders/cancel-order.yaml", meta => ScenarioResult(meta, "result/cancel-order.forbidden"));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Scenario ending in a module-prefixed derived refusal.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact(DisplayName = "scenario ending in a module-prefixed derived refusal")]
    public void ScenarioEndingInAModulePrefixedDerivedRefusal()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, ".litespec/behavior/modules/orders/cancel-order.yaml", meta => ScenarioResult(meta, "result/orders.cancel-order.forbidden"));
        AssertProblem(RunConsumer(folder.Path), "matches none of the 2 allowed shapes");
    }

    /// <summary>Appends a scenario entry ending in the result id to the use case page.</summary>
    /// <param name="meta">The use-case page fields to append the scenario to.</param>
    /// <param name="result">The result id the appended scenario ends in.</param>
    static void ScenarioResult(JsonObject meta, string result)
    {
        var entry = (JsonObject)ConsumerFixture.Meta("""{"type":"scenario","id":"scenario/cancel-order.canceling-after-shipment-is-refused","title":"Canceling After Shipment Is Refused","given":"The shipment already left the shop.","when":"The customer asks to cancel the claim.","then":"The cancel is refused.","result":"result/cancel-order.forbidden"}""");
        entry["result"] = result;
        ((JsonArray)meta["records"]!).Add(entry);
    }
}
