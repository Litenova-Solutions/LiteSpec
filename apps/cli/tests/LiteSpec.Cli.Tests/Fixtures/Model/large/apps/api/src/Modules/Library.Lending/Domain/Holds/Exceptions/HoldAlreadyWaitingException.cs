namespace Library.Lending.Domain.Holds.Exceptions;

/// <summary>The hold is already waiting.</summary>
/// <implements>rule/hold.names-one-copy</implements>
public sealed class HoldAlreadyWaitingException()
    : DomainException("The hold is already waiting.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "lending.hold-already-waiting";
}
