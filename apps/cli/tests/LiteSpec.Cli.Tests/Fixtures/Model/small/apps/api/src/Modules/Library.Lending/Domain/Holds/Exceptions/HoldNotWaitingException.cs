namespace Library.Lending.Domain.Holds.Exceptions;

/// <summary>The hold is not waiting.</summary>
/// <implements>rule/hold.waiting-before-cancel</implements>
public sealed class HoldNotWaitingException()
    : DomainException("The hold is not waiting.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "lending.hold-not-waiting";
}
