using Library.Lending.Domain.Holds;
using Library.Lending.Domain.Holds.Exceptions;

namespace Library.Lending.Application.PlaceHold;

/// <implements>use-case/lending.place-hold</implements>
public sealed class PlaceHoldCommandHandler : ICommandHandler<PlaceHoldCommand>
{
    public Task Handle(PlaceHoldCommand command)
    {
        if (command.CopyId == Guid.Empty)
            throw new HoldAlreadyWaitingException();
        var hold = new Hold();
        hold.Place();
        return Task.CompletedTask;
    }
}
