using Library.Domain.Lending.Holds;
using Library.Domain.Lending.Holds.Exceptions;

namespace Library.Application.Lending.Holds.CancelHold;

/// <implements>use-case/lending.cancel-hold</implements>
public sealed class CancelHoldCommandHandler : ICommandHandler<CancelHoldCommand>
{
    public Task Handle(CancelHoldCommand command)
    {
        if (command.HoldId == Guid.Empty)
            throw new HoldNotWaitingException();
        var hold = new Hold();
        hold.Cancel();
        return Task.CompletedTask;
    }
}
