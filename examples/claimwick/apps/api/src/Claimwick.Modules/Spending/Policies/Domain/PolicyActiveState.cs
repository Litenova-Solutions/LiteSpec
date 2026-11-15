using Claimwick.Domain;

namespace Claimwick.Spending.Policies.Domain;

/// <summary>The policy holds with its threshold.</summary>
public sealed record PolicyActiveState(Money Threshold) : PolicyState;
