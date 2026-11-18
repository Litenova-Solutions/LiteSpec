using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Policies;

/// <summary>The policy holds with its threshold.</summary>
internal sealed record PolicyActiveState(Money Threshold) : PolicyState;
