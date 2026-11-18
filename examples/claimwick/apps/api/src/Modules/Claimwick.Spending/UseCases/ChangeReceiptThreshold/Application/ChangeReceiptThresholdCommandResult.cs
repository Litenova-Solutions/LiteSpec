namespace Claimwick.Spending.UseCases.ChangeReceiptThreshold.Application;

internal sealed record ChangeReceiptThresholdCommandResult(decimal Threshold, string Currency);
