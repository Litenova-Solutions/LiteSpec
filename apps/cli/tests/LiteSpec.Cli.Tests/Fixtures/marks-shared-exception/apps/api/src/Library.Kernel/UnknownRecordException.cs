namespace Library.Kernel;

/// <implements>rule/library.unknown-record</implements>
public sealed partial class UnknownRecordException : DomainException
{
    public UnknownRecordException(string recordKind, string name)
        : base($"No {recordKind} record names '{name}'.")
    {
    }

    public override string Code => "library.unknown-record";
}
