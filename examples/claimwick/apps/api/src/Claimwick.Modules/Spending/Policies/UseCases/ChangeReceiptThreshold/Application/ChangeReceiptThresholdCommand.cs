namespace Claimwick.Spending.Policies.UseCases.ChangeReceiptThreshold.Application;

public sealed record ChangeReceiptThresholdCommand(decimal Threshold, string Currency);
