namespace Library.Domain.Lending.Loans.Exceptions;

/// <summary>The loan is not active.</summary>
/// <implements>rule/loan.returned-while-active</implements>
public sealed class LoanNotActiveException()
    : DomainException("The loan is not active.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "lending.loan-not-active";
}
