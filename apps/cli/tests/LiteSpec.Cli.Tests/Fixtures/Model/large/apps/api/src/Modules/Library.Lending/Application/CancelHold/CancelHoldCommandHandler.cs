using Library.Lending.Domain.Holds;
using Library.Lending.Domain.Holds.Exceptions;

namespace Library.Lending.Application.CancelHold;

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
