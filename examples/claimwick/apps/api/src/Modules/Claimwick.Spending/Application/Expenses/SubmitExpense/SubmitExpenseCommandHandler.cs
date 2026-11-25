using Claimwick.Kernel.Values;
using Claimwick.Spending.Domain.Expenses;
using Claimwick.Spending.Domain.Expenses.Parts;
using Claimwick.Spending.Domain.Policies;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

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
        var lines = command.Lines.Select(row => new LineItem(row.Category, Money.Create(row.Amount, command.Currency))).ToList();
        var expense = Expense.Submit(
            command.ExpenseId,
            command.EmployeeId,
            Money.Create(command.Amount, command.Currency),
            ExpenseCategory.FromCode(command.Category),
            command.ReceiptId,
            policy.ReceiptThreshold,
            command.Note,
            command.ImportId,
            lines);
        _expenses.Store(expense);
        return new SubmitExpenseCommandResult(expense.Id);
    }
}
