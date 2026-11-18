using Claimwick.Kernel;
using Claimwick.Spending.Domain.Expense;
using Claimwick.Spending.Domain.Policy;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.UseCases.SubmitExpense.Application;

/// <implements>use-case/spending.submit-expense</implements>
internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
{
    private readonly IExpenseRepository _expenses;
    private readonly IPolicyRepository _policies;

    public SubmitExpenseCommandHandler(IExpenseRepository expenses, IPolicyRepository policies)
    {
        _expenses = expenses;
        _policies = policies;
    }

    public async Task<SubmitExpenseCommandResult> HandleAsync(
        SubmitExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var policy = await _policies.GetForGuardAsync(cancellationToken);
        var expense = Expense.Submit(
            command.ExpenseId,
            command.EmployeeId,
            Money.Create(command.Amount, command.Currency),
            command.ReceiptId,
            policy.ReceiptThreshold);
        _expenses.Store(expense);
        return new SubmitExpenseCommandResult(expense.Id);
    }
}
