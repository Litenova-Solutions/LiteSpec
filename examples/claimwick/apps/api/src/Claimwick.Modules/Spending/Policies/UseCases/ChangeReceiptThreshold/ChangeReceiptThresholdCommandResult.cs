namespace Claimwick.Spending.Policies.UseCases.ChangeReceiptThreshold;

public sealed record ChangeReceiptThresholdCommandResult(decimal Threshold, string Currency);
