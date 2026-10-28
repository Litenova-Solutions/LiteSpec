namespace Library.Domain.Lending.Copies.Exceptions;

/// <summary>The copy is on loan.</summary>
/// <implements>rule/copy.has-one-home-shelf</implements>
public sealed class CopyOnLoanException()
    : DomainException("The copy is on loan.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "lending.copy-on-loan";
}
