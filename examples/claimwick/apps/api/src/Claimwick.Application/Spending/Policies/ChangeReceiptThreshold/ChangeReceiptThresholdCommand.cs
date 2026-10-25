namespace Claimwick.Application.Spending.Policies.ChangeReceiptThreshold;

public sealed record ChangeReceiptThresholdCommand(decimal Threshold, string Currency);
