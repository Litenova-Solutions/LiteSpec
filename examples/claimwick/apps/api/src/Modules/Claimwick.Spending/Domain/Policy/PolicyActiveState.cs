using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Policy;

/// <summary>The policy holds with its threshold.</summary>
internal sealed record PolicyActiveState(Money Threshold) : PolicyState;
