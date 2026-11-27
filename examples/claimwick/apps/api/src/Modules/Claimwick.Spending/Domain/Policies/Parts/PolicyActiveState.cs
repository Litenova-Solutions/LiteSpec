using Claimwick.Kernel.Values;

namespace Claimwick.Spending.Domain.Policies.Parts;

/// <summary>The policy holds with its threshold.</summary>
internal sealed record PolicyActiveState(Money Threshold) : PolicyState;
