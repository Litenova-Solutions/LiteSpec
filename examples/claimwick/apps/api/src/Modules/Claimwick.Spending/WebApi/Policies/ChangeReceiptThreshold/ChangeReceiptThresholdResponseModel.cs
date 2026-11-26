namespace Claimwick.Spending.WebApi.Policies.ChangeReceiptThreshold;

/// <summary>The answer to a change-threshold request: the new threshold with its currency.</summary>
internal sealed record ChangeReceiptThresholdResponseModel(decimal Threshold, string Currency);
