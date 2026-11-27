using Claimwick.Kernel.Domain;

namespace Claimwick.Travel;

/// <summary>No travel rule is stated for the lookup: no set covers the country, or no row covers the role and date.</summary>
public sealed class TravelRuleMissingException : DomainException
{
    /// <summary>Creates the rejection with its reason.</summary>
    public TravelRuleMissingException(string reason)
        : base(reason)
    {
    }

    /// <summary>The stable failure code.</summary>
    public override string Code => "travel.rule-missing";
}
