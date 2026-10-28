using Library.Domain.Lending.Copies.Events;
using Library.Domain.Lending.Copies.Exceptions;

namespace Library.Domain.Lending.Copies;

/// <implements>aggregate/lending.copy</implements>
public sealed class Copy : AggregateRoot<Guid>
{
    public Guid Id { get; init; }

    /// <implements>use-case/lending.register-copy</implements>
    public void Register()
    {
        RaiseDomainEvent(new CopyRegisteredEvent(Id, "shelf-1"));
    }

    /// <implements>use-case/lending.retire-copy</implements>
    public void Retire(bool onLoan)
    {
        if (onLoan)
            throw new CopyOnLoanException();
        RaiseDomainEvent(new CopyRetiredEvent(Id));
    }
}
