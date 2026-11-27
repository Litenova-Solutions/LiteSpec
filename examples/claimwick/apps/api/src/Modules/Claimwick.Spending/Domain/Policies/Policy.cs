using Claimwick.Kernel.Domain;
using Claimwick.Kernel.Values;
using Claimwick.Spending.Domain.Policies.Exceptions;
using Claimwick.Spending.Domain.Policies.Parts;

namespace Claimwick.Spending.Domain.Policies;

/// <summary>The company spending policy and its receipt threshold.</summary>
/// <implements>aggregate/spending.policy</implements>
internal sealed class Policy : AggregateRoot<Guid>
{
    private Policy(Money threshold)
    {
        State = new PolicyActiveState(threshold);
    }

    /// <summary>The current lifecycle state.</summary>
    public PolicyState State { get; private set; }

    /// <summary>The receipt threshold of the active policy.</summary>
    public Money ReceiptThreshold => ((PolicyActiveState)State).Threshold;

    /// <summary>Activates one policy with its threshold.</summary>
    public static Policy Activate(Money threshold)
    {
        if (threshold.Amount < 0)
        {
            throw new PolicyNegativeThresholdException();
        }

        return new Policy(threshold);
    }

    /// <summary>Sets the receipt threshold.</summary>
    /// <implements>use-case/spending.change-receipt-threshold</implements>
    public void SetThreshold(Money threshold)
    {
        if (threshold.Amount < 0)
        {
            throw new PolicyNegativeThresholdException();
        }

        State = new PolicyActiveState(threshold);
    }
}
