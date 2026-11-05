namespace Claimwick.Spending.Policies.UseCases.ChangeReceiptThreshold;

public sealed record ChangeReceiptThresholdCommand(decimal Threshold, string Currency);
