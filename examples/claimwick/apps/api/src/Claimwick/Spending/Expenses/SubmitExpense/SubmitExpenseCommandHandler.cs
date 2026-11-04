using Claimwick.Domain;
using Claimwick.Spending.Expenses.Domain;
using Claimwick.Spending.Policies.Domain;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Expenses.SubmitExpense;

/// <implements>use-case/spending.submit-expense</implements>
internal sealed class SubmitExpenseCommandHandler(
    IExpenseRepository expenses,
    IPolicyRepository policies) : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
{
    public async Task<SubmitExpenseCommandResult> HandleAsync(
        SubmitExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var policy = await policies.GetActiveAsync(cancellationToken);
        var expense = Expense.Submit(
            command.ExpenseId,
            command.EmployeeId,
            Money.Create(command.Amount, command.Currency),
            command.ReceiptId,
            policy.ReceiptThreshold);
        expenses.Store(expense);
        return new SubmitExpenseCommandResult(expense.Id);
    }
}
