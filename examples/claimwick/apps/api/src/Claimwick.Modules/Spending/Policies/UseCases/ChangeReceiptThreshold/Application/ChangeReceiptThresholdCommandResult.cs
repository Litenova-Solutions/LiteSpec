namespace Claimwick.Spending.Policies.UseCases.ChangeReceiptThreshold.Application;

public sealed record ChangeReceiptThresholdCommandResult(decimal Threshold, string Currency);
