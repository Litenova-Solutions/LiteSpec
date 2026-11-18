namespace Claimwick.Spending.Application.ChangeReceiptThreshold;

internal sealed record ChangeReceiptThresholdCommand(decimal Threshold, string Currency);
