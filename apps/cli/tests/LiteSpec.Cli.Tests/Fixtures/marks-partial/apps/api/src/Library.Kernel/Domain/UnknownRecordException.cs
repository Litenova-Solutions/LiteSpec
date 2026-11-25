namespace Library.Kernel.Domain;

public sealed partial class UnknownRecordException : DomainException
{
    public UnknownRecordException(string recordKind, string name)
        : base($"No {recordKind} record names '{name}'.")
    {
    }
}
