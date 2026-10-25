namespace Claimwick.Application.Spending.Policies.ChangeReceiptThreshold;

public sealed record ChangeReceiptThresholdCommandResult(decimal Threshold, string Currency);
