namespace Claimwick.Spending.UseCases.ChangeReceiptThreshold.Application;

internal sealed record ChangeReceiptThresholdCommand(decimal Threshold, string Currency);
