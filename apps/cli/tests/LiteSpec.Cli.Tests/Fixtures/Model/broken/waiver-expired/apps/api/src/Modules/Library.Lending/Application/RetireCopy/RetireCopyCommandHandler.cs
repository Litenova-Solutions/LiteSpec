using Library.Lending.Domain.Copies;
using Library.Lending.Domain.Copies.Exceptions;

namespace Library.Lending.Application.RetireCopy;

/// <implements>use-case/lending.retire-copy</implements>
public sealed class RetireCopyCommandHandler : ICommandHandler<RetireCopyCommand>
{
    public Task Handle(RetireCopyCommand command)
    {
        if (command.CopyId == Guid.Empty)
            throw new CopyOnLoanException();
        var copy = new Copy();
        copy.Retire(false);
        return Task.CompletedTask;
    }
}
