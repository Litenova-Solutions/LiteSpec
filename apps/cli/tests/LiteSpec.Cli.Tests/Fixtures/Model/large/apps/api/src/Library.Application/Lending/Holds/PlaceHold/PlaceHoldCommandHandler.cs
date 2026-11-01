using Library.Domain.Lending.Holds;
using Library.Domain.Lending.Holds.Exceptions;

namespace Library.Application.Lending.Holds.PlaceHold;

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
