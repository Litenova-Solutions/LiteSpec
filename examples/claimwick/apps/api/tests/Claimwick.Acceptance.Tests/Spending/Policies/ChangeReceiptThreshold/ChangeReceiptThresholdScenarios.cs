namespace Claimwick.Acceptance.Tests.Spending.Policies.ChangeReceiptThreshold;

public sealed class ChangeReceiptThresholdScenarios
{
    /// <proves>scenario/change-receipt-threshold.setting-the-threshold-succeeds</proves>
    [Fact]
    public Task Setting_the_threshold_succeedsAsync() => Runner.RunScenarioAsync(
        when => new ChangeReceiptThresholdSteps().SetTheThresholdAsync(25m),
        then => new ChangeReceiptThresholdSteps().TheThresholdIsSetAsync());

    /// <proves>scenario/change-receipt-threshold.setting-a-negative-threshold-is-refused</proves>
    [Fact]
    public Task Setting_a_negative_threshold_is_refusedAsync() => Runner.RunScenarioAsync(
        when => new ChangeReceiptThresholdSteps().SetTheThresholdAsync(-25m),
        then => new ChangeReceiptThresholdSteps().TheThresholdIsRefusedAsNegativeAsync());
}
