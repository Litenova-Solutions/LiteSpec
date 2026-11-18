using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Policy.Exceptions;

/// <summary>The threshold is negative.</summary>
/// <implements>rule/policy.threshold-never-negative</implements>
internal sealed class PolicyNegativeThresholdException : DomainException
{
    /// <summary>Creates the rejection.</summary>
    public PolicyNegativeThresholdException()
        : base("The threshold is negative.")
    {
    }

    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.negative-threshold";
}
