namespace Library.Lending.Domain.Loans.Exceptions;

/// <summary>The copy is not available.</summary>
/// <implements>rule/loan.copy-available</implements>
public sealed class CopyNotAvailableException()
    : DomainException("The copy is not available.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "lending.copy-not-available";
}
