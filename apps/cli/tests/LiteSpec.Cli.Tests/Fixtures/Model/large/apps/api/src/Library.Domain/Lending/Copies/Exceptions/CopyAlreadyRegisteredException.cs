namespace Library.Domain.Lending.Copies.Exceptions;

/// <summary>The copy is already registered.</summary>
/// <implements>rule/copy.has-one-home-shelf</implements>
public sealed class CopyAlreadyRegisteredException()
    : DomainException("The copy is already registered.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "lending.copy-already-registered";
}
