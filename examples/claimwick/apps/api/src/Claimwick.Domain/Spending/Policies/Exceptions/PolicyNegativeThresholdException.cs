using Claimwick.Domain.Shared;

namespace Claimwick.Domain.Spending.Policies.Exceptions;

/// <summary>The threshold is negative.</summary>
/// <implements>rule/policy.threshold-never-negative</implements>
public sealed class PolicyNegativeThresholdException()
    : DomainException("The threshold is negative.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.negative-threshold";
}
