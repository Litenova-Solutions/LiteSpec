namespace Library.Domain.Lending.Loans.Exceptions;

/// <summary>The ghost refusal no result declares.</summary>
/// <implements>rule/lending.copy-barcode-unique</implements>
public sealed class LoanGhostRefusalException()
    : DomainException("The loan ghost refused.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "lending.ghost-refusal";
}
