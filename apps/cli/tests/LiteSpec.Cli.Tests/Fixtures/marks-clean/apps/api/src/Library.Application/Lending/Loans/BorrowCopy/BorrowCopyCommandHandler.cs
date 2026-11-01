namespace Library.Application.Lending.Loans.BorrowCopy;

/// <implements>use-case/lending.borrow-copy</implements>
public sealed class BorrowCopyCommandHandler(ILoanRepository loanRepository, ICommandMediator mediator)
    : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
{
    public async Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken)
    {
        var loan = await loanRepository.LoadAsync(command.CopyId, cancellationToken);
        loan.Borrow(command.ReaderId, DateOnly.FromDateTime(DateTime.UtcNow));
        await mediator.SendAsync(new RegisterCopyCommand(command.CopyId), cancellationToken);
        return new BorrowCopyCommandResult(loan.Id);
    }
}
