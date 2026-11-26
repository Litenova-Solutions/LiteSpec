namespace Claimwick.Spending.Application.Policies.ChangeReceiptThreshold;

internal sealed record ChangeReceiptThresholdCommandResult(decimal Threshold, string Currency);
