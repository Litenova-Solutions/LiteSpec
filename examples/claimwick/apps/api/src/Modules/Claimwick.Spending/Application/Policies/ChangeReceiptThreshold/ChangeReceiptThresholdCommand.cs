namespace Claimwick.Spending.Application.Policies.ChangeReceiptThreshold;

internal sealed record ChangeReceiptThresholdCommand(decimal Threshold, string Currency);
